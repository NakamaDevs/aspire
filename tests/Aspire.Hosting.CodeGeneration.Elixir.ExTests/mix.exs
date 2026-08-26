defmodule Aspire.ExTests.MixProject do
  use Mix.Project

  @resources Path.expand("../../src/Aspire.Hosting.CodeGeneration.Elixir/Resources", __DIR__)

  def project do
    [
      app: :aspire_ex_tests,
      version: "0.1.0",
      elixir: "~> 1.18",
      elixirc_paths: elixirc_paths(Mix.env()),
      start_permanent: false,
      deps: []
    ]
  end

  def application do
    [extra_applications: [:logger]]
  end

  defp elixirc_paths(:test), do: [@resources, "test/support"]
  defp elixirc_paths(_), do: [@resources]
end
