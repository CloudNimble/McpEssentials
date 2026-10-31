# Samples

Each sample is a standalone Streamable HTTP MCP host. Same fake `X-User-Id` authentication and `HelloWorld` tool. The store is the only thing that changes.

| Project | Store | URL |
|---|---|---|
| [Basic](CloudNimble.McpEssentials.Samples.Basic) | In-memory | `http://localhost:5280/` |
| [Entity Framework Core](CloudNimble.McpEssentials.Samples.EntityFrameworkCore) | SQLite via EF | `http://localhost:5290/` |
| [Redis](CloudNimble.McpEssentials.Samples.Redis) | Redis | `http://localhost:5300/` |
| [Cosmos](CloudNimble.McpEssentials.Samples.Cosmos) | Azure Cosmos DB | `http://localhost:5310/` |

Send `X-User-Id: alice` on every request. Clients that opt into MCP Tasks get a trackable task owned by that user.
