# McpEssentials.Core

Authenticated MCP task stores and shared primitives for generic [Model Context Protocol](https://modelcontextprotocol.io) servers built on the C# SDK.

The SDK ships `InMemoryMcpTaskStore`, which is process-wide: any caller who knows a task id can get, update, or cancel it. `InMemoryMcpTaskStorePlus` is our own in-memory store. It records the user who created each task, isolates later manipulation to that user, and never wraps the SDK store.

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
    .WithTasksPlus(
        sp => sp.GetRequiredService<IHttpContextAccessor>().HttpContext?.User,
        o => o.DefaultTimeToLive = TimeSpan.FromMinutes(5));
```

Local / stdio: `.WithTasksPlus("local-user")`. Tests: `new InMemoryMcpTaskStorePlus("user-1")` or `store.CreateTaskAsync(principal)`.

HTTP hosts call `AddHttpContextAccessor()` and pass a factory that resolves `IHttpContextAccessor` from the built provider.

## Form elicitation

Register named forms at startup. Stamp the tool with `[FormElicitation("ship")]` and call `context.RequireForm("ship")`. `WithTasksPlus` keeps that tool on `tools/call` Multi Round-Trip Requests.

```csharp
services.AddFormElicitation("ship",
    FormElicitationBuilder.Create("Ship this order")
        .String("trackingNumber").Required());

[McpServerTool(Name = "ShipOrder")]
[FormElicitation("ship")]
public static string ShipOrder(RequestContext<CallToolRequestParams> context)
{
    var result = context.RequireForm("ship");
    if (!result.IsAccepted)
    {
        return "Shipping cancelled.";
    }

    return $"Shipped as {result.Content!["trackingNumber"].GetString()}.";
}
```

`From<T>()` seeds a CLR type. Keep the builder and add a field per name when the shape comes from metadata. To park a form on a running task yourself: `store.SetInputRequestsAsync(taskId, form)`.

This package ships Roslyn analyzers (`MCPESS001`–`MCPESS005`) that warn on unmappable `From<T>()` properties, colliding wire names, and nested lambdas.

`IMcpTaskStorePlus` extends `IMcpTaskStore` with `ClaimsPrincipal` overloads. `McpTaskStoreBase` takes a current-user callback and wires `InputResponseReceived`. `CreateTaskAsync` stamps the current user (NameIdentifier → `sub` → UPN → Name). `tasks/get`, `tasks/update`, and `tasks/cancel` return "not found" for another user's task. Server-side completion (`SetCompletedAsync` / `SetFailedAsync` / `SetInputRequestsAsync`) uses the owner recorded at create, because those calls run on a background DI scope after the original request is gone.

## Durable stores

Core is the in-memory, single-process store. Durable providers in this repo: Entity Framework Core, Redis, Cosmos, and native PostgreSQL (`LISTEN`/`NOTIFY`). Redis and Postgres subclass `NotifyingMcpTaskStoreBase`. See [Authenticated Task Stores](../../specs/authenticated-task-stores.md).

## Documentation

- Docs: https://easyaf.dev/mcpessentials
- Source: https://github.com/CloudNimble/McpEssentials

## License

MIT © CloudNimble, Inc.
