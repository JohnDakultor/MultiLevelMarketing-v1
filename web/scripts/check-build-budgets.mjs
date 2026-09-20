import { readdirSync, readFileSync, statSync } from "node:fs";
import { join } from "node:path";
import { gzipSync } from "node:zlib";

const applications = ["storefront", "agent-portal", "admin-portal"];
const maximumGzipBytes = Number(
  process.env.MAX_CLIENT_JS_GZIP_BYTES ?? 2_500_000,
);
const forbiddenPatterns = [
  /PAYMONGO_SECRET/i,
  /ConnectionStrings__/i,
  /Email:Smtp:Password/i,
  /\bsk_(?:live|test)_[A-Za-z0-9_-]{12,}\b/,
  /\bre_[A-Za-z0-9_-]{12,}\b/,
];

let failed = false;
for (const application of applications) {
  const directory = join("apps", application, ".next", "static", "chunks");
  const files = walk(directory).filter((file) => file.endsWith(".js"));
  const gzipBytes = files.reduce(
    (total, file) => total + gzipSync(readFileSync(file)).byteLength,
    0,
  );
  console.log(
    `${application}: ${files.length} chunks, ${gzipBytes} gzip bytes`,
  );
  if (gzipBytes > maximumGzipBytes) {
    console.error(`${application} exceeds ${maximumGzipBytes} gzip bytes.`);
    failed = true;
  }
  for (const file of files) {
    const source = readFileSync(file, "utf8");
    if (forbiddenPatterns.some((pattern) => pattern.test(source))) {
      console.error(`${application}: possible server secret found in ${file}`);
      failed = true;
    }
  }
}

if (failed) process.exitCode = 1;

function walk(directory) {
  return readdirSync(directory).flatMap((name) => {
    const path = join(directory, name);
    return statSync(path).isDirectory() ? walk(path) : [path];
  });
}
