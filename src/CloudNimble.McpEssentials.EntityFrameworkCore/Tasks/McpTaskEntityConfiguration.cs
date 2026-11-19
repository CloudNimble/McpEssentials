using System;
using CloudNimble.McpEssentials.Core.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloudNimble.McpEssentials.EntityFrameworkCore.Tasks
{

    /// <summary>
    /// Maps <see cref="McpTaskEntity"/> to a table (default <c>McpTasks</c>) with owner, work-window,
    /// and discard indexes and an application-managed concurrency token. Protocol enums and payloads
    /// convert to strings. <see cref="StoredMcpTask.DiscardAfter"/> stores UTC ticks so every
    /// provider, including SQLite, can <c>ExecuteDelete</c> on the stamp.
    /// </summary>
    public class McpTaskEntityConfiguration : IEntityTypeConfiguration<McpTaskEntity>
    {

        #region Properties

        /// <summary>
        /// Gets the table name. The default is <c>McpTasks</c>.
        /// </summary>
        public string TableName { get; }

        /// <summary>
        /// Gets the schema, or <see langword="null"/> to use the database default.
        /// </summary>
        public string? Schema { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance that maps to table <c>McpTasks</c>.
        /// </summary>
        public McpTaskEntityConfiguration()
            : this("McpTasks", null)
        {
        }

        /// <summary>
        /// Initializes a new instance that maps to <paramref name="tableName"/> in
        /// <paramref name="schema"/>.
        /// </summary>
        /// <param name="tableName">The table name.</param>
        /// <param name="schema">The schema, or <see langword="null"/> for the database default.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="tableName"/> is <see langword="null"/> or whitespace.
        /// </exception>
        public McpTaskEntityConfiguration(string tableName, string? schema = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
            TableName = tableName;
            Schema = string.IsNullOrWhiteSpace(schema) ? null : schema;
        }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public virtual void Configure(EntityTypeBuilder<McpTaskEntity> builder)
        {
            if (Schema is null)
            {
                builder.ToTable(TableName);
            }
            else
            {
                builder.ToTable(TableName, Schema);
            }

            builder.HasKey(e => e.TaskId);

            builder.Property(e => e.TaskId)
                .HasMaxLength(64)
                .IsRequired();

            builder.Property(e => e.OwnerUserId)
                .HasMaxLength(256)
                .IsRequired();

            builder.Property(e => e.Status)
                .HasMaxLength(32)
                .IsRequired()
                .HasConversion<string>();

            builder.Property(e => e.StatusMessage)
                .HasMaxLength(1024);

            builder.Property(e => e.ResultJson).UsePropertyAccessMode(PropertyAccessMode.Property);
            builder.Property(e => e.ErrorJson).UsePropertyAccessMode(PropertyAccessMode.Property);
            builder.Property(e => e.InputRequestsJson).UsePropertyAccessMode(PropertyAccessMode.Property);

            builder.Ignore(e => e.Result);
            builder.Ignore(e => e.Error);
            builder.Ignore(e => e.InputRequests);
            builder.Ignore(e => e.Info);
            builder.Ignore(e => e.IsFinished);

            builder.Property(e => e.RowVersion)
                .IsConcurrencyToken()
                .IsRequired();

            // SQLite cannot translate DateTimeOffset inequalities. UTC ticks compare everywhere.
            builder.Property(e => e.DiscardAfter)
                .HasConversion(
                    v => v == null ? (long?)null : v.Value.UtcTicks,
                    v => v == null ? null : new DateTimeOffset(v.Value, TimeSpan.Zero));

            builder.HasIndex(e => e.OwnerUserId);
            builder.HasIndex(e => new { e.OwnerUserId, e.TaskId });
            builder.HasIndex(e => e.ExpiresAt);
            builder.HasIndex(e => e.DiscardAfter);
        }

        #endregion

    }

}
