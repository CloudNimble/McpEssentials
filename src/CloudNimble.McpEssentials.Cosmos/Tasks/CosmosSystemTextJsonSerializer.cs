using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Azure.Cosmos;

namespace CloudNimble.McpEssentials.Cosmos.Tasks
{

    /// <summary>
    /// Cosmos serializer that uses source-generated <see cref="CosmosMcpTasksJsonContext"/>
    /// with a reflection fallback for types the context does not list.
    /// </summary>
    public class CosmosSystemTextJsonSerializer : CosmosSerializer
    {

        #region Fields

        private readonly JsonSerializerOptions _options;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance using <see cref="CreateOptions"/>.
        /// </summary>
        public CosmosSystemTextJsonSerializer()
        {
            _options = CreateOptions();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Builds the JSON options passed to Cosmos and used by unit tests.
        /// </summary>
        /// <returns>Camel-case options with the source-generated context first.</returns>
        public static JsonSerializerOptions CreateOptions()
        {
            var options = new JsonSerializerOptions(CosmosMcpTasksJsonContext.Default.Options)
            {
                TypeInfoResolver = JsonTypeInfoResolver.Combine(
                    CosmosMcpTasksJsonContext.Default,
                    new DefaultJsonTypeInfoResolver())
            };
            return options;
        }

        /// <inheritdoc />
        public override T FromStream<T>(Stream stream)
        {
            using (stream)
            {
                if (stream.CanSeek && stream.Length == 0)
                {
                    return default!;
                }

                return JsonSerializer.Deserialize<T>(stream, _options)!;
            }
        }

        /// <inheritdoc />
        public override Stream ToStream<T>(T input)
        {
            var stream = new MemoryStream();
            JsonSerializer.Serialize(stream, input, _options);
            stream.Position = 0;
            return stream;
        }

        #endregion

    }

}
