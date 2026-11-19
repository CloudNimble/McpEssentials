using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace CloudNimble.McpEssentials.EntityFrameworkCore.Infrastructure
{

    /// <summary>
    /// Debug and service-provider identity for <see cref="McpTasksOptionsExtension"/>.
    /// </summary>
    internal class McpTasksOptionsExtensionInfo : DbContextOptionsExtensionInfo
    {

        #region Properties

        /// <summary>
        /// Gets the parent options extension.
        /// </summary>
        private new McpTasksOptionsExtension Extension
            => (McpTasksOptionsExtension)base.Extension;

        /// <inheritdoc />
        public override bool IsDatabaseProvider => false;

        /// <inheritdoc />
        public override string LogFragment
            => Extension.Schema is null
                ? $"McpTasksTable={Extension.TableName} "
                : $"McpTasksTable={Extension.Schema}.{Extension.TableName} ";

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance for <paramref name="extension"/>.
        /// </summary>
        /// <param name="extension">The parent options extension.</param>
        public McpTasksOptionsExtensionInfo(McpTasksOptionsExtension extension)
            : base(extension)
        {
        }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override int GetServiceProviderHashCode() => 0;

        /// <inheritdoc />
        public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other)
            => other is McpTasksOptionsExtensionInfo;

        /// <inheritdoc />
        public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
        {
            ArgumentNullException.ThrowIfNull(debugInfo);
            debugInfo["McpEssentials:TableName"] = Extension.TableName;
            if (Extension.Schema is { } schema)
            {
                debugInfo["McpEssentials:Schema"] = schema;
            }
        }

        #endregion

    }

}
