# Redis sample

Authenticated MCP tasks persisted as Redis strings. Same fake `X-User-Id` auth and `HelloWorld` tool as the Basic sample. The only difference is the store.

Requires a Redis server at the connection string in `appsettings.json` (default `localhost:6379`).

## Run

```bash
dotnet run --project samples/CloudNimble.McpEssentials.Samples.Redis -f net11.0
```

MCP is at `http://localhost:5300/`. Send `X-User-Id: alice`.

## What it wires

```csharp
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379"));

builder.Services.AddMcpServer()
    .WithHttpTransport(options => options.Stateless = true)
    .WithTools<HelloWorldTools>()
    .WithRedisTasks(sp => sp.GetRequiredService<IHttpContextAccessor>().HttpContext?.User);
```

See [Redis](https://easyaf.dev/mcpessentials/guides/stores/redis).
