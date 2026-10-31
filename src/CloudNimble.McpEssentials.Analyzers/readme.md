# McpEssentials.Analyzers

Roslyn analyzers for MCP form elicitation. Docs: [Analyzers](https://easyaf.dev/mcpessentials/guides/analyzers). `McpEssentials.Core` already ships this assembly in the nupkg, so `dotnet add package McpEssentials.Core` is enough.

| Id | Severity | When |
|---|---|---|
| MCPESS001 | Warning | `From<T>()` skips a public property the form cannot represent |
| MCPESS002 | Error | Two properties camelCase to the same wire name |
| MCPESS003 | Error | Expression is not a simple property access (`x => x.Address.City`) |
| MCPESS004 | Info | `Exclude` of a property that was never going to be a field |
| MCPESS005 | Warning | `From<T>()` on a built-in or enum instead of a form type |

## License

MIT © CloudNimble, Inc.
