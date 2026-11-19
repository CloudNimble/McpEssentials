using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using CloudNimble.McpEssentials.Redis.Tasks;
using Garnet;
using Garnet.server;
using StackExchange.Redis;

namespace CloudNimble.McpEssentials.Tests.Redis.Tasks
{

    /// <summary>
    /// In-process Garnet server plus a multiplexer, matching a real Redis host for store tests.
    /// </summary>
    internal class RedisStoreTestContext : IDisposable
    {

        #region Fields

        private int _disposed;

        #endregion

        #region Properties

        public int Port { get; }

        public GarnetServer Server { get; }

        public ConnectionMultiplexer Multiplexer { get; }

        public IDatabase Database => Multiplexer.GetDatabase();

        #endregion

        #region Constructors

        public RedisStoreTestContext()
        {
            Port = GetFreePort();
            var options = new GarnetServerOptions
            {
                QuietMode = true,
                EnableLua = true,
                LuaOptions = new LuaOptions(),
                EndPoints = [new IPEndPoint(IPAddress.Loopback, Port)],
                MemorySize = "16m",
                PageSize = "4k",
                IndexSize = "8m",
                ObjectStoreLogMemorySize = "16m",
                ObjectStorePageSize = "4k",
                ObjectStoreIndexSize = "8m",
                EnableStorageTier = false,
                LogDir = null,
                CheckpointDir = null
            };
            Server = new GarnetServer(options, loggerFactory: null, servers: null, cleanupDir: true);
            Server.Start();

            Multiplexer = ConnectionMultiplexer.Connect($"127.0.0.1:{Port},abortConnect=false,connectTimeout=5000,allowAdmin=true");
            Multiplexer.GetDatabase().Ping();
        }

        #endregion

        #region Public Methods

        public RedisMcpTaskStore CreateStore(Func<string?> getCurrentUserId)
            => new(Multiplexer, getCurrentUserId);

        public RedisMcpTaskStore CreateStore(string? userId)
            => new(Multiplexer, userId);

        public bool KeyExists(string taskId, string prefix = "mcp")
            => Database.KeyExists($"{prefix}:task:{taskId}");

        public TimeSpan? KeyTtl(string taskId, string prefix = "mcp")
            => Database.KeyTimeToLive($"{prefix}:task:{taskId}");

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1)
            {
                return;
            }

            Multiplexer.Dispose();
            Server.Dispose();
        }

        #endregion

        #region Private Methods

        private static int GetFreePort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        #endregion

    }

}
