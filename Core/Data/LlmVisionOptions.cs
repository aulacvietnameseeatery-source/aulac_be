namespace Core.Data
{
    public class LlmVisionOptions
    {
        /// <summary>
        /// LLM provider identifier: "openai", "gemini", "anthropic"
        /// </summary>
        public string Provider { get; set; } = "openai";

        public string ApiKey { get; set; } = null!;

        /// <summary>
        /// Model name (e.g. "gpt-4o", "gemini-2.0-flash", "claude-sonnet-4-20250514")
        /// </summary>
        public string Model { get; set; } = "gpt-4o";

        /// <summary>
        /// Base endpoint override. If null, uses the provider's default URL.
        /// </summary>
        public string? Endpoint { get; set; }

        public int MaxTokens { get; set; } = 4096;

        /// <summary>
        /// Request timeout in seconds.
        /// </summary>
        public int TimeoutSeconds { get; set; } = 60;
    }
}
