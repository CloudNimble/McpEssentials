using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace CloudNimble.McpEssentials.EntityFrameworkCore.Infrastructure
{

    /// <summary>
    /// Includes the MCP table name and schema in the EF model cache key so two option sets on the
    /// same context type do not share a model.
    /// </summary>
    internal class McpTasksModelCacheKeyFactory : IModelCacheKeyFactory
    {

        #region Public Methods

        /// <inheritdoc />
        public virtual object Create(DbContext context, bool designTime)
        {
            ArgumentNullException.ThrowIfNull(context);
            var extension = context.GetService<IDbContextOptions>().FindExtension<McpTasksOptionsExtension>();
            return (context.GetType(), designTime, extension?.TableName, extension?.Schema);
        }

        #endregion

    }

}
