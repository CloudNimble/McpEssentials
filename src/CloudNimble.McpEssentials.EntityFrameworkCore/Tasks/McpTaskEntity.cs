using System;
using CloudNimble.McpEssentials.Core.Tasks;
using ModelContextProtocol.Extensions.Tasks;

namespace CloudNimble.McpEssentials.EntityFrameworkCore.Tasks
{

    /// <summary>
    /// Persisted MCP task row. Inherits protocol fields from <see cref="StoredMcpTask"/> and adds
    /// an application-managed concurrency token plus JSON columns for payloads. Map with
    /// <c>AddMcpTasksTable</c> (default table <c>McpTasks</c>). Do not store these rows
    /// on domain tables; tasks expire in minutes.
    /// </summary>
    public class McpTaskEntity : StoredMcpTask
    {

        #region Fields

        private string? _cachedResultJson;
        private string? _cachedErrorJson;
        private string? _cachedInputRequestsJson;
        private bool _resultJsonCached;
        private bool _errorJsonCached;
        private bool _inputRequestsJsonCached;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the application-managed concurrency token. SQL Server hosts may remap this
        /// as <c>rowversion</c> in their own configuration.
        /// </summary>
        public byte[] RowVersion { get; set; } = [];

        /// <summary>
        /// Gets or sets the terminal result as JSON. Backs <see cref="StoredMcpTask.Result"/>.
        /// </summary>
        public string? ResultJson
        {
            get
            {
                if (!_resultJsonCached)
                {
                    _cachedResultJson = ToJson(Result);
                    _resultJsonCached = true;
                }

                return _cachedResultJson;
            }
            set
            {
                _cachedResultJson = value;
                _resultJsonCached = true;
                Result = ParseElement(value);
            }
        }

        /// <summary>
        /// Gets or sets the JSON-RPC error as JSON. Backs <see cref="StoredMcpTask.Error"/>.
        /// </summary>
        public string? ErrorJson
        {
            get
            {
                if (!_errorJsonCached)
                {
                    _cachedErrorJson = ToJson(Error);
                    _errorJsonCached = true;
                }

                return _cachedErrorJson;
            }
            set
            {
                _cachedErrorJson = value;
                _errorJsonCached = true;
                Error = ParseElement(value);
            }
        }

        /// <summary>
        /// Gets or sets the outstanding input-request map as JSON. Backs <see cref="StoredMcpTask.InputRequests"/>.
        /// </summary>
        public string? InputRequestsJson
        {
            get
            {
                if (!_inputRequestsJsonCached)
                {
                    _cachedInputRequestsJson = ToInputRequestsJson(InputRequests);
                    _inputRequestsJsonCached = true;
                }

                return _cachedInputRequestsJson;
            }
            set
            {
                _cachedInputRequestsJson = value;
                _inputRequestsJsonCached = true;
                InputRequests = ParseInputRequests(value);
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes an empty row for EF Core materialization.
        /// </summary>
        public McpTaskEntity()
        {
        }

        /// <summary>
        /// Initializes a new row from a protocol snapshot.
        /// </summary>
        /// <param name="ownerUserId">The user recorded when the task was created.</param>
        /// <param name="info">The protocol-facing snapshot. Must not include the owner.</param>
        public McpTaskEntity(string ownerUserId, McpTaskInfo info)
            : base(ownerUserId, info)
        {
        }

        /// <summary>
        /// Initializes a new working row owned by <paramref name="ownerUserId"/>.
        /// </summary>
        public McpTaskEntity(string ownerUserId, string taskId, DateTimeOffset createdAt, TimeSpan? timeToLive, long pollIntervalMs)
            : base(ownerUserId, taskId, createdAt, timeToLive, pollIntervalMs)
        {
        }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override StoredMcpTask Clone()
        {
            var clone = (McpTaskEntity)MemberwiseClone();
            if (RowVersion is { Length: > 0 })
            {
                clone.RowVersion = (byte[])RowVersion.Clone();
            }

            return clone;
        }

        #endregion

        #region Protected Methods

        /// <inheritdoc />
        protected override void InvalidateInfo()
        {
            base.InvalidateInfo();
            _resultJsonCached = false;
            _errorJsonCached = false;
            _inputRequestsJsonCached = false;
        }

        #endregion

    }

}
