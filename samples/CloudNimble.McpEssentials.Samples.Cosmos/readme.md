# Cosmos sample

Authenticated MCP tasks persisted as Azure Cosmos DB for NoSQL documents. Same fake `X-User-Id` auth and `HelloWorld` tool as the Basic sample. The only difference is the store.

Requires Cosmos DB at the connection string in `appsettings.json` (default is the local emulator on `https://localhost:8081/`).

## Run

```bash
dotnet run --project samples/CloudNimble.McpEssentials.Samples.Cosmos -f net11.0
```

MCP is at `http://localhost:5310/`. Send `X-User-Id: alice`.

## What it wires

```csharp
builder.Services.AddSingleton(_ => new CosmosClient(
    builder.Configuration.GetConnectionString("Cosmos") ?? emulatorConnectionString,
    CosmosMcpTaskStore.DefaultClientOptions()));

builder.Services.AddMcpServer()
    .WithHttpTransport(options => options.Stateless = true)
    .WithTools<HelloWorldTools>()
    .WithCosmosTasks(sp => sp.GetRequiredService<IHttpContextAccessor>().HttpContext?.User);
```

Pass `CosmosMcpTaskStore.DefaultClientOptions()` so documents serialize with System.Text.Json.

See [Cosmos DB](https://easyaf.dev/mcpessentials/guides/stores/cosmos).
