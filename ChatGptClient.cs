using Newtonsoft.Json;
using System.Net.Http.Json;
using System.Text;

namespace ChatGPTAPI
{
    /// <summary>
    /// The class responsible for the chat conversation
    /// </summary>
    public class ChatGptClient
    {
        #region Service fields
        /// <summary>API keys to auth</summary>
        [JsonIgnore]
        public string ApiKey { get; }
        /// <summary>ChatGpt API page URL</summary>
        [JsonIgnore]
        public string EndPoint { get; }

        /// <summary>Flag indicating whether system messages can be added to the message list</summary>
        [JsonIgnore]
        bool CanAddSystemMessage { get; set; }
        /// <summary>Flag indicating whether user messages can be added to the message list</summary>
        [JsonIgnore]
        bool CanAddUserMessage { get; set; }
        /// <summary>Flag indicating whether it is possible to send a request to the API</summary>
        [JsonIgnore]
        bool CanSendRequest { get; set; }
        /// <summary>List of ChatGPT responses from which you need to choose the appropriate one</summary>
        [JsonIgnore]
        List<Message> MessagesInQueue { get; set; }
        #endregion

        #region Request fields

        //Detailed description at the link: https://platform.openai.com/docs/api-reference/chat/create
        [JsonProperty("model")]
        public string Model { get; }
        [JsonProperty("messages")]
        List<Message> Messages { get; }
        [JsonProperty("temperature")]
        double Temperature { get; set; }
        [JsonProperty("top_p")]
        double TopP { get; set; }
        [JsonProperty("n")]
        int N { get; set; }
        [JsonProperty("stop")]
        List<string>? Stop { get; set; }
        [JsonProperty("max_tokens")]
        int? MaxTokens { get; set; }
        [JsonProperty("presence_penalty")]
        double PresencePenalty { get; set; }
        [JsonProperty("frequency_penalty")]
        double FrequencyFenalty { get; set; }
        [JsonProperty("logit_bias")]
        Dictionary<string, double>? LogitBias { get; set; }
        [JsonProperty("user")]
        string? User { get; set; }


        #endregion

        #region Nested classes

        class SimplifiedModelResponse
        {
            [JsonProperty("data")]
            public List<SimplifiedModel>? Data { get; set; }
        }

        class SimplifiedModel
        {
            [JsonProperty("id")]
            public string? Id { get; set; }
            [JsonProperty("object")]
            string? Object { get; set; }
            [JsonProperty("owned_by")]
            string? OwnedBy { get; set; }
        }

        class ResponseData
        {
            [JsonProperty("id")]
            public string Id { get; set; } = "";
            [JsonProperty("object")]
            public string Object { get; set; } = "";
            [JsonProperty("created")]
            public ulong Created { get; set; }
            [JsonProperty("choices")]
            public List<Choice> Choices { get; set; } = new();
            [JsonProperty("usage")]
            public Usage Usage { get; set; } = new();
        }

        class Choice
        {
            [JsonProperty("index")]
            public int Index { get; set; }
            [JsonProperty("message")]
            public Message Message { get; set; } = new Message("","");
            [JsonProperty("finish_reason")]
            public string FinishReason { get; set; } = "";
        }

        class Usage
        {
            [JsonProperty("prompt_tokens")]
            public int PromptTokens { get; set; }
            [JsonProperty("completion_tokens")]
            public int CompletionTokens { get; set; }
            [JsonProperty("total_tokens")]
            public int TotalTokens { get; set; }
        }

        #endregion

        #region Client creation 

        
        ChatGptClient(string apiKey, string model, string endPoint = "https://api.openai.com/v1/chat/completions", double temperature = 1,
            double top_p = 1, int n = 1, List<string>? stop = null, int? maxTokens = null, double presencePenalty = 0,
            double frequencyFenalty = 0, Dictionary<string, double>? logitBias = null, string? user = null)
        {
            ApiKey = apiKey;
            EndPoint = endPoint;
            Model = model;
            Temperature = temperature;
            TopP = top_p;
            N = n;
            Stop = stop;
            MaxTokens = maxTokens;
            PresencePenalty = presencePenalty;
            FrequencyFenalty = frequencyFenalty;
            LogitBias = logitBias;
            User = user;

            Messages = new List<Message>();
            MessagesInQueue = new List<Message>();
            CanAddSystemMessage = true;
            CanAddUserMessage = true;
            CanSendRequest = false;
        }

        public static async Task<ChatGptClient> CreateAsync(string apiKey, string model, string endPoint = "https://api.openai.com/v1/chat/completions", double temperature = 1,
            double top_p = 1, int n = 1, List<string>? stop = null, int? maxTokens = null, double presencePenalty = 0,
            double frequencyFenalty = 0, Dictionary<string, double>? logitBias = null, string? user = null)
        {
            ChatGptClient result = new ChatGptClient(apiKey, model, endPoint, temperature, top_p, n, stop, maxTokens, presencePenalty, frequencyFenalty, logitBias, user);

            await ServerVerification(result);

            if(temperature < 0 || temperature > 2)
                throw new ArgumentOutOfRangeException("Temperature must be between 0 and 2", "temperature");

            if (top_p < 0 || top_p > 1)
                throw new ArgumentOutOfRangeException("Top_p must be between 0 and 1", "top_p");

            if(n <= 0)
                throw new ArgumentOutOfRangeException("The number of answers must be positive", "n");

            if(stop != null && stop.Count > 4)
                throw new ArgumentOutOfRangeException("\r\nThe maximum number of stop sequences is 4", "stop");

            if (maxTokens <= 0)
                throw new ArgumentOutOfRangeException("The maximum number of tokens must be positive", "maxTokens");

            if (presencePenalty < -2 || presencePenalty > 2)
                throw new ArgumentOutOfRangeException("PresencePenalty must be between -2 and 2", "presencePenalty");

            if (frequencyFenalty < -2 || frequencyFenalty > 2)
                throw new ArgumentOutOfRangeException("FrequencyFenalty must be between -2 and 2", "frequencyFenalty");

            if (logitBias != null && logitBias.Select(x => x.Value).Any(x => x > 100 || x < -100))
                throw new ArgumentOutOfRangeException("Associated bias value must be between -100 and 100", "logitBias");

            return result;
        }

        /// <summary>
        /// Validation of the entered apiKey, endPoint and model
        /// </summary>
        /// <param name="chatGptClient">Validating class instance</param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException">Invalid model name entered</exception>
        /// <exception cref="ArgumentException">Invalid API Key or endPoint entered</exception>
        private static async Task ServerVerification(ChatGptClient chatGptClient)
        {
            using(HttpClient httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {chatGptClient.ApiKey}");
                HttpResponseMessage response = await httpClient.GetAsync($"https://api.openai.com/v1/models");
                HttpContent responseContent = response.Content;
                if (response.IsSuccessStatusCode)
                {
                    string? json;
                    using (var reader = new StreamReader(await responseContent.ReadAsStreamAsync()))
                    {
                        json = await reader.ReadToEndAsync();
                    }
                    SimplifiedModelResponse? deserializedResponse = JsonConvert.DeserializeObject<SimplifiedModelResponse>(json);

                    if (!deserializedResponse.Data.Select(x => x.Id).Contains(chatGptClient.Model))
                        throw new ArgumentOutOfRangeException("The model is not supported by the ChatGPT API", "model");
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    throw new ArgumentException("Invalid token entered", "token");
                else
                    throw new ArgumentException("Invalid endPoint", "endPoint");
            }
        }

        #endregion

        #region Message history operations

        /// <summary>
        /// Clear message history(start new conversation)
        /// </summary>
        public void ClearMessages()
        {
            Messages.Clear();
            MessagesInQueue.Clear();

            CanAddSystemMessage = CanAddUserMessage = true;
            CanSendRequest = false;
        }

        /// <summary>
        /// Delete the last message in the dialog, or all messages to select if they exist
        /// </summary>
        /// <exception cref="InvalidOperationException">No messages in class instance</exception>
        public void DeleteLastMessage()
        {
            if(Messages.Count == 0)
                throw new InvalidOperationException("There are messages in the queue");

            if(MessagesInQueue.Count != 0)
            {
                CanAddUserMessage = false;
                CanSendRequest = true;
                MessagesInQueue.Clear();
                return;
            }

            Messages.Remove(Messages.Last());

            if(Messages.Count == 0)
            {
                CanAddUserMessage = true;
                CanAddSystemMessage = true;
            }
            else
            {
                if (Messages.Last().Role == "user")
                {
                    CanAddUserMessage = false;
                    CanSendRequest = true;
                }
                else
                {
                    CanAddUserMessage = true;
                    CanSendRequest = false;
                }

                if (Messages.Last().Role == "system")
                    CanAddSystemMessage = true;
                else
                    CanAddSystemMessage = false;
            }
        }

        /// <summary>
        /// Add system message in the dialog
        /// </summary>
        /// <param name="content">Message</param>
        /// <exception cref="InvalidOperationException">Cannot add system message to dialog</exception>
        public void AddSystemMessage(string content)
        {
            if (!CanAddSystemMessage)
                throw new InvalidOperationException("There are messages in the queue, expect system's");

            Messages.Add(new Message("system", content));
        }

        /// <summary>
        /// Add user message in the dialog
        /// </summary>
        /// <param name="content">Message</param>
        /// <exception cref="InvalidOperationException">Cannot add user message to dialog</exception>
        public void AddUserMessage(string content)
        {
            if (!CanAddUserMessage)
                throw new InvalidOperationException("It's not your turn to post now");

            Messages.Add(new Message("user", content));

            CanAddSystemMessage = false;
            CanAddUserMessage = false;
            CanSendRequest = true;
        }

        /// <summary>
        /// Get a copy of the conversation
        /// </summary>
        /// <returns>List of Messages from dialog</returns>
        public List<Message> GetMessageHistory()
        {
            List<Message> result = new List<Message>();

            foreach (Message message in Messages)
                result.Add(message.Clone() as Message);

            return result;
        }

        #endregion

        #region Sending request to ChatGPT

        /// <summary>
        /// Request to the server to get ChatGPT response options
        /// </summary>
        /// <param name="numOfRequests">Number of ChatGPT answers</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Can't send request at the moment</exception>
        public async Task<List<Message>> Request(int? numOfRequests = null)
        {
            if (!CanSendRequest)
                throw new InvalidOperationException("To send a request, the queue must be the last user's message");

            if (numOfRequests.HasValue)
                this.N = numOfRequests.Value;

            using(HttpClient httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {ApiKey}");

                var jsonResponse = new StringContent(JsonConvert.SerializeObject(this, Formatting.Indented, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                })
                , Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(EndPoint, jsonResponse);

                if (!response.IsSuccessStatusCode)
                {
                    return new List<Message>();
                }

                ResponseData? responseData = await response.Content.ReadFromJsonAsync<ResponseData>();

                List<Choice> choices = responseData?.Choices ?? new List<Choice>();

                if (choices.Count == 0)
                    return new List<Message>();

                MessagesInQueue = choices.Select(x => x.Message).ToList();

                CanAddUserMessage = false;
                CanSendRequest = false;

                List<Message> result = new List<Message>();

                foreach(Message message in MessagesInQueue)
                    result.Add(message.Clone() as Message);

                return result;
            }
        }

        #endregion

        #region Message Selection

        /// <summary>
        /// Select message from List of ChatGPT responses from which you need to choose the appropriate one
        /// </summary>
        /// <param name="index"></param>
        public void MessageSelection(int index)
        {
            Messages.Add(Messages[index]);
            CanAddUserMessage = true;
        }

        #endregion
    }
}
