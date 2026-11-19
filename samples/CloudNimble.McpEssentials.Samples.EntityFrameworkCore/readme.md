# Entity Framework Core sample

Authenticated MCP tasks persisted with EF Core on a local SQLite file. Same fake `X-User-Id` auth and `HelloWorld` tool as the Basic sample. The only difference is the store.

## Run

```bash
dotnet run --project samples/CloudNimble.McpEssentials.Samples.EntityFrameworkCore -f net11.0
```

MCP is at `http://localhost:5290/`. Send `X-User-Id: alice`. SQLite file `mcp-tasks.db` is created next to the app.

## What it wires

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=mcp-tasks.db"));

builder.Services.AddMcpServer()
    .WithHttpTransport(options => options.Stateless = true)
    .WithTools<HelloWorldTools>()
    .WithTasksPlus<AppDbContext>(sp => sp.GetRequiredService<IHttpContextAccessor>().HttpContext?.User);
```

`AppDbContext.OnModelCreating` calls `modelBuilder.AddMcpTasksTable()`. Startup calls `EnsureCreated()` so you do not need a migration for the sample.

Swap `UseSqlite` for `UseSqlServer` (or any other EF provider) in a real host. See [Entity Framework Core](https://easyaf.dev/mcpessentials/guides/stores/entity-framework-core).
