defmodule WorkerTest do
  use ExUnit.Case, async: true

  describe "parse/1" do
    test "uses the defaults when the connection string is missing" do
      assert Worker.parse(nil) == [host: "localhost", port: 6379]
    end

    test "reads the host and the port" do
      assert Worker.parse("cache-host:7001") == [host: "cache-host", port: 7001]
    end

    test "reads the TLS flag" do
      assert Keyword.equal?(
               Worker.parse("localhost:6380,ssl=true"),
               host: "localhost",
               port: 6380,
               ssl: true
             )
    end

    test "reads the password" do
      assert Keyword.equal?(
               Worker.parse("localhost:6379,password=secret"),
               host: "localhost",
               port: 6379,
               password: "secret"
             )
    end
  end
end
