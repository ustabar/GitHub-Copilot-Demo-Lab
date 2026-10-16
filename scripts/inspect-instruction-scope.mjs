import { readFileSync, readdirSync } from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const instruction = path.join(root, ".github", "instructions", "api.instructions.md");
const content = readFileSync(instruction, "utf8");
const match = content.match(/^applyTo:\s*["']([^"']+)["']\s*$/m);
if (!match) throw new Error("Expected a quoted applyTo in API instruction frontmatter.");
const patterns = match[1].split(",").map((value) => value.trim());
const api = path.join(root, "src", "Orders.Api");
const files = readdirSync(api, { recursive: true, withFileTypes: true })
  .filter((entry) => entry.isFile() && entry.name.endsWith(".cs"))
  .map((entry) => path.relative(root, path.join(entry.parentPath, entry.name)).replaceAll("\\", "/"))
  .filter((name) => !name.includes("/obj/") && !name.includes("/bin/"));
if (files.length === 0) throw new Error("No API C# files found.");
const matches = files.filter((name) => patterns.some((pattern) => path.posix.matchesGlob(name, pattern)));
console.log(JSON.stringify({
  instruction: path.relative(root, instruction),
  patterns,
  sourceFiles: files.length,
  matchedFiles: matches.length,
  unmatched: files.filter((name) => !matches.includes(name)),
  limitation: "Static glob evidence only; not proof of what a Copilot session actually loaded."
}, null, 2));
if (matches.length !== files.length) process.exitCode = 1;
