using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CloudNimble.McpEssentials.Tests.AspNetCore
{

    /// <summary>
    /// JSON-RPC helpers for Streamable HTTP MCP at protocol revision 2026-07-28.
    /// </summary>
    public static class McpStreamableHttp
    {

        #region Fields

        /// <summary>
        /// Protocol revision that carries Tasks in per-request <c>_meta</c>.
        /// </summary>
        public const string ProtocolVersion = "2026-07-28";

        /// <summary>
        /// Per-request <c>_meta</c> that opts into <c>io.modelcontextprotocol/tasks</c>.
        /// </summary>
        public const string TasksMetaJson = """{"io.modelcontextprotocol/protocolVersion":"2026-07-28","io.modelcontextprotocol/clientInfo":{"name":"mcpessentials-tests","version":"1.0"},"io.modelcontextprotocol/clientCapabilities":{"extensions":{"io.modelcontextprotocol/tasks":{}}}}""";

        /// <summary>
        /// Per-request <c>_meta</c> that advertises form elicitation.
        /// </summary>
        public const string ElicitationMetaJson = """{"io.modelcontextprotocol/protocolVersion":"2026-07-28","io.modelcontextprotocol/clientInfo":{"name":"mcpessentials-tests","version":"1.0"},"io.modelcontextprotocol/clientCapabilities":{"elicitation":{"form":{}}}}""";

        /// <summary>
        /// Per-request <c>_meta</c> that advertises form elicitation and Tasks.
        /// </summary>
        public const string TasksAndElicitationMetaJson = """{"io.modelcontextprotocol/protocolVersion":"2026-07-28","io.modelcontextprotocol/clientInfo":{"name":"mcpessentials-tests","version":"1.0"},"io.modelcontextprotocol/clientCapabilities":{"elicitation":{"form":{}},"extensions":{"io.modelcontextprotocol/tasks":{}}}}""";

        #endregion

        #region Public Methods

        /// <summary>
        /// Builds JSON content for an MCP POST.
        /// </summary>
        public static StringContent Content(string json) => new(json, Encoding.UTF8, "application/json");

        /// <summary>
        /// Extracts the first JSON object from a JSON or SSE body.
        /// </summary>
        public static string ExtractJson(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                return body ?? string.Empty;
            }

            var start = body.IndexOf('{');
            if (start < 0)
            {
                return body;
            }

            var last = body.LastIndexOf('}');
            return last > start ? body[start..(last + 1)] : body[start..];
        }

        /// <summary>
        /// Posts a 2026-07-28 JSON-RPC request. Method and name travel as headers.
        /// </summary>
        public static async Task<HttpResponseMessage> PostAsync(
            HttpClient client,
            string json,
            string method,
            string? name = null)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentException.ThrowIfNullOrWhiteSpace(json);
            ArgumentException.ThrowIfNullOrWhiteSpace(method);

            using var request = new HttpRequestMessage(HttpMethod.Post, "/")
            {
                Content = Content(json)
            };
            request.Headers.Accept.ParseAdd("application/json, text/event-stream");
            request.Headers.TryAddWithoutValidation("MCP-Protocol-Version", ProtocolVersion);
            request.Headers.TryAddWithoutValidation("Mcp-Method", method);
            if (!string.IsNullOrWhiteSpace(name))
            {
                request.Headers.TryAddWithoutValidation("Mcp-Name", name);
            }

            return await client.SendAsync(request);
        }

        /// <summary>
        /// Posts <c>initialize</c> for the 2025-06-18 handshake used to read server capabilities.
        /// </summary>
        public static async Task<HttpResponseMessage> InitializeAsync(HttpClient client)
        {
            ArgumentNullException.ThrowIfNull(client);

            const string json = """{"jsonrpc":"2.0","id":"init","method":"initialize","params":{"protocolVersion":"2025-06-18","capabilities":{},"clientInfo":{"name":"mcpessentials-tests","version":"1.0"}}}""";
            using var request = new HttpRequestMessage(HttpMethod.Post, "/")
            {
                Content = Content(json)
            };
            request.Headers.Accept.ParseAdd("application/json, text/event-stream");
            return await client.SendAsync(request);
        }

        /// <summary>
        /// The sample tool name. The SDK snake-cases method names unless <c>McpServerTool.Name</c> is set.
        /// </summary>
        public const string HelloWorldToolName = "HelloWorld";

        /// <summary>
        /// Builds a <c>tools/list</c> payload.
        /// </summary>
        public static string ToolsListPayload() =>
            "{\"jsonrpc\":\"2.0\",\"id\":\"1\",\"method\":\"tools/list\",\"params\":{\"_meta\":" + TasksMetaJson + "}}";

        /// <summary>
        /// Builds a task-augmented <c>tools/call</c> payload.
        /// </summary>
        public static string CallToolPayload(string toolName, string argumentsJson = "{}", string? metaJson = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(toolName);
            var args = string.IsNullOrWhiteSpace(argumentsJson) ? "{}" : argumentsJson;
            var meta = string.IsNullOrWhiteSpace(metaJson) ? TasksMetaJson : metaJson;
            return "{\"jsonrpc\":\"2.0\",\"id\":\"1\",\"method\":\"tools/call\",\"params\":{\"name\":\"" + toolName
                + "\",\"arguments\":" + args + ",\"_meta\":" + meta + "}}";
        }

        /// <summary>
        /// Builds a <c>tools/call</c> retry with <c>inputResponses</c>.
        /// </summary>
        public static string CallToolRetryPayload(
            string toolName,
            string inputResponsesJson,
            string argumentsJson = "{}",
            string? metaJson = null,
            string id = "2")
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(toolName);
            ArgumentException.ThrowIfNullOrWhiteSpace(inputResponsesJson);
            var args = string.IsNullOrWhiteSpace(argumentsJson) ? "{}" : argumentsJson;
            var meta = string.IsNullOrWhiteSpace(metaJson) ? ElicitationMetaJson : metaJson;
            var jsonId = JsonSerializer.Serialize(id);
            return "{\"jsonrpc\":\"2.0\",\"id\":" + jsonId + ",\"method\":\"tools/call\",\"params\":{\"name\":\"" + toolName
                + "\",\"arguments\":" + args + ",\"inputResponses\":" + inputResponsesJson
                + ",\"_meta\":" + meta + "}}";
        }

        /// <summary>
        /// Builds a <c>tasks/get</c> payload.
        /// </summary>
        public static string TasksGetPayload(string taskId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(taskId);
            return "{\"jsonrpc\":\"2.0\",\"id\":\"2\",\"method\":\"tasks/get\",\"params\":{\"taskId\":"
                + JsonSerializer.Serialize(taskId) + ",\"_meta\":" + TasksMetaJson + "}}";
        }

        /// <summary>
        /// Reads the HTTP body as text.
        /// </summary>
        public static Task<string> ReadBodyAsync(HttpResponseMessage response)
        {
            ArgumentNullException.ThrowIfNull(response);
            return response.Content.ReadAsStringAsync();
        }

        /// <summary>
        /// Reads <c>result.taskId</c> from a tools/call or tasks/get JSON body.
        /// </summary>
        public static string? ReadTaskId(string body)
        {
            using var document = JsonDocument.Parse(ExtractJson(body));
            if (!document.RootElement.TryGetProperty("result", out var result))
            {
                return null;
            }

            return result.TryGetProperty("taskId", out var taskId) ? taskId.GetString() : null;
        }

        /// <summary>
        /// Reads <c>result.status</c> from a tasks/get JSON body.
        /// </summary>
        public static string? ReadStatus(string body)
        {
            using var document = JsonDocument.Parse(ExtractJson(body));
            if (!document.RootElement.TryGetProperty("result", out var result))
            {
                return null;
            }

            return result.TryGetProperty("status", out var status) ? status.GetString() : null;
        }

        /// <summary>
        /// Returns whether the JSON-RPC body is a successful result (no <c>error</c>).
        /// </summary>
        public static bool IsResult(string body)
        {
            using var document = JsonDocument.Parse(ExtractJson(body));
            return document.RootElement.TryGetProperty("result", out _)
                && !document.RootElement.TryGetProperty("error", out _);
        }

        /// <summary>
        /// Reads <c>result.resultType</c> from a JSON-RPC result body.
        /// </summary>
        public static string? ReadResultType(string body)
        {
            using var document = JsonDocument.Parse(ExtractJson(body));
            if (!document.RootElement.TryGetProperty("result", out var result))
            {
                return null;
            }

            return result.TryGetProperty("resultType", out var resultType) ? resultType.GetString() : null;
        }

        /// <summary>
        /// Returns whether <c>result.inputRequests</c> contains <paramref name="key"/>.
        /// </summary>
        public static bool HasInputRequest(string body, string key)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            using var document = JsonDocument.Parse(ExtractJson(body));
            if (!document.RootElement.TryGetProperty("result", out var result)
                || !result.TryGetProperty("inputRequests", out var requests)
                || requests.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            return requests.TryGetProperty(key, out _);
        }

        /// <summary>
        /// Reads the first text content from a <c>CallToolResult</c>.
        /// </summary>
        public static string? ReadText(string body)
        {
            using var document = JsonDocument.Parse(ExtractJson(body));
            if (!document.RootElement.TryGetProperty("result", out var result)
                || !result.TryGetProperty("content", out var content)
                || content.ValueKind != JsonValueKind.Array
                || content.GetArrayLength() == 0)
            {
                return null;
            }

            var first = content[0];
            return first.TryGetProperty("text", out var text) ? text.GetString() : null;
        }

        /// <summary>
        /// Returns whether the tool result is an execution error.
        /// </summary>
        public static bool IsToolError(string body)
        {
            using var document = JsonDocument.Parse(ExtractJson(body));
            if (!document.RootElement.TryGetProperty("result", out var result))
            {
                return false;
            }

            return result.TryGetProperty("isError", out var isError) && isError.ValueKind == JsonValueKind.True;
        }

        #endregion

    }

}
