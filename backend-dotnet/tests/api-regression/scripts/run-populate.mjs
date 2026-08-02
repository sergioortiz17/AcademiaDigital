import { spawnSync } from 'node:child_process';
import { resolve } from 'node:path';

const playwrightCli = resolve('node_modules', '@playwright', 'test', 'cli.js');
const result = spawnSync(process.execPath, [playwrightCli, 'test', 'tests/flows', 'tests/queries'], {
  env: { ...process.env, E2E_PRESERVE_DATA: 'true' },
  stdio: 'inherit'
});

if (result.error) throw result.error;
process.exit(result.status ?? 1);
