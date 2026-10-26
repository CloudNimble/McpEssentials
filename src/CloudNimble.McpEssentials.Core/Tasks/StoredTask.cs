using System;
using ModelContextProtocol.Extensions.Tasks;

namespace CloudNimble.McpEssentials.Core.Tasks
{

    /// <summary>
    /// An immutable in-memory snapshot of a task and its owner.
    /// </summary>
    public class StoredTask
    {

        #region Properties

        /// <summary>
        /// Gets the user recorded when the task was created.
        /// </summary>
        public string OwnerUserId { get; }

        /// <summary>
        /// Gets the protocol-facing snapshot.
        /// </summary>
        public McpTaskInfo Info { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="StoredTask"/> class.
        /// </summary>
        /// <param name="ownerUserId">The user recorded when the task was created.</param>
        /// <param name="info">The protocol-facing snapshot. Must not include the owner.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="ownerUserId"/> is <see langword="null"/> or whitespace.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="info"/> is <see langword="null"/>.</exception>
        public StoredTask(string ownerUserId, McpTaskInfo info)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(ownerUserId);
            ArgumentNullException.ThrowIfNull(info);

            OwnerUserId = ownerUserId;
            Info = info;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Returns a snapshot with the same owner and a new <see cref="Info"/>.
        /// </summary>
        /// <param name="info">The protocol-facing snapshot to store.</param>
        /// <returns>A new snapshot with the same <see cref="OwnerUserId"/>.</returns>
        public virtual StoredTask With(McpTaskInfo info) => new(OwnerUserId, info);

        #endregion

    }

}
