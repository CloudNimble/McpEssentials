# Basic sample

In-memory authenticated MCP tasks over Streamable HTTP. Fake authentication is the `X-User-Id` header. Tasks go through `InMemoryMcpTaskStorePlus`.

## Run

```bash
dotnet run --project samples/CloudNimble.McpEssentials.Samples.Basic -f net11.0
```

MCP is at `http://localhost:5280/` (or `https://localhost:5281/`). Send `X-User-Id: alice`.

## What it wires

```csharp
builder.Services.AddHttpContextAccessor();

builder.Services.AddMcpServer()
    .WithHttpTransport(options => options.Stateless = true)
    .WithTools<HelloWorldTools>()
    .WithTasksPlus(sp => sp.GetRequiredService<IHttpContextAccessor>().HttpContext?.User);
```

## Isolation

1. Call `HelloWorld` as `alice` with the Tasks extension. Keep the task id.
2. `tasks/get` as `alice` returns the task.
3. `tasks/get` as `bob` returns not found.
4. Omit `X-User-Id` and client-facing task operations throw `McpTaskUserRequiredException`.
