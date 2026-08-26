// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var builder = DistributedApplication.CreateBuilder(args);

var db = builder.AddPostgres("pg")
                .AddDatabase("appdb");

var cache = builder.AddRedis("cache");

// AddServerpodApp adds the three Serverpod endpoints, and it passes the mode and the
// migration option to `dart run bin/main.dart`.
var api = builder.AddServerpodApp("api", "../serverpod_api/serverpod_api_server")
                 .WithServerpodDatabase(db)
                 .WithServerpodRedis(cache)
                 .WithExternalHttpEndpoints();

// AddJasprApp starts `jaspr serve` on the port that Aspire allocated. `jaspr serve` holds
// its own file watcher, so Aspire does not restart the resource on a change.
builder.AddJasprApp("site", "../jaspr_site")
       .WithExternalHttpEndpoints();

// AddDartApp is the generic path. It starts `dart run bin/main.dart`, and it restarts the
// resource when a Dart file below the application directory changes.
builder.AddDartApp("worker", "../worker")
       .WithReference(api)
       .WaitFor(api);

builder.Build().Run();
