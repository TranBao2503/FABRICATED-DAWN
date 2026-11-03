import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { args, projectRoot, ensureClosed, fail } from './common.mjs';

try {
  const options = args(process.argv.slice(2));
  if (Object.keys(options).some(k => k !== 'project')) throw new Error('Only --project is supported.');
  const root = projectRoot(options.project);
  ensureClosed(root);
  const source = fileURLToPath(new URL('../templates/com.4koq.fabricated-dawn/', import.meta.url));
  const destination = path.join(root, 'Packages', 'com.4koq.fabricated-dawn');
  if (fs.existsSync(destination)) throw new Error('Toolkit already exists; refusing to overwrite it.');
  // Reject symlinks anywhere in the distributable source before copying.
  function check(folder) {
    for (const entry of fs.readdirSync(folder, { withFileTypes: true })) {
      if (entry.isSymbolicLink()) throw new Error('Package source contains a symlink.');
      if (entry.isDirectory()) check(path.join(folder, entry.name));
    }
  }
  check(source);
  JSON.parse(fs.readFileSync(path.join(source, 'package.json'), 'utf8'));
  fs.cpSync(source, destination, { recursive: true, force: false, errorOnExist: true });
  console.log(JSON.stringify({ installed: destination, manifestChanged: false }, null, 2));
} catch (error) { fail(error); }
