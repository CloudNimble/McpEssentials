# Compact XML for form elicitation

**Status:** Implemented alongside the fluent builders.  
**Consumers:** any MCP host. OData MCP wraps the island in `Mcp.ElicitForm` (see Microsoft.OData.Mcp `specs/v3/ELICIT-FORM.md`).

The live object is `FormElicitationBuilder` / `IFormElicitationSource`. Compact XML is the OData `$metadata` helper (`Mcp.ElicitForm`); MCP elicitation on the wire is `Build()` → `ElicitRequestParams`.

| Method | Result |
|--------|--------|
| `Build()` | SDK `ElicitRequestParams` (object graph, `mode: form`); seals the field list |
| `ToInputRequest()` | that graph as JSON (`InputRequest.Params`); cached on the builder |
| `ToJson()` | `ToInputRequest().Params` |
| `ToDefinition()` | immutable `FormDefinition`; cached |
| `ToXml()` | compact `<Form>` island (`XmlWriter`, AOT-safe); cached (`_xml ??=`) |
| `FormElicitationBuilder.ParseXml` / `FromDefinition` | rebuild a builder |

No `XmlSerializer`. No reflection. `XmlWriter` / `XmlReader` over `Form` / `Field` / `Option`. Optional `xmlns` on write; LocalName-only on read. DTD prohibited.

Dialect, attributes, and `Type` spellings: Microsoft.OData.Mcp `specs/v3/ELICIT-FORM.md` §2.

Named registration:

```csharp
services.AddFormElicitation("ship", FormElicitationBuilder.Create("Ship this order").String("trackingNumber").Required());
```

Keyed singleton `IFormElicitationSource`.
