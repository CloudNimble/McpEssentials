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

Map the table in your context:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.AddMcpTasksTable();
    // or: modelBuilder.AddMcpTasksTable("AppTasks", "mcp");
}
```

Then add a migration. This package does not ship a database.

Single-instance hosts can skip a signal bus. Multi-node hosts that wait on `ElicitAsync` register an `IMcpTaskInputSignalBus` (Redis, Service Bus, …) in DI; the store publishes after a successful resolve and subscribes so the originating process wakes up.

## Isolation

`CreateTaskAsync` stamps `OwnerUserId`. Client-facing get/cancel/resolve require that user. Server-side complete/fail/input-request skip the live-user check. Another user's task is not found.

## Documentation

- Docs: https://easyaf.dev/mcpessentials
- Spec: [Authenticated Task Stores](../../specs/authenticated-task-stores.md)
- Source: https://github.com/CloudNimble/McpEssentials

## License

MIT © CloudNimble, Inc.
