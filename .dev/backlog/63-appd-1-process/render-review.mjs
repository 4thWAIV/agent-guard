// Work-item presentation renderer. Emits the fragment to stdout; never edits source.
import { readFileSync } from 'node:fs';
import { createHash } from 'node:crypto';
import { execFileSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import { dirname, resolve, relative } from 'node:path';

const folder = dirname(fileURLToPath(import.meta.url));
const repo = resolve(folder, '../../..');
const read = path => readFileSync(resolve(repo, path), 'utf8');
const modelPath = relative(repo, resolve(folder, 'review-model.json'));
const viewPath = relative(repo, resolve(folder, 'review-view.html'));
const model = JSON.parse(read(modelPath));
for (const node of model.nodes) {
  if (!node.codeFrom) continue;
  const source = model.nodes.find(candidate => candidate.id === node.codeFrom);
  if (!source || !source.code) throw new Error('Missing declaration source for ' + node.id);
  node.code = source.code;
}
const designPath = '.dev/inprocess/63-appd-1-process/design-output.json';
model.rules = JSON.parse(read(designPath)).verdict.rulesToAdd;
if (model.rules.length !== model.ruleExamples.length) throw new Error('Recorded rules changed; review example mapping before rendering.');
model.currentSources = {};
for (const node of model.nodes) {
  if (node.currentPath) model.currentSources[node.currentPath] = read(node.currentPath);
}
model.generatedAt = new Date().toISOString();
model.revision = execFileSync('git', ['rev-parse', 'HEAD'], {cwd:repo, encoding:'utf8'}).trim();
model.workingTree = execFileSync('git', ['status', '--short'], {cwd:repo, encoding:'utf8'}).trim() || 'Clean';
model.sourceHashes = [...new Set([...model.sources, modelPath, viewPath])].map(path => ({path, sha256:createHash('sha256').update(read(path)).digest('hex')}));
const payload = JSON.stringify(model).replace(/</g, '\\u003c');
const template = read(viewPath);
if (template.split('__APPD_REVIEW_MODEL__').length !== 2) throw new Error('Expected one model insertion point.');
process.stdout.write(template.replace('__APPD_REVIEW_MODEL__', () => payload));
