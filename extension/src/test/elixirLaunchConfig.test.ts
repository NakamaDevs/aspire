import * as assert from 'assert';
import * as sinon from 'sinon';
import * as vscode from 'vscode';
import { elixirLSExtensionId, getSupportedCapabilities } from '../capabilities';
import { AspireDebugSession } from '../debugger/AspireDebugSession';
import { getResourceDebuggerExtensions } from '../debugger/debuggerExtensions';
import { elixirDebuggerExtension } from '../debugger/languages/elixir';
import { AspireResourceExtendedDebugConfiguration, ElixirLaunchConfiguration } from '../dcp/types';

function createDebugConfig(overrides: Partial<AspireResourceExtendedDebugConfiguration> = {}): AspireResourceExtendedDebugConfiguration {
    return {
        runId: '1',
        debugSessionId: '1',
        type: 'elixir',
        name: 'Elixir',
        request: 'launch',
        program: '/workspace/apps/api',
        args: [],
        ...overrides
    };
}

function createLaunchConfig(overrides: Partial<ElixirLaunchConfiguration> = {}): ElixirLaunchConfiguration {
    return {
        type: 'elixir',
        project_dir: '/workspace/apps/api',
        task: 'phx.server',
        task_args: [],
        working_directory: '/workspace/apps/api',
        ...overrides
    };
}

suite('Elixir Debugger Extension Tests', () => {
    const fakeAspireDebugSession = {} as AspireDebugSession;

    teardown(() => sinon.restore());

    test('advertises Elixir support when ElixirLS is installed', () => {
        sinon.stub(vscode.extensions, 'getExtension').callsFake((extensionId: string) => {
            return extensionId === elixirLSExtensionId ? { id: extensionId } as vscode.Extension<unknown> : undefined;
        });

        const capabilities = getSupportedCapabilities();
        assert.ok(capabilities.includes('elixir'));
        assert.ok(capabilities.includes(elixirLSExtensionId));
        assert.ok(getResourceDebuggerExtensions().some(extension => extension.resourceType === 'elixir'));
    });

    test('does not advertise Elixir support when ElixirLS is missing', () => {
        sinon.stub(vscode.extensions, 'getExtension').returns(undefined);

        const capabilities = getSupportedCapabilities();
        assert.ok(!capabilities.includes('elixir'));
        assert.ok(!getResourceDebuggerExtensions().some(extension => extension.resourceType === 'elixir'));
    });

    test('builds a mix_task configuration from the launch payload', async () => {
        const launchConfig = createLaunchConfig({
            task: 'phx.server',
            task_args: ['--port', '4000'],
        });
        const debugConfig = createDebugConfig();

        await elixirDebuggerExtension.createDebugSessionConfigurationCallback!(
            launchConfig,
            [],
            [],
            { debug: true, runId: '1', debugSessionId: '1', isApphost: false, debugSession: fakeAspireDebugSession },
            debugConfig);

        assert.strictEqual(debugConfig.type, 'mix_task');
        assert.strictEqual(debugConfig.request, 'launch');
        assert.strictEqual((debugConfig as any).projectDir, '/workspace/apps/api');
        assert.strictEqual((debugConfig as any).task, 'phx.server');
        assert.deepStrictEqual((debugConfig as any).taskArgs, ['--port', '4000']);
        assert.strictEqual((debugConfig as any).startApps, true);
        assert.deepStrictEqual((debugConfig as any).requireFiles, []);
        assert.strictEqual(debugConfig.cwd, '/workspace/apps/api');
    });

    test('maps task_args to taskArgs', async () => {
        const launchConfig = createLaunchConfig({
            task_args: ['run', '--no-halt'],
        });
        const debugConfig = createDebugConfig();

        await elixirDebuggerExtension.createDebugSessionConfigurationCallback!(
            launchConfig,
            [],
            [],
            { debug: true, runId: '1', debugSessionId: '1', isApphost: false, debugSession: fakeAspireDebugSession },
            debugConfig);

        assert.deepStrictEqual((debugConfig as any).taskArgs, ['run', '--no-halt']);
    });

    test('adds MIX_ENV when mix_env is present', async () => {
        const launchConfig = createLaunchConfig({
            mix_env: 'dev',
        });
        const debugConfig = createDebugConfig({ env: { EXISTING: 'value' } });

        await elixirDebuggerExtension.createDebugSessionConfigurationCallback!(
            launchConfig,
            [],
            [],
            { debug: true, runId: '1', debugSessionId: '1', isApphost: false, debugSession: fakeAspireDebugSession },
            debugConfig);

        assert.strictEqual((debugConfig.env as Record<string, string>).MIX_ENV, 'dev');
        assert.strictEqual((debugConfig.env as Record<string, string>).EXISTING, 'value');
    });

    test('omits MIX_ENV when mix_env is absent', async () => {
        const launchConfig = createLaunchConfig();
        delete launchConfig.mix_env;
        const debugConfig = createDebugConfig({ env: { EXISTING: 'value' } });

        await elixirDebuggerExtension.createDebugSessionConfigurationCallback!(
            launchConfig,
            [],
            [],
            { debug: true, runId: '1', debugSessionId: '1', isApphost: false, debugSession: fakeAspireDebugSession },
            debugConfig);

        assert.strictEqual((debugConfig.env as Record<string, string> | undefined)?.MIX_ENV, undefined);
        assert.strictEqual((debugConfig.env as Record<string, string>).EXISTING, 'value');
    });
});
