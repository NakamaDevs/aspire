import * as assert from 'assert';
import * as sinon from 'sinon';
import * as vscode from 'vscode';
import { dartCodeExtensionId } from '../capabilities';
import { AspireDebugSession } from '../debugger/AspireDebugSession';
import * as debuggerExtensionsModule from '../debugger/debuggerExtensions';
import { AlreadyStartedResourceDebugSession } from '../debugger/debuggerExtensions';
import * as dartLanguageModule from '../debugger/languages/dart';
import { dartAppHostRunningWithoutDebugger, dartCodeNotInstalledHint } from '../loc/strings';

interface OutputEvent {
    body: { category: string; output: string };
}

suite('Dart AppHost Launch Tests', () => {
    const fakeParentDebugSession = {
        id: 'aspire-session',
        type: 'aspire',
        name: 'Aspire',
        configuration: {
            type: 'aspire',
            request: 'launch',
            name: 'Aspire',
            program: '/workspace/apphost.dart',
            command: 'run',
        },
    };

    teardown(() => sinon.restore());

    function createFakeAppHostSession(): AlreadyStartedResourceDebugSession {
        return {
            id: 'dart-apphost',
            processId: 4242,
            session: { id: 'dart-apphost' } as vscode.DebugSession,
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

    test('runs dart run apphost.dart when Dart-Code is missing', async () => {
        sinon.stub(vscode.extensions, 'getExtension').returns(undefined);
        const createDebugSessionConfiguration = sinon.stub(debuggerExtensionsModule, 'createDebugSessionConfiguration');
        const fakeAppHostSession = createFakeAppHostSession();
        const spawnDartAppHost = sinon.stub(dartLanguageModule, 'spawnDartAppHost').returns(fakeAppHostSession);

        const debugSession = createDebugSession();
        const messages = collectMessages(debugSession);

        await debugSession.startAppHost(
            '/workspace/apphost.dart',
            ['--', '--urls', 'http://localhost:5000'],
            [],
            true,
            { forceBuild: false });

        // The AppHost runs as a plain child process: no Dart-Code debug configuration is ever
        // built for it, with or without Dart-Code installed.
        sinon.assert.notCalled(createDebugSessionConfiguration);
        sinon.assert.calledOnce(spawnDartAppHost);
        sinon.assert.calledWith(
            spawnDartAppHost,
            '/workspace/apphost.dart',
            ['--', '--urls', 'http://localhost:5000'],
            [],
            '/workspace',
            sinon.match.string,
            sinon.match.func);
        assert.ok(messages.some(message => message.includes(dartAppHostRunningWithoutDebugger)));
    });

    test('reports the Dart-Code install hint', async () => {
        sinon.stub(vscode.extensions, 'getExtension').returns(undefined);
        sinon.stub(dartLanguageModule, 'spawnDartAppHost').returns(createFakeAppHostSession());

        const debugSession = createDebugSession();
        const messages = collectMessages(debugSession);

        await debugSession.startAppHost('/workspace/apphost.dart', [], [], true, { forceBuild: false });

        assert.ok(messages.some(message => message.includes(dartCodeNotInstalledHint(dartCodeExtensionId))));
    });

    test('suppresses the hint when installed', async () => {
        sinon.stub(vscode.extensions, 'getExtension').callsFake((extensionId: string) => {
            return extensionId === dartCodeExtensionId ? { id: extensionId } as vscode.Extension<unknown> : undefined;
        });
        sinon.stub(dartLanguageModule, 'spawnDartAppHost').returns(createFakeAppHostSession());

        const debugSession = createDebugSession();
        const messages = collectMessages(debugSession);

        await debugSession.startAppHost('/workspace/apphost.dart', [], [], true, { forceBuild: false });

        assert.ok(!messages.some(message => message.includes(dartCodeNotInstalledHint(dartCodeExtensionId))));
        assert.ok(messages.some(message => message.includes(dartAppHostRunningWithoutDebugger)));
    });
});
