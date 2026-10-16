import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
import test from 'node:test';
import { Client } from '@modelcontextprotocol/sdk/client/index.js';
import { StdioClientTransport } from '@modelcontextprotocol/sdk/client/stdio.js';

const definitions = [
  ['get_order_contract', 'orders-contract://v1/contract', '../../fixtures/order-contract.json'],
  ['get_acceptance_matrix', 'orders-contract://v1/acceptance', '../../fixtures/acceptance-matrix.json'],
];

test('official SDK stdio client validates the complete read-only contract surface', { timeout: 20000 }, async (t) => {
  const transport = new StdioClientTransport({
    command: process.execPath,
    args: [fileURLToPath(new URL('./server.mjs', import.meta.url))],
    // This must work independently of the caller's current working directory.
    cwd: fileURLToPath(new URL('../../fixtures', import.meta.url)),
    stderr: 'pipe',
  });
  let stderr = '';
  transport.stderr?.on('data', (chunk) => { stderr += chunk.toString(); });
  const client = new Client({ name: 'orders-contract-test', version: '1.0.0' });
  const protocolErrors = [];
  client.onerror = (error) => { protocolErrors.push(error.message); };
  t.after(async () => { await client.close(); await transport.close(); });
  await client.connect(transport);

  await t.test('initialize advertises only tools and resources', () => {
    assert.deepEqual(client.getServerVersion(), { name: 'orders-contract', version: '1.0.0' });
    const capabilities = client.getServerCapabilities();
    assert.ok(capabilities.tools);
    assert.ok(capabilities.resources);
    assert.equal(capabilities.prompts, undefined);
  });

  const { tools } = await client.listTools();
  await t.test('tools/list has bounded, strict schemas and read-only annotations', () => {
    assert.deepEqual(tools.map((tool) => tool.name).sort(), definitions.map(([name]) => name).sort());
    for (const tool of tools) {
      assert.equal(tool.inputSchema.additionalProperties, false);
      assert.deepEqual(Object.keys(tool.inputSchema.properties), ['version']);
      assert.deepEqual(tool.annotations, {
        readOnlyHint: true, destructiveHint: false, idempotentHint: true, openWorldHint: false,
      });
    }
  });

  const { resources } = await client.listResources();
  assert.deepEqual(resources.map(({ uri }) => uri).sort(), definitions.map(([, uri]) => uri).sort());

  for (const [name, uri, path] of definitions) {
    const expected = await readFile(new URL(path, import.meta.url), 'utf8');
    await t.test(`${name}: tools/call returns the exact checked-in JSON with default and explicit versions`, async () => {
      for (const args of [{}, { version: '1.0.0' }]) {
        const result = await client.callTool({ name, arguments: args });
        assert.notEqual(result.isError, true);
        assert.deepEqual(result.content, [{ type: 'text', text: expected }]);
        assert.equal(JSON.parse(result.content[0].text).contractVersion, '1.0.0');
      }
    });
    await t.test(`${name}: resources/read returns the exact same bytes`, async () => {
      const result = await client.readResource({ uri });
      assert.deepEqual(result.contents, [{ uri, mimeType: 'application/json', text: expected }]);
    });
    await t.test(`${name}: unsupported versions return a clear error`, async () => {
      const result = await client.callTool({ name, arguments: { version: '9.9.9' } });
      assert.equal(result.isError, true);
      assert.match(result.content[0].text, /Unsupported contract version "9\.9\.9"\. Supported: 1\.0\.0/);
    });
    await t.test(`${name}: arbitrary path, URL, shell command and wrong types are rejected`, async () => {
      for (const args of [
        { path: '..\\..\\private.txt' },
        { url: 'https://example.invalid' },
        { command: 'whoami' },
        { version: 1 },
        { version: 'x'.repeat(33) },
      ]) {
        const result = await client.callTool({ name, arguments: args });
        assert.equal(result.isError, true);
      }
    });
  }

  await t.test('unknown resources and tools cannot expand access', async () => {
    await assert.rejects(client.readResource({ uri: 'file:///private.txt' }), /not found|unknown/i);
    const result = await client.callTool({ name: 'run_shell', arguments: {} });
    assert.equal(result.isError, true);
  });
  assert.equal(stderr, '');
  assert.deepEqual(protocolErrors, []);
});

test('canonical fixtures have consistent versions, unique case ids and a valid example', async () => {
  const contract = JSON.parse(await readFile(new URL(definitions[0][2], import.meta.url), 'utf8'));
  const matrix = JSON.parse(await readFile(new URL(definitions[1][2], import.meta.url), 'utf8'));
  assert.equal(contract.contractVersion, matrix.contractVersion);
  assert.equal(new Set(matrix.cases.map(({ id }) => id)).size, matrix.cases.length);
  assert.equal(contract.example.body.sku, matrix.execution.body.sku);
  assert.equal(contract.example.body.quantity, matrix.execution.body.quantity);
  for (const [name, schema] of Object.entries(contract.headers)) {
    const value = contract.example.headers[name];
    assert.ok(value.trim().length >= schema.minLength && value.length <= schema.maxLength);
  }
  for (const entry of matrix.cases) {
    assert.ok(entry.actions.length > 0, entry.id);
    assert.ok(Object.keys(entry.expected).length > 0, entry.id);
    for (const mode of entry.intentionalFailureModes ?? []) {
      assert.ok(mode in contract.checkpoints.modes && mode !== 'fixed', `${entry.id}: ${mode}`);
    }
  }
});
