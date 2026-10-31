# Authenticated MCP Task Stores

**Status:** Core in-memory, Entity Framework Core, Redis, and Cosmos providers are implemented. Other provider packages are specified here and are not in this repository yet.

**Audience:** Anyone adding `CloudNimble.McpEssentials.*` packages that persist MCP Tasks (SEP-2663) per logged-in user.

## Why

The MCP C# SDK (`ModelContextProtocol.Extensions.Tasks`) defines `IMcpTaskStore` with no user parameter. `InMemoryMcpTaskStore` is process-wide. Under stateless Streamable HTTP that is correct for a single-purpose server. A generic multi-user MCP server must:

1. Record who started the task.
2. Isolate `tasks/get`, `tasks/update`, and `tasks/cancel` to that user.
3. Still allow the SDK background runner to complete, fail, or attach input requests after the originating HTTP request is gone.

`CloudNimble.McpEssentials.Core` does that in memory. This document specifies how later packages do it on popular databases and caches.

## Shared contract (from Core)

Every store, regardless of engine:

| Rule | Detail |
|---|---|
| Interface | `IMcpTaskStorePlus` : `IMcpTaskStore` (ClaimsPrincipal overloads for client-facing calls) |
| User | `Func<string?>` / `Func<ClaimsPrincipal?>` / constant user id on `McpTaskStoreBase` |
| Isolation helpers | `RequireUserId` / `IsOwnedBy` (ordinal) |
| Lifetime | Singleton. Stateless HTTP creates a new `McpServer` per POST. |
| Create durability | `CreateTaskAsync` does not return until a later `GetTaskAsync` on any node can see the task (SEP-2663 §306). |
| Client-facing | `Create`, `Get`, `SetCancelled`, `ResolveInputRequests` require a current user. Missing user → `McpTaskUserRequiredException`. Wrong user → not found (`null` / `false` / no-op). Never leak existence with 403. |
| Server-facing | `SetCompleted`, `SetFailed`, `SetInputRequests` skip the live-user check. |
| Terminal transitions | Idempotent no-ops on an already-terminal task. |
| `InputResponseReceived` | Raised after the resolved input is persisted. Multi-node stores must fan the event out to the originating process. |
| TTL | Expired tasks disappear on access and via a sweep or native expiry. |
| Thread safety | Every method may run concurrently. |

Claim order for `McpTaskStoreBase.GetUserId` (same as the SDK session identity): `ClaimTypes.NameIdentifier`, `sub`, `ClaimTypes.Upn`, `ClaimTypes.Name`, `IIdentity.Name`. Principal must be authenticated.

## Canonical record

`StoredMcpTask` in Core is the persistence foundation. Construct it from `McpTaskInfo`, call `ToMcpTaskInfo()` to emit the SDK record, and run cancel/complete/fail/input on the object (`TryCancel`, `TryComplete`, `TryFail`, `TrySetInputRequests`, `TryResolveInputRequests`). Durable entities subclass it and add only engine columns (EF: `McpTaskEntity.RowVersion`). `Status` is `McpTaskStatus`; durable stores persist the enum name (EF: `HasConversion<string>()`). Protocol snake_case is the SDK JSON layer.

Providers persist this shape. Names can match the host's conventions; the meaning cannot.

| Field | Type | Notes |
|---|---|---|
| `TaskId` | string, PK | Opaque id from the store. Do not embed the user id in the task id. |
| `OwnerUserId` | string, required | Isolation key. Indexed. Cosmos partition key. |
| `Status` | `McpTaskStatus` | Enum name (`Working`, `InputRequired`, `Completed`, `Cancelled`, `Failed`) |
| `CreatedAt` | DateTimeOffset UTC | |
| `LastUpdatedAt` | DateTimeOffset UTC | |
| `TimeToLive` | TimeSpan? | Duration from create |
| `ExpiresAt` | DateTimeOffset? | Work-window end (`CreatedAt + TimeToLive`) |
| `DiscardAfter` | DateTimeOffset? | Visibility ceiling (`CreatedAt + 2×TimeToLive`), stamped at write |
| `PollIntervalMs` | long? | Default 1000 |
| `StatusMessage` | string? | |
| `ResultJson` | JSON | Terminal `CallToolResult` |
| `ErrorJson` | JSON | JSON-RPC error only |
| `InputRequestsJson` | JSON object | Map of request id → `InputRequest` |

Suggested secondary index: `(OwnerUserId, TaskId)`. Suggested discard index: `DiscardAfter`. Work-window index: `ExpiresAt`.

Do **not** store MCP task rows on domain tables (connections, orders, users). Tasks expire in minutes; domain rows do not.

## Package plan

NuGet package ids drop the `CloudNimble.` prefix (same as EasyAF / ClaudeEssentials). Assembly names stay `CloudNimble.McpEssentials.*`. Each provider references Core. None reference each other.

### 1. `McpEssentials.Core` — done

- `IMcpTaskStorePlus` extends `IMcpTaskStore` with `ClaimsPrincipal` overloads for create/get/cancel/resolve.
- `McpTaskStoreBase` takes a current-user callback, fail-closed identity, and `InputResponseReceived`.
- `StoredMcpTask` is the persistence foundation: settable protocol fields, `McpTaskStatus`, ctor from `McpTaskInfo`, `ToMcpTaskInfo()`, `IsFinished` / `IsActive` / `HasExpired` / `IsExpired` / `IsOwnedBy`, and `Try*` transitions. `DiscardAfter` is stamped at write as `CreatedAt + 2×TimeToLive`. Cached `Info` avoids an allocation on unchanged gets.
- `InMemoryMcpTaskStorePlus` owns `StoredMcpTask` snapshots in a `ConcurrentDictionary`, clones then `Try*` for compare-and-swap, and TTL sweep. It does not wrap the SDK `InMemoryMcpTaskStore`. Unsealed; methods are virtual.
- `WithTasksPlus("userId")` (stdio). HTTP: `AddHttpContextAccessor()` then `WithTasksPlus(sp => sp.GetRequiredService<IHttpContextAccessor>().HttpContext?.User)`. The same `Func<IServiceProvider, ClaimsPrincipal?>` factory exists on `AddInMemoryMcpTaskStorePlus`. Both take `Action<McpTaskStoreOptions>` to set `DefaultTimeToLive` and `DefaultPollIntervalMs` at registration.
- Target frameworks: `net11.0;net10.0;net9.0;net8.0` (same as OData MCP). AOT compatible.

Use for tests, local MCP, and single-instance remote MCP.

### 2. `McpEssentials.EntityFrameworkCore` — implemented

**Engine:** any EF Core database (SQL Server, SQLite, PostgreSQL, Cosmos via EF).

**Types:**

- `McpTaskEntity : StoredMcpTask` mapped to table `McpTasks` (configurable). Adds `RowVersion` only. Status and payloads convert to strings.
- `IEntityTypeConfiguration<McpTaskEntity>` so hosts can call `modelBuilder.ApplyConfiguration`.
- `EFCoreMcpTaskStore<TContext>` where `TContext : DbContext`.
- Extension `AddMcpEssentialsEntityFrameworkTasks<TContext>()` / `WithTasksPlus<TContext>()`, same `McpTaskStoreOptions` as Core.

**Concurrency:** rowversion / `xmin` / ETag on the entity; `SaveChanges` retry on unique-key races for create.

**Events:** in-process `InputResponseReceived` is enough for a single instance. For scale-out, take an optional `IMcpTaskInputSignalBus` (see Cross-node signals). Do not take a dependency on Redis from this package.

**TTL:** `DiscardAfter` is stored as UTC ticks so SQLite and SQL Server can both `ExecuteDelete` (`WHERE DiscardAfter <= now`). Sweep does not load the table. Hosts that override `ShouldDiscard` without changing the stamp should also override `SweepExpiredCoreAsync`.

**Migrations:** ship no database. Host owns migrations. Document the recommended `ToTable("McpTasks")` plus indexes on `OwnerUserId` and `DiscardAfter`.

**Why this package first:** one implementation covers most CloudNimble hosts already on EF Core. Native PostgreSQL / SQL Server packages are only for hosts that do not want EF.

### 3. `McpEssentials.PostgreSQL`

**Engine:** Npgsql, no EF.

**Storage:** one table created by an embedded script the host can run (`EnsureCreatedAsync`).

**Events:** `LISTEN mcp_essentials_tasks` / `NOTIFY` with payload `{ "taskId", "requestId" }`. Each process that holds a waiting `ElicitAsync` subscribes and raises `InputResponseReceived` locally. This is the reason a native Postgres package exists alongside EF Core.

**Durability:** `INSERT ... RETURNING` inside a transaction; `CreateTaskAsync` awaits commit.

**TTL:** `DiscardAfter timestamptz` plus `pg_cron` or a hosted service `DELETE WHERE "DiscardAfter" < now()`.

### 4. `McpEssentials.SqlServer`

**Engine:** `Microsoft.Data.SqlClient`.

**Storage:** `McpTasks` table, `rowversion` for CAS.

**Events:** Service Broker or Query Notifications are heavy. Default: optional `IMcpTaskInputSignalBus`. Document Azure SQL + Service Bus as the scale-out pair.

**Why not fold into EF Core:** hosts that want ADO.NET only, or Dapper, without an EF model.

### 5. `McpEssentials.Redis`

**Engine:** StackExchange.Redis. Shipped.

**Key layout:**

```
{prefix}:task:{taskId}   JSON string  (RedisStoredMcpTask, including OwnerUserId)
{prefix}:task:input      pub/sub      (RedisInputSignal: taskId + requestId)
```

Default prefix is `mcp`. No per-user SET index: SET members do not expire with the task key.

**TTL:** `PEXPIREAT` on the task key from `DiscardAfter` (`CreatedAt + 2×TimeToLive`). Native, no sweeper required. `AutoSweep` defaults to `false`. `SweepExpiredCoreAsync` is a no-op. Per-row get still honors `ShouldDiscard` so a custom clock can drop a key before Redis does. Unlimited TTL leaves the key until a terminal owner get (`ServeAndDrop`) or an explicit delete. `GetTaskAsync` treats a missing key as not found and does not distinguish expiry from isolation.

**Events:** native pub/sub. After a successful `ResolveInputRequests`, the publishing node raises `InputResponseReceived` locally, then `PUBLISH` JSON `{ taskId, requestId }`. Subscribers skip their own echo (token dictionary) and raise a stub `InputResponse` so `ElicitAsync` waiters on other nodes wake.

**Consistency:** `CreateTaskAsync` Lua `EXISTS` then `SET` + `PEXPIREAT`, then `WAIT` when `WaitReplicas` is positive. Standalone Redis leaves wait at 0. Eventually-consistent Redis without wait is not SEP-2663 durable across nodes.

**CAS:** Lua compares the expected JSON, then `SET` + `PEXPIREAT`. Retry on conflict.

### 6. `McpEssentials.DistributedCache`

**Engine:** `IDistributedCache` (Redis, NCache, SQL Server cache, FusionCache, and so on).

**Payload:** serialize the canonical record with source-generated JSON. Key `mcp:task:{taskId}`.

**Events:** `IDistributedCache` has no pub/sub. Require `IMcpTaskInputSignalBus` for multi-node, or document single-node only.

**Use when:** the host already registered `IDistributedCache` and does not want a Redis-specific package. Prefer `McpEssentials.Redis` when the cache is Redis and you need pub/sub.

### 7. `McpEssentials.Cosmos` — implemented

**Engine:** Azure Cosmos DB for NoSQL. Shipped.

**Partition key:** `/ownerUserId`. Point reads are `taskId` + partition `ownerUserId`. Client-facing ops always have the user, so they are in-partition. Server-facing completion has `taskId` only: index container `McpTaskIndex` partitioned by `/id` (the task id) mapping to `ownerUserId`. `CreateTaskAsync` writes the task, then the index, and rolls the task back if the index write fails. Do **not** encode PII in the task id.

**Events:** v1 does not subscribe to the change feed. Optional `IMcpTaskInputSignalBus` (Azure Service Bus is the usual pair) for multi-node `ElicitAsync`.

**TTL:** Cosmos native TTL on the document from `DiscardAfter` (whole seconds from now, at least 1). The index row gets the same `ttl`. Both containers set `DefaultTimeToLive = -1` so per-document TTL applies. `AutoSweep` defaults to `false`. `SweepExpiredCoreAsync` is a no-op.

**Client:** the host owns `CosmosClient` and must pass `CosmosMcpTaskStore.DefaultClientOptions()` so documents serialize with System.Text.Json. The SDK default is Newtonsoft.

**Registration:** `WithCosmosTasks` / `AddMcpEssentialsCosmosTasks` / `UseCosmos`. On first use the store creates the database and containers when missing (default 400 RU/s; set `Throughput` to `null` on serverless and pre-create the containers).

### 8. `McpEssentials.MongoDB`

**Engine:** MongoDB.

**Collection:** `mcpTasks`, unique index on `taskId`, compound index on `{ ownerUserId: 1, taskId: 1 }`.

**CAS:** `findOneAndUpdate` with a filter on current status.

**Events:** change streams on the collection, filtered to input-request updates.

**TTL:** TTL index on `expiresAt`.

### 9. `McpEssentials.Nats`

**Engine:** NATS JetStream Key/Value for the record, Core NATS subject `mcp.essentials.task.input` for signals.

**Use when:** edge / IoT / already-on-NATS CloudNimble hosts.

### 10. `McpEssentials.AzureTables`

**Engine:** Azure Table Storage.

**Keys:** `PartitionKey = OwnerUserId`, `RowKey = TaskId`. Server-facing completion needs a secondary table `McpTaskIndex` with `PartitionKey = TaskId`, `RowKey = OwnerUserId`.

**Events:** none native. Pair with Azure Queue or Service Bus (`IMcpTaskInputSignalBus`).

Cheap point reads; weak queries. Fine for task handles that live minutes.

## Cross-node signals

`IMcpTaskStore.InputResponseReceived` is how a waiting `server.ElicitAsync` / `SampleAsync` wakes up. In-process events do not cross machines.

Core should grow (in a later PR, not required for the in-memory store):

```csharp
public interface IMcpTaskInputSignalBus
{
    Task PublishAsync(string taskId, string requestId, CancellationToken cancellationToken);
    IDisposable Subscribe(Action<string /*taskId*/, string /*requestId*/> handler);
}
```

Providers that have a native bus (Redis pub/sub, Postgres NOTIFY, NATS) implement this internally. Providers that do not (EF Core, Cosmos v1, `IDistributedCache`, Azure Tables) take it as an optional constructor argument. Missing bus + multiple nodes = elicitation hangs until timeout. Document that as a host misconfiguration, not a silent fallback.

Do not put tokens, vault ids, or elicitation secrets on the bus payload. Task id + request id are enough; the store already has the response body.

## Isolation vs session

The SDK's stateful HTTP mode can isolate tasks per MCP session. Stateless mode has no session id. Authenticated stores isolate per **user**, which is the right grain for a multi-user remote MCP API:

- Same user, two browser tabs / two agents: they can see each other's in-flight tasks for that user. That is acceptable; the alternative is session affinity.
- Different users: never.

Do not mix session id and user id in the owner key unless a host explicitly implements a custom accessor that returns `userId + ":" + sessionId`.

## What not to build

- A store that prefixes the task id with the user id (leaks identity, breaks SDK ids).
- `McpTaskId` on domain entities.
- A scoped `IMcpTaskStore` under stateless HTTP.
- Anonymous-by-default isolation in Core (hosts that want it pass a constant user id).
- A provider that reimplements claim lookup. Call `McpTaskStoreBase.GetUserId` or override `RequireUserId`.

## Implementation order

1. Core in-memory store (this repo, done).
2. Entity Framework Core package + SQLite tests (this repo, done).
3. Redis package (TTL + pub/sub) (this repo, done).
4. PostgreSQL native package if EF + Npgsql is not enough for NOTIFY.
5. Cosmos (this repo, done). MongoDB, DistributedCache, Azure Tables, NATS as demand appears.

Each package: same TFMs as Core, MSTest + FluentAssertions, `InternalsVisibleTo` the matching `*.Tests.*` project, DotNetDocs API pages generated from XML comments.

## Key decisions

| Decision | Rationale |
|---|---|
| Own in-memory storage in `InMemoryMcpTaskStorePlus` | Isolation, TTL, CAS, and leak-resistance stay under our control. Durable providers subclass `McpTaskStoreBase` and persist their own snapshots. |
| Current-user callback, fail closed | `IMcpTaskStore` has no user argument; hosts pass `Func<string?>`, a principal factory, or a constant id. Missing user is a host bug. |
| Server-side completion skips live user | Background DI scope has no `HttpContext`. |
| Wrong user → not found | Do not leak that the task exists. |
| Owner not encoded in task id | Opaque SDK ids; owner is metadata. |
| EF Core before native SQL | One package, many databases. Native packages earn their keep with fan-out. |
| Optional signal bus on stores without pub/sub | Do not drag Redis into the EF package. |
