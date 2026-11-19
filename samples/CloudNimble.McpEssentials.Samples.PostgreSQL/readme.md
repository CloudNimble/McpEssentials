# PostgreSQL sample

Authenticated MCP tasks persisted as PostgreSQL rows with `LISTEN` / `NOTIFY`. Same fake `X-User-Id` auth and `HelloWorld` tool as the Basic sample. The only difference is the store.

Requires Postgres at the connection string in `appsettings.json` (default `Host=localhost;Database=mcpessentials`).

This sample uses the **native** package, not EF Core. Stay on the Entity Framework Core sample with `UseNpgsql` when the host already has a `DbContext`. Native Postgres exists so elicitation can wake other nodes through `NOTIFY` without Redis.

## Run

```bash
dotnet run --project samples/CloudNimble.McpEssentials.Samples.PostgreSQL -f net11.0
```

MCP is at `http://localhost:5320/`. Send `X-User-Id: alice`.

## What it wires

```csharp
builder.Services.AddSingleton(_ => NpgsqlDataSource.Create(
    builder.Configuration.GetConnectionString("Postgres") ?? "Host=localhost;Database=mcpessentials;Username=postgres;Password=postgres"));

builder.Services.AddMcpServer()
    .WithHttpTransport(options => options.Stateless = true)
    .WithTools<HelloWorldTools>()
    .WithPostgreSqlTasks(sp => sp.GetRequiredService<IHttpContextAccessor>().HttpContext?.User);
```

See [PostgreSQL](https://easyaf.dev/mcpessentials/guides/stores/postgresql).
