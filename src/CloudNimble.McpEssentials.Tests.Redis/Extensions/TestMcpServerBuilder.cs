using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;

namespace CloudNimble.McpEssentials.Tests.Redis.Extensions
{

    /// <summary>
    /// Real <see cref="IMcpServerBuilder"/> used by Breakdance tests.
    /// </summary>
    internal class TestMcpServerBuilder : IMcpServerBuilder
    {

        #region Properties

        public IServiceCollection Services { get; } = new ServiceCollection();

        #endregion

    }

}
