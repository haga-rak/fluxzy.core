#!/usr/bin/env node

const { execFileSync } = require("child_process");
const path = require("path");
const fs = require("fs");

const PLATFORM_MAPPING = {
  "win32-x64": { pkg: "@fluxzy/cli-win32-x64", bin: "fluxzy.exe" },
  "win32-ia32": { pkg: "@fluxzy/cli-win32-ia32", bin: "fluxzy.exe" },
  "win32-arm64": { pkg: "@fluxzy/cli-win32-arm64", bin: "fluxzy.exe" },
  "linux-x64": { pkg: "@fluxzy/cli-linux-x64", bin: "fluxzy" },
  "linux-arm64": { pkg: "@fluxzy/cli-linux-arm64", bin: "fluxzy" },
  "darwin-x64": { pkg: "@fluxzy/cli-darwin-x64", bin: "fluxzy" },
  "darwin-arm64": { pkg: "@fluxzy/cli-darwin-arm64", bin: "fluxzy" },
};

const key = `${process.platform}-${process.arch}`;
const mapping = PLATFORM_MAPPING[key];

if (!mapping) {
  console.error(
    `Error: Unsupported platform ${process.platform}-${process.arch}`
  );
  console.error(`Supported platforms: ${Object.keys(PLATFORM_MAPPING).join(", ")}`);
  process.exit(1);
}

let binPath;
try {
  const pkgDir = path.dirname(require.resolve(`${mapping.pkg}/package.json`));
  binPath = path.join(pkgDir, "bin", mapping.bin);
} catch {
  console.error(
    `Error: Platform package '${mapping.pkg}' is not installed.`
  );
  console.error(
    `This usually means your platform (${key}) was not included during installation.`
  );
  console.error(`Try reinstalling: npm install -g @fluxzy/cli`);
  process.exit(1);
}

if (!fs.existsSync(binPath)) {
  console.error(`Error: Binary not found at ${binPath}`);
  process.exit(1);
}

const args = process.argv.slice(2);

try {
  const result = execFileSync(binPath, args, {
    stdio: "inherit",
    env: process.env,
  });
} catch (err) {
  if (err.status !== null) {
    process.exit(err.status);
  }
  throw err;
}
