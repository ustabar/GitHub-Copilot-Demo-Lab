import { readFile } from 'node:fs/promises';
import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { StdioServerTransport } from '@modelcontextprotocol/sdk/server/stdio.js';
import { z } from 'zod';

const version = '1.0.0';
const fixtures = [
  {
    tool: 'get_order_contract',
    name: 'order-contract',
    description: 'Read the canonical synthetic Orders API contract, including validation and idempotency rules.',
    uri: 'orders-contract://v1/contract',
    file: new URL('../../fixtures/order-contract.json', import.meta.url),
  },
  {
    tool: 'get_acceptance_matrix',
    name: 'acceptance-matrix',
    description: 'Read the synthetic acceptance matrix. Expected outcomes are not evidence of an executed test.',
    uri: 'orders-contract://v1/acceptance',
    file: new URL('../../fixtures/acceptance-matrix.json', import.meta.url),
  },
];

async function main() {
  const server = new McpServer({ name: 'orders-contract', version });

  for (const fixture of fixtures) {
    // Only these checked-in file URLs are readable; no caller-supplied paths or URLs.
    const text = await readFile(fixture.file, 'utf8');
    const data = JSON.parse(text);
    if (data.contractVersion !== version) {
      throw new Error(`Fixture ${fixture.name} has an unsupported contractVersion.`);
    }

    server.registerTool(fixture.tool, {
      description: fixture.description,
      inputSchema: z.strictObject({
        version: z.string().max(32).optional().describe('Contract version; omit for 1.0.0.'),
      }),
      annotations: {
        readOnlyHint: true,
        destructiveHint: false,
        idempotentHint: true,
        openWorldHint: false,
      },
    }, async ({ version: requested = version }) => {
      if (requested !== version) {
        return {
          isError: true,
          content: [{ type: 'text', text: `Unsupported contract version "${requested}". Supported: ${version}.` }],
        };
      }
      return { content: [{ type: 'text', text }] };
    });

    server.registerResource(fixture.name, fixture.uri, {
      description: fixture.description,
      mimeType: 'application/json',
    }, async () => ({
      contents: [{ uri: fixture.uri, mimeType: 'application/json', text }],
    }));
  }

  // stdout belongs exclusively to the SDK JSON-RPC transport.
  await server.connect(new StdioServerTransport());
}

main().catch((error) => {
  console.error(`Orders contract MCP could not start: ${error.message}`);
  process.exitCode = 1;
});
