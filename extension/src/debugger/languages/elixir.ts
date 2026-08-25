import * as vscode from 'vscode';
import { spawn } from 'child_process';
import { elixirLSExtensionId } from '../../capabilities';
import { AspireResourceExtendedDebugConfiguration, ElixirLaunchConfiguration, EnvVar, ExecutableLaunchConfiguration, isElixirLaunchConfiguration } from "../../dcp/types";
import { elixirDisplayName, elixirLabel, invalidLaunchConfiguration } from "../../loc/strings";
import { extensionLogOutputChannel } from "../../utils/logging";
import { getEnvironmentForChildProcess, mergeEnvs } from "../../utils/environment";
import { AlreadyStartedResourceDebugSession, ResourceDebuggerExtension } from "../debuggerExtensions";

function asElixirConfig(launchConfig: ExecutableLaunchConfiguration): ElixirLaunchConfiguration {
    if (isElixirLaunchConfiguration(launchConfig)) {
        return launchConfig;
    }

    extensionLogOutputChannel.info(`The resource type was not elixir for ${JSON.stringify(launchConfig)}`);
    throw new Error(invalidLaunchConfiguration(JSON.stringify(launchConfig)));
}

/**
 * Maps an Elixir resource's launch payload to a VS Code debug configuration for the ElixirLS
 * `mix_task` debug adapter. `mix_task` starts `mix <task> <taskArgs>` itself inside `projectDir`,
 * so Aspire supplies the task and its arguments rather than a `program` to run.
 *
 * This extension is only for Elixir *resources* started under an AppHost, not the AppHost itself:
 * an Elixir AppHost script (`apphost.exs`) is not part of a Mix project, so `mix_task` cannot
 * launch it. See {@link spawnElixirAppHost} for how the AppHost script runs instead.
 */
export const elixirDebuggerExtension: ResourceDebuggerExtension = {
    resourceType: 'elixir',
    debugAdapter: 'mix_task',
    extensionId: elixirLSExtensionId,
    getDisplayName: (launchConfiguration: ExecutableLaunchConfiguration) => {
        if (isElixirLaunchConfiguration(launchConfiguration)) {
            return elixirDisplayName(launchConfiguration.task || vscode.workspace.asRelativePath(launchConfiguration.project_dir));
        }

        return elixirLabel;
    },
    getSupportedFileTypes: () => ['.ex', '.exs'],
    getProjectFile: (launchConfig) => asElixirConfig(launchConfig).project_dir,
    createDebugSessionConfigurationCallback: async (launchConfig, _args, _env, _launchOptions, debugConfiguration: AspireResourceExtendedDebugConfiguration): Promise<void> => {
        const config = asElixirConfig(launchConfig);

        debugConfiguration.type = 'mix_task';
        debugConfiguration.request = 'launch';
        debugConfiguration.projectDir = config.project_dir;
        debugConfiguration.task = config.task;
        debugConfiguration.taskArgs = config.task_args ?? [];
        debugConfiguration.startApps = true;
        debugConfiguration.requireFiles = [];

        if (config.working_directory) {
            debugConfiguration.cwd = config.working_directory;
        }

        // prepareDebugSession already set `env` to mergeEnvs(getEnvironmentForChildProcess(), env),
        // the full inherited environment with the resource's own variables layered on top. mix_env
        // is not part of that resource environment, so it is layered on top here instead.
        if (config.mix_env) {
            debugConfiguration.env = {
                ...(debugConfiguration.env as Record<string, string | undefined> ?? {}),
                MIX_ENV: config.mix_env
            };
        }
    }
};

/**
 * Runs an Elixir AppHost script (`apphost.exs`) as a plain child process, without attaching a
 * debugger.
 *
 * ElixirLS's only debug adapter, `mix_task`, starts a debug session by running
 * `mix <task> <taskArgs>` inside a Mix project directory (one containing `mix.exs`). The Aspire
 * Elixir AppHost is a standalone script run with `elixir apphost.exs`, not a Mix project, so
 * `mix_task` cannot launch it — installing ElixirLS would not change that. Until ElixirLS (or
 * another adapter) supports attaching to a plain `elixir` invocation, the AppHost process runs
 * unmodified and its output is forwarded to the Aspire debug console like any other child process.
 *
 * Elixir *resources* started by the AppHost are unaffected: {@link elixirDebuggerExtension} still
 * debugs them via `mix_task` when ElixirLS is installed, because those resources do run through
 * `mix`.
 */
export function spawnElixirAppHost(
    projectFile: string,
    args: string[],
    environment: EnvVar[],
    workingDirectory: string,
    debugSessionId: string,
    onOutput: (output: string, category: 'stdout' | 'stderr') => void
): AlreadyStartedResourceDebugSession {
    // The CLI sends the full command line for the AppHost (e.g. dotnet's
    // ["run", "--no-build", ..., "--", ...appHostArgs]); only the arguments after "--" belong to
    // the AppHost process itself. Elixir's runtime spec has no flags between the command and the
    // AppHost file (`elixir {appHostFile}`), so mirror the same convention other AppHost languages
    // use for forwarding the AppHost's own arguments.
    const separatorIndex = args.indexOf('--');
    const appHostArgs = separatorIndex >= 0 ? args.slice(separatorIndex + 1) : [];

    const mergedEnv = mergeEnvs(getEnvironmentForChildProcess(), environment);
    const spawnEnv = Object.fromEntries(
        Object.entries(mergedEnv).filter((entry): entry is [string, string] => entry[1] !== undefined)
    );

    const child = spawn('elixir', [projectFile, ...appHostArgs], {
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
        extensionLogOutputChannel.error(`Error spawning Elixir AppHost process: ${error.message}`);
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
