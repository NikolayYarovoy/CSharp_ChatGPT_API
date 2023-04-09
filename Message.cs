using Newtonsoft.Json;

namespace ChatGPTAPI
{
    public class Message : ICloneable
    {
        [JsonProperty("role")]
        public string Role { get; set; } = "";
        [JsonProperty("content")]
        public string Content { get; set; } = "";

        public Message(string role, string content)
        {
            Role = role;
            Content = content;
        }

        public object Clone()
        {
            return new Message(Role, Content);
        }
    }
}
