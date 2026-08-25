# File watcher for the Aspire Elixir AppHost.
#
# This file is copied verbatim into `.aspire/modules/`. It has no Hex
# dependency and it loads none of the other SDK files.
#
#     elixir .aspire/modules/watch.exs apphost.exs
#
# The watcher starts the AppHost, forwards its output, and polls the mtime of
# every `*.ex` and `*.exs` file below the AppHost directory. A change stops the
# child with SIGTERM and starts it again.
#
# ## Options
#
#   * `--once` — do not restart. The watcher stops with the exit code of the
#     child. The tests use it to check the exit code.
#
# ## Behaviour
#
# The watcher follows `nodemon`. When the AppHost stops on its own, the watcher
# stays alive and waits for the next file change. `_build/`, `deps/` and every
# directory whose name starts with a dot, such as `.aspire/`, are not watched.

defmodule Aspire.Watch do
  @moduledoc false

  @poll_interval 500
  @stop_timeout 5_000
  @ignored_directories ["_build", "deps"]

  def main(argv) do
    {once?, arguments} = take_flag(argv, "--once")

    case arguments do
      [app_host_file | child_arguments] ->
        start(app_host_file, child_arguments, once?)

      [] ->
        IO.puts(:stderr, "usage: elixir watch.exs <apphost.exs> [--once]")
        System.halt(2)
    end
  end

  defp start(app_host_file, child_arguments, once?) do
    elixir = find_elixir()
    app_host_path = Path.expand(app_host_file)
    root = Path.dirname(app_host_path)

    trap_signals()

    state = %{
      elixir: elixir,
      app_host_file: app_host_file,
      app_host_path: app_host_path,
      root: root,
      child_arguments: child_arguments,
      once?: once?,
      port: nil,
      last_status: nil,
      snapshot: snapshot(app_host_path, root)
    }

    schedule_poll()
    loop(%{state | port: spawn_child(state)})
  end

  # ── Loop ──────────────────────────────────────────────────────────────────

  defp loop(state) do
    receive do
      {port, {:data, data}} when port == state.port ->
        IO.binwrite(data)
        loop(state)

      {port, {:exit_status, status}} when port == state.port ->
        child_stopped(state, status)

      :poll ->
        schedule_poll()
        poll(state)

      {:watch_signal, _signal} ->
        shutdown(state)

      _other ->
        loop(state)
    end
  end

  defp poll(state) do
    snapshot = snapshot(state.app_host_path, state.root)

    case first_change(state.snapshot, snapshot) do
      nil ->
        loop(%{state | snapshot: snapshot})

      changed ->
        IO.puts(:stderr, "[aspire-watch] restarting: #{relative(changed, state.root)}")
        state = %{state | snapshot: snapshot}
        state = stop_child(state)
        loop(%{state | port: spawn_child(state)})
    end
  end

  # A child that stops on its own does not stop the watcher. `nodemon` prints
  # "app crashed - waiting for file changes" and waits, and so does this.
  defp child_stopped(%{once?: true}, status), do: System.halt(status)

  defp child_stopped(state, status) do
    IO.puts(
      :stderr,
      "[aspire-watch] apphost stopped with status #{status}. Waiting for a file change."
    )

    loop(%{state | port: nil})
  end

  defp shutdown(state) do
    state = stop_child(state)
    System.halt(exit_code(state))
  end

  defp exit_code(%{last_status: status}) when is_integer(status), do: status
  defp exit_code(_state), do: 0

  # ── Child process ─────────────────────────────────────────────────────────

  defp spawn_child(state) do
    Port.open(
      {:spawn_executable, state.elixir},
      [
        :binary,
        :exit_status,
        :hide,
        :stderr_to_stdout,
        args: [state.app_host_file | state.child_arguments]
      ]
    )
  end

  defp stop_child(%{port: nil} = state), do: state

  defp stop_child(state) do
    case Port.info(state.port, :os_pid) do
      {:os_pid, os_pid} -> signal(os_pid, "TERM")
      nil -> :ok
    end

    status = await_exit(state.port, @stop_timeout)
    %{state | port: nil, last_status: status}
  end

  defp await_exit(port, timeout) do
    receive do
      {^port, {:data, data}} ->
        IO.binwrite(data)
        await_exit(port, timeout)

      {^port, {:exit_status, status}} ->
        status
    after
      timeout ->
        force_stop(port)
        nil
    end
  end

  defp force_stop(port) do
    case Port.info(port, :os_pid) do
      {:os_pid, os_pid} ->
        signal(os_pid, "KILL")

        receive do
          {^port, {:exit_status, status}} -> status
        after
          1_000 -> nil
        end

      nil ->
        nil
    end
  end

  defp signal(os_pid, name) do
    _ = System.cmd("kill", ["-#{name}", Integer.to_string(os_pid)], stderr_to_stdout: true)
    :ok
  rescue
    _exception -> :ok
  end

  # ── File snapshot ─────────────────────────────────────────────────────────

  defp snapshot(app_host_path, root) do
    [app_host_path | watched_files(root)]
    |> Enum.uniq()
    |> Map.new(fn path -> {path, mtime(path)} end)
  end

  defp watched_files(root) do
    root
    |> Path.join("**/*.{ex,exs}")
    |> Path.wildcard()
    |> Enum.reject(&ignored?(&1, root))
  end

  # Path.wildcard/1 does not match a directory whose name starts with a dot, so
  # `.aspire/modules/` never reaches this function. `_build/` and `deps/` do.
  defp ignored?(path, root) do
    path
    |> relative(root)
    |> Path.split()
    |> Enum.any?(fn segment ->
      segment in @ignored_directories or String.starts_with?(segment, ".")
    end)
  end

  defp mtime(path) do
    case File.stat(path, time: :posix) do
      {:ok, %File.Stat{mtime: mtime, size: size}} -> {mtime, size}
      {:error, _reason} -> nil
    end
  end

  defp first_change(previous, current) do
    changed =
      Enum.find(current, fn {path, stamp} -> Map.get(previous, path, :missing) != stamp end)

    case changed do
      {path, _stamp} -> path
      nil -> Enum.find_value(previous, fn {path, _} -> not_in(current, path) end)
    end
  end

  defp not_in(current, path), do: if(Map.has_key?(current, path), do: nil, else: path)

  defp relative(path, root), do: Path.relative_to(path, root)

  # ── Helpers ───────────────────────────────────────────────────────────────

  defp take_flag(argv, flag) do
    {matches, rest} = Enum.split_with(argv, &(&1 == flag))
    {matches != [], rest}
  end

  defp schedule_poll, do: Process.send_after(self(), :poll, @poll_interval)

  # The BEAM keeps SIGINT for its break handler, so only SIGTERM and SIGQUIT can
  # be trapped. Ctrl-C sends SIGINT to the full process group, so the AppHost
  # gets it directly.
  defp trap_signals do
    watcher = self()

    for signal <- [:sigterm, :sigquit] do
      System.trap_signal(signal, fn ->
        send(watcher, {:watch_signal, signal})
        :ok
      end)
    end

    :ok
  end

  defp find_elixir do
    case System.find_executable("elixir") do
      nil ->
        IO.puts(:stderr, "[aspire-watch] cannot find the elixir executable on PATH")
        System.halt(2)

      path ->
        path
    end
  end
end

Aspire.Watch.main(System.argv())
