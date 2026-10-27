# McpEssentials.Core

Authenticated MCP task stores and shared primitives for generic [Model Context Protocol](https://modelcontextprotocol.io) servers built on the C# SDK.

The SDK ships `InMemoryMcpTaskStore`, which is process-wide: any caller who knows a task id can get, update, or cancel it. `InMemoryAuthenticatedMcpTaskStore` is our own in-memory store. It records the user who created each task, isolates later manipulation to that user, and never wraps the SDK store.

## Install

```bash
dotnet add package McpEssentials.Core
```

## Usage

```csharp
builder.Services.AddHttpContextAccessor();

builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithTools<MyTools>()
    .WithAuthenticatedTasks(
        sp => sp.GetRequiredService<IHttpContextAccessor>().HttpContext?.User,
        o => o.DefaultTimeToLive = TimeSpan.FromMinutes(5));
```

Local / stdio: `.WithAuthenticatedTasks("local-user")`. Tests: `new InMemoryAuthenticatedMcpTaskStore("user-1")` or `store.CreateTaskAsync(principal)`.

HTTP hosts call `AddHttpContextAccessor()` and pass a factory that resolves `IHttpContextAccessor` from the built provider.

`IPerUserMcpTaskStore` extends `IMcpTaskStore` with `ClaimsPrincipal` overloads. `AuthenticatedMcpTaskStoreBase` takes a current-user callback and wires `InputResponseReceived`. `CreateTaskAsync` stamps the current user (NameIdentifier → `sub` → UPN → Name). `tasks/get`, `tasks/update`, and `tasks/cancel` return "not found" for another user's task. Server-side completion (`SetCompletedAsync` / `SetFailedAsync` / `SetInputRequestsAsync`) uses the owner recorded at create, because those calls run on a background DI scope after the original request is gone.

## Durable stores

Core is the in-memory, single-process store. `McpEssentials.EntityFrameworkCore` is the first durable provider. Other packages (PostgreSQL, Redis, …) subclass `AuthenticatedMcpTaskStoreBase` the same way. See [Authenticated Task Stores](../../specs/authenticated-task-stores.md).

## Documentation

- Docs: https://easyaf.dev/mcpessentials
- Source: https://github.com/CloudNimble/McpEssentials

## License

MIT © CloudNimble, Inc.
