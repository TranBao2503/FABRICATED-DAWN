import fs from 'node:fs';
import path from 'node:path';
import { spawn } from 'node:child_process';
import { args, projectRoot, ensureClosed, fail } from './common.mjs';

try {
  const command = process.argv[2];
  const options = args(process.argv.slice(3));
  if (Object.keys(options).some(k => !['project', 'unity'].includes(k))) throw new Error('Unsupported option.');
  const root = projectRoot(options.project);
  if (command === 'inspect') {
    const read = file => fs.readFileSync(path.join(root, file), 'utf8');
    console.log(JSON.stringify({ project: root, version: read('ProjectSettings/ProjectVersion.txt').trim(),
      manifest: JSON.parse(read('Packages/manifest.json')),
      toolkitInstalled: fs.existsSync(path.join(root, 'Packages/com.4koq.fabricated-dawn/package.json')),
      editorLockPresent: fs.existsSync(path.join(root, 'Temp/UnityLockfile')),
      assets: fs.readdirSync(path.join(root, 'Assets')),
      reports: ['content-report.json', 'verification-report.json'].filter(f =>
        fs.existsSync(path.join(root, 'Assets/FabricatedDawn/Reports', f))) }, null, 2));
  } else {
    const methods = { scaffold: 'Scaffold', validate: 'Validate', verify: 'Verify' };
    if (!Object.hasOwn(methods, command)) throw new Error('Usage: unity.mjs inspect|scaffold|validate|verify --project ABSOLUTE_PATH [--unity ABSOLUTE_PATH]');
    ensureClosed(root);
    if (!options.unity || !path.isAbsolute(options.unity) || !fs.statSync(options.unity).isFile())
      throw new Error('Supply the actual Unity executable with --unity.');
    if (!fs.existsSync(path.join(root, 'Packages/com.4koq.fabricated-dawn/package.json')))
      throw new Error('Install the toolkit before running its batch methods.');
    const logDirectory = path.join(root, 'Logs/FabricatedDawn');
    fs.mkdirSync(logDirectory, { recursive: true });
    const log = path.join(logDirectory, `${command}.log`);
    const child = spawn(options.unity, ['-batchmode', '-nographics', '-quit', '-projectPath', root,
      '-executeMethod', `FabricatedDawn.Editor.Batch.${methods[command]}`, '-logFile', log],
    { shell: false, windowsHide: true, stdio: 'inherit' });
    const code = await new Promise((resolve, reject) => { child.once('error', reject); child.once('exit', resolve); });
    console.log(JSON.stringify({ action: command, exitCode: code, log }, null, 2));
    if (code !== 0) process.exitCode = 1;
  }
} catch (error) { fail(error); }
