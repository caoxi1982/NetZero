using System.Text.Json.Serialization;

namespace NetZero
{
    public struct CoaNode
    {
        [JsonPropertyName("name")]
        public string Name
        {
            get; set;
        }

        [JsonPropertyName("type")]
        public int Type
        {
            get; set;
        }

        [JsonPropertyName("infoAttributes")]
        public CoaInfoAttribute[] InfoAttributes
        {
            get; set;
        }

        [JsonPropertyName("children")]
        public CoaNode[] Children
        {
            get; set;
        }
    }
}