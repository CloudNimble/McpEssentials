# Authenticated MCP sample

A Streamable HTTP MCP server. Fake authentication is the `X-User-Id` header. Tasks go through `InMemoryMcpTaskStorePlus`, so the user who started a task is the only one who can poll or cancel it.

## Run

```bash
dotnet run --project samples/CloudNimble.McpEssentials.Samples.Web -f net11.0
```

MCP is at `http://localhost:5280/` (or `https://localhost:5281/`). Point a harness at that URL and send `X-User-Id: alice`.

## What it wires

```csharp
builder.Services.AddHttpContextAccessor();

builder.Services.AddMcpServer()
    .WithHttpTransport(options => options.Stateless = true)
    .WithTools<HelloWorldTools>()
    .WithTasksPlus(sp => sp.GetRequiredService<IHttpContextAccessor>().HttpContext?.User);
```

`AddHttpContextAccessor()` is the platform registration. The store factory pulls `IHttpContextAccessor` from the built provider on each client-facing task call.

## Tool

`HelloWorld` injects `ClaimsPrincipal`, greets that user, and returns `Task<string>`. Clients that opt into `io.modelcontextprotocol/tasks` get a task id; `tasks/get` / `tasks/cancel` as a different `X-User-Id` look like not found.

## Isolation

1. Call `HelloWorld` as `alice` with the Tasks extension. Keep the task id.
2. `tasks/get` as `alice` returns the task.
3. `tasks/get` as `bob` returns not found.
4. Omit `X-User-Id` and client-facing task operations throw `McpTaskUserRequiredException`.
