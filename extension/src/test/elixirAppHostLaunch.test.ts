import * as assert from 'assert';
import * as sinon from 'sinon';
import * as vscode from 'vscode';
import { elixirLSExtensionId } from '../capabilities';
import { AspireDebugSession } from '../debugger/AspireDebugSession';
import * as debuggerExtensionsModule from '../debugger/debuggerExtensions';
import { AlreadyStartedResourceDebugSession } from '../debugger/debuggerExtensions';
import * as elixirLanguageModule from '../debugger/languages/elixir';
import { elixirAppHostRunningWithoutDebugger, elixirLSNotInstalledHint } from '../loc/strings';

interface OutputEvent {
    body: { category: string; output: string };
}

suite('Elixir AppHost Launch Tests', () => {
    const fakeParentDebugSession = {
        id: 'aspire-session',
        type: 'aspire',
        name: 'Aspire',
        configuration: {
            type: 'aspire',
            request: 'launch',
            name: 'Aspire',
            program: '/workspace/apphost.exs',
            command: 'run',
        },
    };

    teardown(() => sinon.restore());

    function createFakeAppHostSession(): AlreadyStartedResourceDebugSession {
        return {
            id: 'elixir-apphost',
            processId: 4242,
            session: { id: 'elixir-apphost' } as vscode.DebugSession,
            stopSession: () => Promise.resolve(),
            // Never resolves during the test, so the termination-driven shutdown path below it is
            // not exercised here.
            termination: new Promise<number>(() => { }),
        };
    }

    function createDebugSession(): AspireDebugSession {
        const debugSession = new AspireDebugSession(
            fakeParentDebugSession as unknown as vscode.DebugSession,
            {} as any,
            {} as any,
            {} as any,
            () => { });
        sinon.stub(debugSession, 'createDebugAdapterTrackerCore');
        return debugSession;
    }

    function collectMessages(debugSession: AspireDebugSession): string[] {
        const messages: string[] = [];
        debugSession.onDidSendMessage((event: OutputEvent) => messages.push(event.body.output));
        return messages;
    }

    test('runs elixir apphost.exs without a debugger when ElixirLS is missing', async () => {
        sinon.stub(vscode.extensions, 'getExtension').returns(undefined);
        const createDebugSessionConfiguration = sinon.stub(debuggerExtensionsModule, 'createDebugSessionConfiguration');
        const fakeAppHostSession = createFakeAppHostSession();
        const spawnElixirAppHost = sinon.stub(elixirLanguageModule, 'spawnElixirAppHost').returns(fakeAppHostSession);

        const debugSession = createDebugSession();
        const messages = collectMessages(debugSession);

        await debugSession.startAppHost(
            '/workspace/apphost.exs',
            ['--', '--urls', 'http://localhost:5000'],
            [],
            true,
            { forceBuild: false });

        // The AppHost runs as a plain child process: no ElixirLS/mix_task debug configuration is
        // ever built for it, with or without ElixirLS installed.
        sinon.assert.notCalled(createDebugSessionConfiguration);
        sinon.assert.calledOnce(spawnElixirAppHost);
        sinon.assert.calledWith(
            spawnElixirAppHost,
            '/workspace/apphost.exs',
            ['--', '--urls', 'http://localhost:5000'],
            [],
            '/workspace',
            sinon.match.string,
            sinon.match.func);
        assert.ok(messages.some(message => message.includes(elixirAppHostRunningWithoutDebugger)));
    });

    test('reports the ElixirLS install hint', async () => {
        sinon.stub(vscode.extensions, 'getExtension').returns(undefined);
        sinon.stub(elixirLanguageModule, 'spawnElixirAppHost').returns(createFakeAppHostSession());

        const debugSession = createDebugSession();
        const messages = collectMessages(debugSession);

        await debugSession.startAppHost('/workspace/apphost.exs', [], [], true, { forceBuild: false });

        assert.ok(messages.some(message => message.includes(elixirLSNotInstalledHint(elixirLSExtensionId))));
    });

    test('does not report the ElixirLS install hint when ElixirLS is installed', async () => {
        sinon.stub(vscode.extensions, 'getExtension').callsFake((extensionId: string) => {
            return extensionId === elixirLSExtensionId ? { id: extensionId } as vscode.Extension<unknown> : undefined;
        });
        sinon.stub(elixirLanguageModule, 'spawnElixirAppHost').returns(createFakeAppHostSession());

        const debugSession = createDebugSession();
        const messages = collectMessages(debugSession);

        await debugSession.startAppHost('/workspace/apphost.exs', [], [], true, { forceBuild: false });

        assert.ok(!messages.some(message => message.includes(elixirLSNotInstalledHint(elixirLSExtensionId))));
        assert.ok(messages.some(message => message.includes(elixirAppHostRunningWithoutDebugger)));
    });
});
