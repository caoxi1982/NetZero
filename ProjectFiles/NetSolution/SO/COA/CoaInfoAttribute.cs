using System.Text.Json.Serialization;

namespace NetZero
{
    public struct CoaInfoAttribute
    {
        [JsonPropertyName("name")]
        public string Name
        {
            get; set;
        }

        [JsonPropertyName("value")]
        public string Value
        {
            get; set;
        }
    }
}
