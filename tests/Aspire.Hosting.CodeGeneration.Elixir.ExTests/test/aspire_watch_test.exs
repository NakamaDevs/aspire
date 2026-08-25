defmodule Aspire.WatchTest do
  @moduledoc """
  Tests for `watch.exs`, the AppHost file watcher.

  Each test starts a real `elixir` process, so the timeouts stay short and every process is
  stopped in `on_exit/1`.
  """

  use ExUnit.Case, async: false

  @watch_script Path.expand(
                  "../../../src/Aspire.Hosting.CodeGeneration.Elixir/Resources/watch.exs",
                  __DIR__
                )

  @timeout 10_000

  test "watch restarts child when apphost.exs mtime changes" do
    directory = temporary_directory()

    write(directory, "apphost.exs", """
    IO.puts("APPHOST-STARTED")
    Process.sleep(60_000)
    """)

    port = start_watch(directory, ["apphost.exs"])

    started = await_output(port, "APPHOST-STARTED", @timeout)
    assert started =~ "APPHOST-STARTED"

    # A second granularity mtime needs a stamp that is clearly newer than the snapshot.
    File.touch!(Path.join(directory, "apphost.exs"), System.os_time(:second) + 2)

    output = await_output(port, "APPHOST-STARTED", @timeout, 2, started)
    assert count(output, "APPHOST-STARTED") >= 2
    assert output =~ "[aspire-watch] restarting: apphost.exs"
  end

  test "watch ignores _build and the generated SDK" do
    directory = temporary_directory()

    write(directory, "apphost.exs", """
    IO.puts("APPHOST-STARTED")
    Process.sleep(60_000)
    """)

    File.mkdir_p!(Path.join(directory, "_build"))
    File.mkdir_p!(Path.join([directory, ".aspire", "modules"]))
    write(directory, "_build/stale.ex", "x = 1\n")
    write(directory, ".aspire/modules/aspire_generated.ex", "y = 1\n")

    port = start_watch(directory, ["apphost.exs"])
    started = await_output(port, "APPHOST-STARTED", @timeout)
    assert started =~ "APPHOST-STARTED"

    stamp = System.os_time(:second) + 2
    File.touch!(Path.join(directory, "_build/stale.ex"), stamp)
    File.touch!(Path.join(directory, ".aspire/modules/aspire_generated.ex"), stamp)

    output = collect(port, 2_000, started)
    refute output =~ "[aspire-watch] restarting:"
    assert count(output, "APPHOST-STARTED") == 1
  end

  test "watch forwards child exit code" do
    directory = temporary_directory()

    write(directory, "apphost.exs", """
    IO.puts("APPHOST-FAILED")
    System.halt(3)
    """)

    port = start_watch(directory, ["apphost.exs", "--once"])

    assert {output, 3} = await_exit(port, @timeout)
    assert output =~ "APPHOST-FAILED"
  end

  test "watch keeps waiting after the apphost stops on its own" do
    directory = temporary_directory()

    write(directory, "apphost.exs", """
    IO.puts("APPHOST-STARTED")
    System.halt(3)
    """)

    port = start_watch(directory, ["apphost.exs"])

    output = await_output(port, "Waiting for a file change", @timeout)
    assert output =~ "[aspire-watch] apphost stopped with status 3. Waiting for a file change."
    assert Port.info(port) != nil
  end

  # ── Helpers ───────────────────────────────────────────────────────────────

  defp start_watch(directory, arguments) do
    port =
      Port.open(
        {:spawn_executable, System.find_executable("elixir")},
        [
          :binary,
          :exit_status,
          :hide,
          :stderr_to_stdout,
          cd: directory,
          args: [@watch_script | arguments]
        ]
      )

    on_exit(fn -> stop(port) end)
    port
  end

  defp stop(port) do
    case Port.info(port, :os_pid) do
      {:os_pid, os_pid} ->
        _ = System.cmd("kill", ["-KILL", Integer.to_string(os_pid)], stderr_to_stdout: true)
        :ok

      nil ->
        :ok
    end
  end

  # Reads until the marker appeared `wanted` times, or the timeout elapsed.
  defp await_output(port, marker, timeout, wanted \\ 1, acc \\ "") do
    if count(acc, marker) >= wanted do
      acc
    else
      receive do
        {^port, {:data, data}} -> await_output(port, marker, timeout, wanted, acc <> data)
        {^port, {:exit_status, _status}} -> acc
      after
        timeout ->
          flunk("no #{inspect(marker)} (x#{wanted}) from the watcher in #{timeout}ms: #{acc}")
      end
    end
  end

  defp await_exit(port, timeout, acc \\ "") do
    receive do
      {^port, {:data, data}} -> await_exit(port, timeout, acc <> data)
      {^port, {:exit_status, status}} -> {acc, status}
    after
      timeout -> flunk("the watcher did not stop in #{timeout}ms: #{acc}")
    end
  end

  defp collect(port, timeout, acc) do
    receive do
      {^port, {:data, data}} -> collect(port, timeout, acc <> data)
      {^port, {:exit_status, _status}} -> acc
    after
      timeout -> acc
    end
  end

  defp count(text, marker) do
    text |> String.split(marker) |> length() |> Kernel.-(1)
  end

  defp temporary_directory do
    path =
      Path.join(
        System.tmp_dir!(),
        "aspire_watch_#{System.unique_integer([:positive])}"
      )

    File.mkdir_p!(path)
    on_exit(fn -> File.rm_rf(path) end)
    path
  end

  defp write(directory, name, content) do
    path = Path.join(directory, name)
    File.mkdir_p!(Path.dirname(path))
    File.write!(path, content)
    path
  end
end
