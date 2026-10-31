# McpEssentials.Cosmos

Authenticated MCP task store backed by Azure Cosmos DB for NoSQL. Same per-user isolation as `InMemoryMcpTaskStorePlus`, with `/ownerUserId` as the partition key and native document TTL.

The host owns `CosmosClient`. This package ships the store and registration. Cosmos expires documents; the request-path sweep is off by default.

## Install

```bash
dotnet add package McpEssentials.Cosmos
```

## Usage

```csharp
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton(_ => new CosmosClient(
    connectionString,
    CosmosMcpTaskStore.DefaultClientOptions()));

builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithTools<MyTools>()
    .WithCosmosTasks(
        sp => sp.GetRequiredService<IHttpContextAccessor>().HttpContext?.User,
        o => o.DefaultTimeToLive = TimeSpan.FromMinutes(5));
```

Pass `CosmosMcpTaskStore.DefaultClientOptions()` so documents serialize with System.Text.Json (`id`, `ttl`, camelCase enum names). The Cosmos SDK default is Newtonsoft.

**Stdio / single user**

```csharp
builder.Services.AddSingleton(_ => new CosmosClient(
    connectionString,
    CosmosMcpTaskStore.DefaultClientOptions()));
builder.Services.AddMcpServer()
    .WithCosmosTasks("local-user");
```

## Containers

| Container | Partition | Role |
|---|---|---|
| `McpTasks` | `/ownerUserId` | Task document, `id` = task id |
| `McpTaskIndex` | `/id` | `taskId` → `ownerUserId` for server-side complete |

`CreateTaskAsync` writes both. Client-facing get/cancel/resolve are in-partition. Background complete reads the index first.

## Documentation

- Docs: https://easyaf.dev/mcpessentials
- Spec: [Authenticated Task Stores](../../specs/authenticated-task-stores.md)
- Source: https://github.com/CloudNimble/McpEssentials

## License

MIT © CloudNimble, Inc.
