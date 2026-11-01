# McpEssentials — by CloudNimble

Useful extensions to the [Model Context Protocol C# SDK](https://github.com/modelcontextprotocol/csharp-sdk) that make it easier to build generic MCP servers.

## What it does

The SDK `InMemoryMcpTaskStore` is process-wide. A multi-user remote MCP server needs per-user isolation: the user who started a task is the only one who can poll, update, or cancel it.

**McpEssentials.Core** ships `InMemoryMcpTaskStorePlus`. It records the logged-in user on create and treats everyone else's `tasks/get` / `tasks/update` / `tasks/cancel` as not found.

**McpEssentials.EntityFrameworkCore** persists the same contract on any EF Core database. **McpEssentials.Redis** persists it as Redis strings with native `PEXPIREAT` and pub/sub. **McpEssentials.Cosmos** persists it as Cosmos documents partitioned by owner, with native document TTL. **McpEssentials.PostgreSQL** persists it as Npgsql rows with `LISTEN` / `NOTIFY` (use this instead of EF when you want Postgres itself to wake elicitation). Other durable providers (MongoDB, …) are specified in [`specs/authenticated-task-stores.md`](specs/authenticated-task-stores.md).

## Install

```bash
dotnet add package McpEssentials.Core
dotnet add package McpEssentials.EntityFrameworkCore
dotnet add package McpEssentials.Redis
dotnet add package McpEssentials.Cosmos
dotnet add package McpEssentials.PostgreSQL
```

## Quick start

```csharp
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

builder.Services.AddHttpContextAccessor();

builder.Services.AddMcpServer()
    .WithHttpTransport(o => o.Stateless = true)
    .WithTools<MyTools>()
    .WithTasksPlus(sp => sp.GetRequiredService<IHttpContextAccessor>().HttpContext?.User);
```

## Solution layout

Matches other CloudNimble *Essentials products and the OData MCP repo:

| Path | Role |
|---|---|
| `src/Directory.Build.props` | Pack, test, and TFM defaults |
| `src/global.json` | .NET SDK (same pin as OData MCP) |
| `src/CloudNimble.McpEssentials.Core` | Authenticated in-memory task store |
| `src/CloudNimble.McpEssentials.EntityFrameworkCore` | EF Core authenticated task store |
| `src/CloudNimble.McpEssentials.Redis` | Redis authenticated task store |
| `src/CloudNimble.McpEssentials.Cosmos` | Cosmos DB authenticated task store |
| `src/CloudNimble.McpEssentials.PostgreSQL` | Native PostgreSQL authenticated task store (`LISTEN`/`NOTIFY`) |
| `src/CloudNimble.McpEssentials.Tests.Core` | Breakdance + MSTest + FluentAssertions |
| `src/CloudNimble.McpEssentials.Tests.EntityFrameworkCore` | SQLite in-memory coverage for the EF provider |
| `src/CloudNimble.McpEssentials.Tests.Redis` | In-process Garnet coverage for the Redis provider |
| `src/CloudNimble.McpEssentials.Tests.Cosmos` | Mapping/DI always; live store tests skip without Cosmos |
| `src/CloudNimble.McpEssentials.Tests.PostgreSQL` | In-memory gateway coverage for the native Postgres provider |
| `src/CloudNimble.McpEssentials.Tests.AspNetCore` | Breakdance.AspNetCore host: concurrent users over Streamable HTTP |
| `src/CloudNimble.McpEssentials.Docs` | DotNetDocs / Mintlify |
| `samples/CloudNimble.McpEssentials.Samples.Basic` | In-memory authenticated tasks over Streamable HTTP |
| `samples/CloudNimble.McpEssentials.Samples.EntityFrameworkCore` | Same host, EF Core + SQLite store |
| `samples/CloudNimble.McpEssentials.Samples.Redis` | Same host, Redis store |
| `samples/CloudNimble.McpEssentials.Samples.Cosmos` | Same host, Cosmos DB store |
| `samples/CloudNimble.McpEssentials.Samples.PostgreSQL` | Same host, native PostgreSQL store |
| `specs/authenticated-task-stores.md` | Provider package design |

Target frameworks: `net11.0; net10.0; net9.0; net8.0`.

## Build and release

GitHub Actions (same pattern as EasyAF / ClaudeEssentials):

- PRs: build, test, pack `1.0.0-CI-<timestamp>`
- `dev`: publish `1.0.0-preview.N` to NuGet
- `main`: publish `1.0.x` to NuGet and create GitHub release `v1.0.x`

What to create so 1.0.0 increments correctly is in [`.github/RELEASE.md`](.github/RELEASE.md).

## License

MIT © CloudNimble, Inc.
