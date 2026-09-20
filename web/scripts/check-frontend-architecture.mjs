import { existsSync, readFileSync, readdirSync, statSync } from "node:fs";
import { join, relative } from "node:path";

const workspace = process.cwd();
const applications = ["storefront", "agent-portal", "admin-portal"];
const failures = [];

if (existsSync(join(workspace, "src")))
  failures.push("The retired web/src application must not be restored.");

for (const application of applications) {
  const applicationRoot = join(workspace, "apps", application);
  for (const file of sourceFiles(applicationRoot)) {
    const source = readFileSync(file, "utf8");
    for (const otherApplication of applications.filter(
      (candidate) => candidate !== application,
    )) {
      if (
        source.includes(`/apps/${otherApplication}/`) ||
        source.includes(`apps/${otherApplication}`)
      )
        failures.push(
          `${relative(workspace, file)} imports another deployable application (${otherApplication}).`,
        );
    }
    if (
      /NEXT_PUBLIC_(?:PAYMONGO|DATABASE|SMTP|SECRET|PASSWORD|CONNECTION)/i.test(
        source,
      )
    )
      failures.push(
        `${relative(workspace, file)} exposes a server secret as NEXT_PUBLIC_.`,
      );
    if (
      /<(?:InputField|input)\b(?:(?!>).)*(?:name|id)=["'](?:organizationId|customerId|agentId)["']/is.test(
        source,
      )
    )
      failures.push(
        `${relative(workspace, file)} renders a protected identity identifier as user input.`,
      );
  }
}

if (failures.length > 0) {
  for (const failure of failures) console.error(failure);
  process.exitCode = 1;
} else {
  console.log(
    "Frontend application boundaries and identity-input rules passed.",
  );
}

function sourceFiles(directory) {
  return readdirSync(directory).flatMap((name) => {
    const path = join(directory, name);
    if (statSync(path).isDirectory())
      return name === ".next" || name === "node_modules"
        ? []
        : sourceFiles(path);
    return /\.(?:ts|tsx)$/.test(name) ? [path] : [];
  });
}
