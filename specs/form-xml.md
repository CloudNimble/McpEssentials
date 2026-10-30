# Compact XML for form elicitation

**Status:** Implemented alongside the fluent builders.  
**Consumers:** any MCP host. OData MCP wraps the island in `Mcp.ElicitForm` (see Microsoft.OData.Mcp `specs/v3/ELICIT-FORM.md`).

The live object is `FormElicitation` / `IFormElicitationSource`.

| Method | Result |
|--------|--------|
| `Build()` | SDK `ElicitRequestParams` (object graph, `mode: form`) |
| `ToInputRequest()` | that graph as JSON (`InputRequest.Params`) |
| `ToDefinition()` | immutable `FormDefinition` |
| `ToXml()` | compact `<Form>` island (`XmlWriter`, AOT-safe) |
| `FormElicitation.ParseXml` / `FromDefinition` | rebuild a builder |

No `XmlSerializer`. No reflection. `XmlWriter` / `XmlReader` over `Form` / `Field` / `Option`. Optional `xmlns` on write; LocalName-only on read. DTD prohibited.

Dialect, attributes, and `Type` spellings: Microsoft.OData.Mcp `specs/v3/ELICIT-FORM.md` §2.

Named registration:

```csharp
services.AddFormElicitation("ship", FormElicitation.Create("Ship this order").String("trackingNumber").Required());
```

Keyed singleton `IFormElicitationSource`.
