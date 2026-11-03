import fs from 'node:fs';
import path from 'node:path';

export function args(argv) {
  const result = {};
  for (let i = 0; i < argv.length; i += 2) {
    if (!argv[i]?.startsWith('--') || !argv[i + 1] || argv[i + 1].startsWith('--'))
      throw new Error('Expected --name value pairs.');
    const key = argv[i].slice(2);
    if (key in result) throw new Error(`Duplicate argument: ${key}`);
    result[key] = argv[i + 1];
  }
  return result;
}

export function projectRoot(input) {
  if (!input || !path.isAbsolute(input)) throw new Error('--project must be an absolute path.');
  const root = fs.realpathSync(input);
  for (const child of ['Assets', 'Packages', 'ProjectSettings']) {
    const full = path.join(root, child);
    if (!fs.statSync(full).isDirectory()) throw new Error(`Not a Unity project: missing ${child}`);
    const resolved = fs.realpathSync(full);
    const relative = path.relative(root, resolved);
    if (relative.startsWith('..') || path.isAbsolute(relative)) throw new Error(`Project directory escapes root: ${child}`);
  }
  if (!fs.existsSync(path.join(root, 'ProjectSettings', 'ProjectVersion.txt')))
    throw new Error('Missing ProjectVersion.txt.');
  return root;
}

export function ensureClosed(root) {
  if (fs.existsSync(path.join(root, 'Temp', 'UnityLockfile')))
    throw new Error('UnityLockfile exists. Close the Unity project normally before this operation; do not delete an active lock.');
}

export function fail(error) { console.error(error.message); process.exitCode = 1; }
