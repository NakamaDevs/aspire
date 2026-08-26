import * as assert from 'assert';
import * as sinon from 'sinon';
import * as vscode from 'vscode';
import { dartCodeExtensionId, getSupportedCapabilities } from '../capabilities';
import { AspireDebugSession } from '../debugger/AspireDebugSession';
import { getResourceDebuggerExtensions } from '../debugger/debuggerExtensions';
import { dartDebuggerExtension } from '../debugger/languages/dart';
import { AspireResourceExtendedDebugConfiguration, DartLaunchConfiguration } from '../dcp/types';

function createDebugConfig(overrides: Partial<AspireResourceExtendedDebugConfiguration> = {}): AspireResourceExtendedDebugConfiguration {
    return {
        runId: '1',
        debugSessionId: '1',
        type: 'dart',
        name: 'Dart',
        request: 'launch',
        program: '/workspace/apps/api/bin/main.dart',
        args: [],
        ...overrides
    };
}

function createLaunchConfig(overrides: Partial<DartLaunchConfiguration> = {}): DartLaunchConfiguration {
    return {
        type: 'dart',
        program: '/workspace/apps/api/bin/main.dart',
        cwd: '/workspace/apps/api',
        args: [],
        tool_args: [],
        ...overrides
    };
}

suite('Dart Debugger Extension Tests', () => {
    const fakeAspireDebugSession = {} as AspireDebugSession;

    teardown(() => sinon.restore());

    test('advertises the capability when Dart-Code is installed', () => {
        sinon.stub(vscode.extensions, 'getExtension').callsFake((extensionId: string) => {
            return extensionId === dartCodeExtensionId ? { id: extensionId } as vscode.Extension<unknown> : undefined;
        });

        const capabilities = getSupportedCapabilities();
        assert.ok(capabilities.includes('dart'));
        assert.ok(capabilities.includes(dartCodeExtensionId));
        assert.ok(getResourceDebuggerExtensions().some(extension => extension.resourceType === 'dart'));
    });

    test('does not advertise the capability when Dart-Code is missing', () => {
        sinon.stub(vscode.extensions, 'getExtension').returns(undefined);

        const capabilities = getSupportedCapabilities();
        assert.ok(!capabilities.includes('dart'));
        assert.ok(!getResourceDebuggerExtensions().some(extension => extension.resourceType === 'dart'));
    });

    test('builds a dart configuration from the launch payload', async () => {
        const launchConfig = createLaunchConfig({
            program: '/workspace/apps/api/bin/main.dart',
            cwd: '/workspace/apps/api',
            args: ['--verbose'],
        });
        const debugConfig = createDebugConfig();

        await dartDebuggerExtension.createDebugSessionConfigurationCallback!(
            launchConfig,
            [],
            [],
            { debug: true, runId: '1', debugSessionId: '1', isApphost: false, debugSession: fakeAspireDebugSession },
            debugConfig);

        assert.strictEqual(debugConfig.type, 'dart');
        assert.strictEqual(debugConfig.request, 'launch');
        assert.strictEqual(debugConfig.program, '/workspace/apps/api/bin/main.dart');
        assert.strictEqual(debugConfig.cwd, '/workspace/apps/api');
        assert.deepStrictEqual(debugConfig.args, ['--verbose']);
    });

    test('maps tool_args to toolArgs', async () => {
        const launchConfig = createLaunchConfig({
            tool_args: ['--define', 'FOO=bar'],
        });
        const debugConfig = createDebugConfig();

        await dartDebuggerExtension.createDebugSessionConfigurationCallback!(
            launchConfig,
            [],
            [],
            { debug: true, runId: '1', debugSessionId: '1', isApphost: false, debugSession: fakeAspireDebugSession },
            debugConfig);

        assert.deepStrictEqual((debugConfig as any).toolArgs, ['--define', 'FOO=bar']);
    });

    test('passes env through', async () => {
        const launchConfig = createLaunchConfig();
        const debugConfig = createDebugConfig({ env: { EXISTING: 'value' } });

        await dartDebuggerExtension.createDebugSessionConfigurationCallback!(
            launchConfig,
            [],
            [],
            { debug: true, runId: '1', debugSessionId: '1', isApphost: false, debugSession: fakeAspireDebugSession },
            debugConfig);

        assert.strictEqual((debugConfig.env as Record<string, string>).EXISTING, 'value');
    });
});
