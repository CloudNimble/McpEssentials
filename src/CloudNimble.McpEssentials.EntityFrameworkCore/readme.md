# McpEssentials.EntityFrameworkCore

Authenticated MCP task store backed by Entity Framework Core. Same per-user isolation as `InMemoryAuthenticatedMcpTaskStore`, persisted on any EF Core database.

The host owns the `DbContext`, the connection, and migrations. This package ships the entity, the `IEntityTypeConfiguration`, and a singleton store that opens a scope per operation so a standard scoped `AddDbContext` works.

## Install

```bash
dotnet add package McpEssentials.EntityFrameworkCore
```

## Usage

```csharp
builder.Services.AddHttpContextAccessor();
builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlServer(connectionString));

builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithTools<MyTools>()
    .WithEntityFrameworkTasks<AppDbContext>(
        sp => sp.GetRequiredService<IHttpContextAccessor>().HttpContext?.User,
        o => o.DefaultTimeToLive = TimeSpan.FromMinutes(5));
```

## Map the table

Pick **one**. Both map `McpTaskEntity` onto the same host `DbContext`. Default table `McpTasks`; pass a name and schema to change it (`AddMcpTasksTable("AppTasks", "mcp")`). The host does not need a `DbSet`.

**`OnModelCreating`** — the project that owns `AppDbContext` already references this package. Mapping lives with the rest of the model. `dotnet ef migrations add` from that project sees the table.

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.AddMcpTasksTable();
}
```

**`AddDbContext`** — `AppDbContext` lives in a class library that must stay free of MCP. Only the application project takes this package. Call `AddMcpTasksTable` on the options next to `UseSqlServer`. The mapping runs after `OnModelCreating`. Run migrations with the application as `--startup-project`. A migration from the context project alone will not see `McpTasks` and can emit a drop.

```csharp
builder.Services.AddDbContext<AppDbContext>(o =>
{
    o.UseSqlServer(connectionString);
    o.AddMcpTasksTable();
});
```

The host owns the database.

Single-instance hosts can skip a signal bus. Multi-node hosts that wait on `ElicitAsync` register an `IMcpTaskInputSignalBus` (Redis, Service Bus, …) in DI; the store publishes after a successful resolve and subscribes so the originating process wakes up.

## Isolation

`CreateTaskAsync` stamps `OwnerUserId`. Client-facing get/cancel/resolve require that user. Server-side complete/fail/input-request skip the live-user check. Another user's task is not found.

## Documentation

- Docs: https://easyaf.dev/mcpessentials
- Spec: [Authenticated Task Stores](../../specs/authenticated-task-stores.md)
- Source: https://github.com/CloudNimble/McpEssentials

## License

MIT © CloudNimble, Inc.
