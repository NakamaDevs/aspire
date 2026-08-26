import * as vscode from 'vscode';
import { spawn } from 'child_process';
import { dartCodeExtensionId } from '../../capabilities';
import { AspireResourceExtendedDebugConfiguration, DartLaunchConfiguration, EnvVar, ExecutableLaunchConfiguration, isDartLaunchConfiguration } from "../../dcp/types";
import { dartDisplayName, dartLabel, invalidLaunchConfiguration } from "../../loc/strings";
import { extensionLogOutputChannel } from "../../utils/logging";
import { getEnvironmentForChildProcess, mergeEnvs } from "../../utils/environment";
import { AlreadyStartedResourceDebugSession, ResourceDebuggerExtension } from "../debuggerExtensions";

function asDartConfig(launchConfig: ExecutableLaunchConfiguration): DartLaunchConfiguration {
    if (isDartLaunchConfiguration(launchConfig)) {
        return launchConfig;
    }

    extensionLogOutputChannel.info(`The resource type was not dart for ${JSON.stringify(launchConfig)}`);
    throw new Error(invalidLaunchConfiguration(JSON.stringify(launchConfig)));
}

/**
 * Maps a Dart resource's launch payload to a VS Code debug configuration for the Dart-Code `dart`
 * debug adapter. Dart-Code's launch schema names its fields `program`, `cwd`, `args`, and
 * `toolArgs`; the app host sends the same shape with `tool_args` for the last one, so this only
 * renames that field.
 */
export const dartDebuggerExtension: ResourceDebuggerExtension = {
    resourceType: 'dart',
    debugAdapter: 'dart',
    extensionId: dartCodeExtensionId,
    getDisplayName: (launchConfiguration: ExecutableLaunchConfiguration) => {
        if (isDartLaunchConfiguration(launchConfiguration)) {
            return dartDisplayName(vscode.workspace.asRelativePath(launchConfiguration.program));
        }

        return dartLabel;
    },
    getSupportedFileTypes: () => ['.dart'],
    getProjectFile: (launchConfig) => asDartConfig(launchConfig).cwd,
    createDebugSessionConfigurationCallback: async (launchConfig, _args, _env, _launchOptions, debugConfiguration: AspireResourceExtendedDebugConfiguration): Promise<void> => {
        const config = asDartConfig(launchConfig);

        debugConfiguration.type = 'dart';
        debugConfiguration.request = 'launch';
        debugConfiguration.program = config.program;
        debugConfiguration.cwd = config.cwd;
        debugConfiguration.args = config.args ?? [];
        debugConfiguration.toolArgs = config.tool_args ?? [];

        // prepareDebugSession already set `env` to mergeEnvs(getEnvironmentForChildProcess(), env),
        // the full inherited environment with the resource's own variables layered on top. Dart-Code's
        // `env` field uses the same name/value map shape, so it needs no translation here.
    }
};

/**
 * Runs a Dart AppHost script (`apphost.dart`) as a plain child process, without attaching a
 * debugger.
 *
 * Unlike Elixir's `mix_task` adapter, Dart-Code's `dart` debug adapter has no Mix-project-style
 * restriction that would stop it launching `dart run apphost.dart` directly: passing `program:
 * apphost.dart` and `cwd` would work even when the AppHost is a standalone script. This function
 * still runs it as a plain process rather than under Dart-Code, deliberately keeping parity with
 * {@link spawnElixirAppHost}: the AppHost's own console output already reaches the Aspire debug
 * console through this path, and Dart *resources* the AppHost starts are debugged individually
 * through {@link dartDebuggerExtension} when Dart-Code is installed, so there is little value in
 * the added complexity of a real debug session (breakpoints, stepping) for the AppHost process
 * itself. Revisit this if AppHost-level Dart debugging is requested. See NAK-541.
 */
export function spawnDartAppHost(
    projectFile: string,
    args: string[],
    environment: EnvVar[],
    workingDirectory: string,
    debugSessionId: string,
    onOutput: (output: string, category: 'stdout' | 'stderr') => void
): AlreadyStartedResourceDebugSession {
    // The CLI sends the full command line for the AppHost (e.g. dotnet's
    // ["run", "--no-build", ..., "--", ...appHostArgs]); only the arguments after "--" belong to
    // the AppHost process itself. Mirror the same convention other AppHost languages use for
    // forwarding the AppHost's own arguments.
    const separatorIndex = args.indexOf('--');
    const appHostArgs = separatorIndex >= 0 ? args.slice(separatorIndex + 1) : [];

    const mergedEnv = mergeEnvs(getEnvironmentForChildProcess(), environment);
    const spawnEnv = Object.fromEntries(
        Object.entries(mergedEnv).filter((entry): entry is [string, string] => entry[1] !== undefined)
    );

    const child = spawn('dart', ['run', projectFile, ...appHostArgs], {
        cwd: workingDirectory,
        env: spawnEnv,
    });

    let resolveTermination: (exitCode: number) => void;
    const termination = new Promise<number>(resolve => {
        resolveTermination = resolve;
    });

    child.stdout?.setEncoding('utf8');
    child.stderr?.setEncoding('utf8');
    child.stdout?.on('data', (data: string) => onOutput(data, 'stdout'));
    child.stderr?.on('data', (data: string) => onOutput(data, 'stderr'));
    child.on('error', (error: Error) => {
        extensionLogOutputChannel.error(`Error spawning Dart AppHost process: ${error.message}`);
        onOutput(error.message, 'stderr');
    });
    child.on('close', (code, signal) => {
        resolveTermination(code ?? (signal ? 1 : 0));
    });

    return {
        id: debugSessionId,
        processId: child.pid ?? -1,
        // No real VS Code debug session backs this process, so `session` is a minimal stand-in
        // carrying only the id the rest of AspireDebugSession keys off of.
        session: { id: debugSessionId } as vscode.DebugSession,
        stopSession: () => {
            if (child.exitCode === null && child.signalCode === null) {
                child.kill();
            }

            return Promise.resolve();
        },
        termination
    };
}
