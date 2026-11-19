# McpEssentials.Redis

Authenticated MCP task store backed by Redis. Same per-user isolation as `InMemoryMcpTaskStorePlus`, persisted as strings with native `PEXPIREAT` from `DiscardAfter` and pub/sub for multi-node elicitation.

The host owns `IConnectionMultiplexer`. This package ships the store and registration. Redis expires keys; the request-path sweep is off by default.

## Install

```bash
dotnet add package McpEssentials.Redis
```

## Usage

Register a multiplexer, then the store:

```csharp
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IConnectionMultiplexer>(
    _ => ConnectionMultiplexer.Connect(connectionString));

builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithTools<MyTools>()
    .WithRedisTasks(
        sp => sp.GetRequiredService<IHttpContextAccessor>().HttpContext?.User,
        o =>
        {
            o.DefaultTimeToLive = TimeSpan.FromMinutes(5);
            o.KeyPrefix = "mcp";          // keys: mcp:task:{taskId}
            o.WaitReplicas = 1;           // WAIT after create on a replica set
        });
```

**Stdio / single user**

```csharp
builder.Services.AddSingleton<IConnectionMultiplexer>(
    _ => ConnectionMultiplexer.Connect("localhost:6379"));
builder.Services.AddMcpServer()
    .WithRedisTasks("local-user");
```

**Store only**, without `IMcpServerBuilder`:

```csharp
builder.Services.AddMcpEssentialsRedisTasks(
    sp => sp.GetRequiredService<IHttpContextAccessor>().HttpContext?.User,
    o => o.DefaultTimeToLive = TimeSpan.FromMinutes(5));
```

## Keys and TTL

| Key / channel | Role |
|---|---|
| `{prefix}:task:{taskId}` | JSON snapshot, including `OwnerUserId` |
| `{prefix}:task:input` | Pub/sub for resolved input (`taskId` + `requestId`) |

`DiscardAfter` is stamped at write as `CreatedAt + 2×TimeToLive`. Redis `PEXPIREAT` uses that instant. Unlimited TTL (`null`) leaves the key until a terminal owner get (`ServeAndDrop`) or an explicit delete.

`AutoSweep` defaults to `false`. `SweepExpiredAsync` is a no-op; Redis owns expiry. Per-row get still honors `ShouldDiscard` so a custom clock can drop a key before Redis does. A custom `ShouldDiscard` that disagrees with `DiscardAfter` should also override `SweepExpiredCoreAsync` and skip native expiry.

Replica sets: set `WaitReplicas` so `CreateTaskAsync` does not return until a later get on another node can see the key. Standalone Redis leaves this at 0.

## Isolation

`CreateTaskAsync` stamps `OwnerUserId`. Client-facing get/cancel/resolve require that user. Server-side complete/fail/input-request skip the live-user check. Another user's task is not found. A missing key is not found; get does not distinguish expiry from isolation.

## Documentation

- Docs: https://easyaf.dev/mcpessentials
- Spec: [Authenticated Task Stores](../../specs/authenticated-task-stores.md)
- Source: https://github.com/CloudNimble/McpEssentials

## License

MIT © CloudNimble, Inc.
