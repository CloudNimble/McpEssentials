using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using CloudNimble.McpEssentials.Cosmos.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.McpEssentials.Tests.Cosmos.Tasks
{

    /// <summary>
    /// Live Cosmos client plus a unique database id. Skips when neither
    /// <c>COSMOS_CONNECTION_STRING</c> nor the emulator on localhost:8081 is available.
    /// </summary>
    internal class CosmosStoreTestContext : IDisposable
    {

        #region Fields

        private int _disposed;

        private static readonly Lazy<bool> Available = new(Probe, LazyThreadSafetyMode.ExecutionAndPublication);

        #endregion

        #region Properties

        /// <summary>
        /// Well-known Cosmos emulator connection string, including the flag that
        /// accepts the emulator's self-signed certificate.
        /// </summary>
        public const string EmulatorConnectionString =
            "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==;DisableServerCertificateValidation=True;";

        public CosmosClient Client { get; }

        public string DatabaseId { get; }

        public string TasksContainerId { get; } = "McpTasks";

        public string IndexContainerId { get; } = "McpTaskIndex";

        public static bool IsAvailable => Available.Value;

        #endregion

        #region Constructors

        private CosmosStoreTestContext(CosmosClient client, string databaseId)
        {
            Client = client;
            DatabaseId = databaseId;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Marks the current test inconclusive when Cosmos is not reachable.
        /// </summary>
        public static void SkipIfUnavailable()
        {
            if (!IsAvailable)
            {
                Assert.Inconclusive(
                    "Azure Cosmos DB is not available. Set COSMOS_CONNECTION_STRING or start the emulator on localhost:8081.");
            }
        }

        /// <summary>
        /// Creates a client that does not talk to Cosmos until the first request.
        /// Safe for constructor and DI tests.
        /// </summary>
        /// <returns>A client pointed at the emulator endpoint.</returns>
        public static CosmosClient CreateDummyClient()
            => new(EmulatorConnectionString, CosmosMcpTaskStore.DefaultClientOptions());

        /// <summary>
        /// Opens a live client. Call <see cref="SkipIfUnavailable"/> first.
        /// </summary>
        /// <returns>A context that owns the client and a unique database id.</returns>
        public static CosmosStoreTestContext Create()
        {
            var connectionString = Environment.GetEnvironmentVariable("COSMOS_CONNECTION_STRING");
            var options = CosmosMcpTaskStore.DefaultClientOptions();
            var client = string.IsNullOrWhiteSpace(connectionString)
                ? new CosmosClient(EmulatorConnectionString, options)
                : new CosmosClient(connectionString, options);
            return new CosmosStoreTestContext(client, "mcpessentials-tests-" + Guid.NewGuid().ToString("N"));
        }

        public InMemoryCosmosTaskGateway Gateway { get; } = new();

        public CosmosMcpTaskStore CreateStore(Func<string?> getCurrentUserId)
        {
            var store = new CosmosMcpTaskStore(() => Client, getCurrentUserId, null, Gateway);
            Apply(store);
            return store;
        }

        public CosmosMcpTaskStore CreateStore(string? userId)
        {
            var store = new CosmosMcpTaskStore(() => Client, () => userId, null, Gateway);
            Apply(store);
            return store;
        }

        public ClockCosmosStore CreateClockStore(string? userId, DateTimeOffset now)
        {
            var store = new ClockCosmosStore(() => Client, userId, now, Gateway);
            Apply(store);
            return store;
        }

        public void Apply(CosmosMcpTaskStore store)
        {
            store.DatabaseId = DatabaseId;
            store.TasksContainerId = TasksContainerId;
            store.IndexContainerId = IndexContainerId;
        }

        public Task DeleteDatabaseAsync()
            => Task.CompletedTask;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1)
            {
                return;
            }

            Client.Dispose();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Returns <see langword="true"/> when a connection string is set or the emulator port is open.
        /// </summary>
        /// <returns>Whether live store tests should run.</returns>
        private static bool Probe()
        {
            if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("COSMOS_CONNECTION_STRING")))
            {
                return true;
            }

            try
            {
                using var tcp = new TcpClient();
                var connect = tcp.ConnectAsync(IPAddress.Loopback, 8081);
                return connect.Wait(TimeSpan.FromMilliseconds(250)) && tcp.Connected;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion

    }

}
