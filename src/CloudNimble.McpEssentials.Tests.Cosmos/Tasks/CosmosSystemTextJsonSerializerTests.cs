using CloudNimble.Breakdance.Extensions.MSTest2;
using CloudNimble.McpEssentials.Core.Tasks;
using CloudNimble.McpEssentials.Cosmos.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ModelContextProtocol.Protocol;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

namespace CloudNimble.McpEssentials.Tests.Cosmos.Tasks
{

    /// <summary>
    /// Coverage for the Cosmos System.Text.Json serializer without a live account.
    /// </summary>
    [TestClass]
    public class CosmosSystemTextJsonSerializerTests : BreakdanceMSTestBase
    {

        #region Public Methods

        [TestMethod]
        public void RoundTrip_TaskDocument()
        {
            var serializer = new CosmosSystemTextJsonSerializer();
            var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var stored = new StoredMcpTask("alice", "abc", now, TimeSpan.FromSeconds(10), 1000);
            var document = CosmosStoredMcpTask.From(stored, now);

            using var stream = serializer.ToStream(document);
            var roundTripped = serializer.FromStream<CosmosStoredMcpTask>(stream);

            roundTripped.Id.Should().Be("abc");
            roundTripped.OwnerUserId.Should().Be("alice");
        }

        [TestMethod]
        public void FromStream_WhenEmpty_ReturnsDefault()
        {
            var serializer = new CosmosSystemTextJsonSerializer();
            using var stream = new MemoryStream();

            serializer.FromStream<CosmosStoredMcpTask>(stream).Should().BeNull();
        }

        [TestMethod]
        public void Index_GettersAndSetters()
        {
            var index = new CosmosTaskIndex
            {
                Id = "abc",
                OwnerUserId = "alice",
                TimeToLiveSeconds = 20
            };

            index.Id.Should().Be("abc");
            index.OwnerUserId.Should().Be("alice");
            index.TimeToLiveSeconds.Should().Be(20);
        }

        [TestMethod]
        public void From_CopiesNonDictionaryInputRequests()
        {
            var now = DateTimeOffset.UtcNow;
            var inner = new Dictionary<string, InputRequest>
            {
                ["r1"] = InputRequest.ForElicitation(new ElicitRequestParams { Message = "Need input" })
            };
            var stored = new StoredMcpTask("alice", "abc", now, null, 1000);
            stored.TrySetInputRequests(inner, now);
            stored.InputRequests = new ReadOnlyDictionary<string, InputRequest>(inner);

            var document = CosmosStoredMcpTask.From(stored, now);
            document.InputRequests.Should().ContainKey("r1");
        }

        #endregion

    }

}
