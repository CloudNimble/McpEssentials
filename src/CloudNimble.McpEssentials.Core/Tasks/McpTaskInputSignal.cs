using System.Diagnostics.CodeAnalysis;

namespace CloudNimble.McpEssentials.Core.Tasks
{

    /// <summary>
    /// Wire payload for a resolved-input notification: task id, a newline, then request id.
    /// Native channels (Redis pub/sub, Postgres <c>NOTIFY</c>) publish this string as-is.
    /// </summary>
    /// <remarks>
    /// Split is on the first newline so a request id may contain further newlines.
    /// Task ids from <see cref="McpTaskStoreBase"/> do not contain newlines.
    /// </remarks>
    public static class McpTaskInputSignal
    {

        #region Public Methods

        /// <summary>
        /// Builds the payload published on the native channel and used as the local-echo token.
        /// </summary>
        /// <param name="taskId">The task identifier.</param>
        /// <param name="requestId">The input-request identifier.</param>
        /// <returns><paramref name="taskId"/>, a newline, then <paramref name="requestId"/>.</returns>
        public static string Format(string taskId, string requestId)
            => string.Concat(taskId, "\n", requestId);

        /// <summary>
        /// Reads <see cref="Format"/> back into task id and request id.
        /// </summary>
        /// <param name="payload">The channel payload.</param>
        /// <param name="taskId">The task identifier when parsing succeeds.</param>
        /// <param name="requestId">The input-request identifier when parsing succeeds.</param>
        /// <returns>
        /// <see langword="true"/> when <paramref name="payload"/> has a non-empty task id,
        /// a newline, and a non-whitespace request id.
        /// </returns>
        public static bool TryParse(
            string? payload,
            [NotNullWhen(true)] out string? taskId,
            [NotNullWhen(true)] out string? requestId)
        {
            taskId = null;
            requestId = null;
            if (string.IsNullOrEmpty(payload))
            {
                return false;
            }

            var split = payload.IndexOf('\n');
            if (split <= 0 || split >= payload.Length - 1)
            {
                return false;
            }

            var parsedTaskId = payload[..split];
            var parsedRequestId = payload[(split + 1)..];
            if (string.IsNullOrWhiteSpace(parsedTaskId) || string.IsNullOrWhiteSpace(parsedRequestId))
            {
                return false;
            }

            taskId = parsedTaskId;
            requestId = parsedRequestId;
            return true;
        }

        #endregion

    }

}
