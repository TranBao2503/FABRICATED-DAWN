import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import crypto from 'node:crypto';
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const plugin = path.join(root, 'fabricated-dawn-assistant');
const parse = p => JSON.parse(fs.readFileSync(p, 'utf8').replace(/^\uFEFF/, ''));
const manifest = parse(path.join(plugin, 'plugin.json'));
assert.equal(manifest.$schema, 'https://agent-plugins.org/schemas/1.0.0/plugin.schema.json');
assert.match(manifest.name, /^[a-z0-9]+(?:[.-][a-z0-9]+)*$/);
assert.match(manifest.version, /^\d+\.\d+\.\d+$/);
const ui = manifest.extensions['com.openai'].interface;
assert.ok(ui.shortDescription.length <= 30 && ui.shortDescription.trim());
assert.ok(ui.displayName.length <= 30);
assert.equal(ui.category, 'Developer Tools');
assert.ok(Array.isArray(ui.capabilities));
assert.ok(ui.defaultPrompt.length <= 3 && ui.defaultPrompt.every(s => s.length <= 128));
for (const icon of [ui.logo, ui.composerIcon]) assert.ok(fs.existsSync(path.resolve(plugin, icon)));
const compat = parse(path.join(plugin, '.codex-plugin/plugin.json'));
assert.equal(compat.name, manifest.name); assert.equal(compat.version, manifest.version);
assert.deepEqual(compat.interface, ui);

let skillCount = 0, jsonCount = 0, fileCount = 0;
function walk(folder) {
  for (const entry of fs.readdirSync(folder, { withFileTypes: true })) {
    assert.ok(!entry.isSymbolicLink(), `Symlink: ${entry.name}`);
    const file = path.join(folder, entry.name);
    if (entry.isDirectory()) { assert.ok(!['node_modules', 'Library', '.git'].includes(entry.name)); walk(file); continue; }
    fileCount++;
    if (/\.(json|asmdef)$/.test(file)) { parse(file); jsonCount++; }
    if (entry.name === 'SKILL.md') {
      const text = fs.readFileSync(file, 'utf8');
      assert.ok(Buffer.byteLength(text) < 1024 * 1024);
      const match = text.match(/^---\r?\n([\s\S]*?)\r?\n---\r?\n/);
      assert.ok(match, `Missing frontmatter: ${file}`);
      // Both skills intentionally use the plain-string mapping subset of YAML.
      const data = {};
      for (const line of match[1].split(/\r?\n/)) {
        const field = line.match(/^([a-z_]+): ([^\r\n]+)$/);
        assert.ok(field, `Invalid frontmatter mapping: ${line}`);
        assert.ok(!Object.hasOwn(data, field[1])); data[field[1]] = field[2];
      }
      assert.equal(data.name, path.basename(path.dirname(file)));
      assert.ok(data.description.trim()); skillCount++;
    }
    if (entry.name.endsWith('.md')) {
      const text = fs.readFileSync(file, 'utf8');
      for (const match of text.matchAll(/\[[^\]]*\]\(([^)]+)\)/g)) {
        if (/^(https?:|#)/.test(match[1])) continue;
        assert.ok(fs.existsSync(path.resolve(path.dirname(file), match[1])), `Broken reference in ${file}: ${match[1]}`);
      }
    }
  }
}
walk(plugin); assert.equal(skillCount, 2);

const canonical = path.join(root, 'UnityProject/Packages/com.4koq.fabricated-dawn');
const template = path.join(plugin, 'skills/unity-workflow/templates/com.4koq.fabricated-dawn');
const digest = file => crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex');
function compare(a, b) {
  assert.deepEqual(fs.readdirSync(a).sort(), fs.readdirSync(b).sort());
  for (const entry of fs.readdirSync(a, { withFileTypes: true })) {
    const ap = path.join(a, entry.name), bp = path.join(b, entry.name);
    if (entry.isDirectory()) compare(ap, bp); else assert.equal(digest(ap), digest(bp));
  }
}
compare(canonical, template);

const fixture = path.join(root, 'verification/installer-fixture');
assert.ok(!fs.existsSync(fixture), 'Fixture already exists; use a fresh location before repeating this check.');
for (const dir of ['Assets', 'Packages', 'ProjectSettings']) fs.mkdirSync(path.join(fixture, dir), { recursive: true });
fs.writeFileSync(path.join(fixture, 'ProjectSettings/ProjectVersion.txt'), 'm_EditorVersion: 6000.6.6f1\n');
const projectManifest = path.join(fixture, 'Packages/manifest.json');
const untouched = '{"dependencies":{},"userSentinel":"preserve"}\n';
fs.writeFileSync(projectManifest, untouched);
const run = (name, argv) => spawnSync(process.execPath, [path.join(plugin, 'skills/unity-workflow/tools', name), ...argv], { encoding: 'utf8' });
let result = run('unity.mjs', ['inspect', '--project', fixture]);
assert.equal(result.status, 0, result.stderr);
assert.equal(JSON.parse(result.stdout).toolkitInstalled, false);
result = run('install-toolkit.mjs', ['--project', fixture]); assert.equal(result.status, 0, result.stderr);
compare(canonical, path.join(fixture, 'Packages/com.4koq.fabricated-dawn'));
assert.equal(fs.readFileSync(projectManifest, 'utf8'), untouched);
result = run('install-toolkit.mjs', ['--project', fixture]);
assert.equal(result.status, 1); assert.match(result.stderr, /refusing to overwrite/);
assert.equal(fs.readFileSync(projectManifest, 'utf8'), untouched);
result = run('unity.mjs', ['arbitrary-method', '--project', fixture]); assert.equal(result.status, 1);
fs.mkdirSync(path.join(fixture, 'Temp'));
fs.writeFileSync(path.join(fixture, 'Temp/UnityLockfile'), 'fixture');
result = run('unity.mjs', ['verify', '--project', fixture]);
assert.equal(result.status, 1); assert.match(result.stderr, /UnityLockfile/);

const report = { success: true, skillCount, jsonCount, fileCount,
  checks: ['Portable and compatibility manifests', 'Skill frontmatter and references', 'JSON and assembly definitions',
    'Unity source/template parity', 'Read-only project inspect', 'Installer preserves project manifest',
    'Installer rejects overwrite', 'Batch rejects unknown commands', 'Batch respects editor lock'] };
fs.writeFileSync(path.join(root, 'verification/plugin-report.json'), JSON.stringify(report, null, 2));
console.log(JSON.stringify(report, null, 2));
