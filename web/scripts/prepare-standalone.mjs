import { cpSync, existsSync, mkdirSync } from "node:fs";
import { join } from "node:path";
import { fileURLToPath } from "node:url";

const application = process.argv[2];
if (!application) throw new Error("Application name is required.");

const workspace = fileURLToPath(new URL("..", import.meta.url));
const root = join(workspace, "apps", application);
const target = join(root, ".next", "standalone", "apps", application);
if (!existsSync(join(target, "server.js")))
  throw new Error(`Standalone server was not generated for ${application}.`);

copy(join(root, ".next", "static"), join(target, ".next", "static"));
copy(join(root, "public"), join(target, "public"));
console.log(`${application}: standalone static assets prepared.`);

function copy(source, destination) {
  if (!existsSync(source)) return;
  mkdirSync(destination, { recursive: true });
  cpSync(source, destination, { recursive: true, force: true });
}
