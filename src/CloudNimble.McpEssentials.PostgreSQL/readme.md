# McpEssentials.PostgreSQL

Authenticated MCP task store backed by PostgreSQL through Npgsql. Same per-user isolation as `InMemoryMcpTaskStorePlus`, persisted as one `McpTasks` table with `LISTEN` / `NOTIFY` for multi-node elicitation.

This package exists **beside** `McpEssentials.EntityFrameworkCore`, not instead of it. Stay on EF + `UseNpgsql` when you already have a `DbContext`. Use this package when the host talks to Postgres with Npgsql or Dapper and wants Postgres itself to wake `ElicitAsync` on other nodes.

The host owns `NpgsqlDataSource`. This package ships the store, the table script, and registration.

## Install

```bash
dotnet add package McpEssentials.PostgreSQL
```

## Usage

```csharp
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton(_ => NpgsqlDataSource.Create(connectionString));

builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithTools<MyTools>()
    .WithPostgreSqlTasks(
        sp => sp.GetRequiredService<IHttpContextAccessor>().HttpContext?.User,
        o =>
        {
            o.DefaultTimeToLive = TimeSpan.FromMinutes(5);
            o.TableName = "McpTasks";
            o.ListenChannel = "mcp_essentials_tasks";
        });
```

**Stdio / single user**

```csharp
builder.Services.AddSingleton(_ => NpgsqlDataSource.Create("Host=localhost;Database=mcpessentials;Username=postgres;Password=postgres"));
builder.Services.AddMcpServer()
    .WithPostgreSqlTasks("local-user");
```

On first use the store creates the table when it is missing (`CREATE TABLE IF NOT EXISTS`). Hosts that own migrations can run the same shape themselves.

## Why native Postgres

| | Entity Framework Core + Npgsql | This package |
|---|---|---|
| Row | Same canonical `StoredMcpTask` columns | Same |
| Host model | Your `DbContext` and migrations | No EF model |
| Elicitation fan-out | Optional `IMcpTaskInputSignalBus` (Redis, Service Bus, …) | `LISTEN` / `NOTIFY` on a dedicated connection |
| Sweep | `ExecuteDelete` on `DiscardAfter` | `DELETE WHERE "DiscardAfter" <= now` |

`LISTEN` / `NOTIFY` is the reason this package exists. Redis pub/sub is the other native channel (`NotifyingMcpTaskStoreBase`). EF does not subscribe to Postgres notifications.

## Isolation and TTL

`CreateTaskAsync` stamps `OwnerUserId` and awaits `INSERT` commit so a later get on any node can see the row. Client-facing get/cancel/resolve require that user. Server-side complete/fail/input-request skip the live-user check. Another user's task is not found.

`DiscardAfter` is `timestamptz`, stamped at write as `CreatedAt + 2×TimeToLive`. `AutoSweep` defaults to **true**. Call `SweepExpiredAsync` from `pg_cron` or a hosted service if you turn AutoSweep off. Compare-and-swap uses an integer `RowVersion`.

## Documentation

- Docs: https://easyaf.dev/mcpessentials/guides/stores/postgresql
- Spec: [Authenticated Task Stores](../../specs/authenticated-task-stores.md)
- Source: https://github.com/CloudNimble/McpEssentials

## License

MIT © CloudNimble, Inc.
