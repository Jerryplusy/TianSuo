using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace TianSuo.Tool.Protocol
{
    public sealed class Envelope
    {
        public string Type { get; set; }
        public string Name { get; set; }
        public object Data { get; set; }
    }

    public static class EnvelopeBuilder
    {
        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
        };

        public static string Event(string name, object data) => Build(ProtocolNames.EventType, name, data);

        public static string Api(string name, object data) => Build(ProtocolNames.ApiType, name, data);

        private static string Build(string type, string name, object data)
        {
            var envelope = new Envelope { Type = type, Name = name, Data = data };
            return JsonConvert.SerializeObject(envelope, SerializerSettings);
        }
    }
}
