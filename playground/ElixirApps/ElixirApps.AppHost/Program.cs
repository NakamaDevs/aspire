// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var builder = DistributedApplication.CreateBuilder(args);

var db = builder.AddPostgres("pg")
                .AddDatabase("appdb");

var cache = builder.AddRedis("cache");

builder.AddPhoenixApp("web", "../phoenix_web")
       .WithEctoDatabase(db)
       .WithEctoMigrate()
       .WithExternalHttpEndpoints();

builder.AddElixirApp("worker", "../worker")
       .WithReference(cache)
       .WaitFor(cache);

builder.Build().Run();
