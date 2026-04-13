namespace Core.DTO.Inventory
{
    public class InvoiceScanResult
    {
        public bool Success { get; set; }

        /// <summary>
        /// Raw JSON string returned by the LLM vision model.
        /// FE parses and processes this client-side.
        /// </summary>
        public string? RawJson { get; set; }

        public string? ErrorMessage { get; set; }

        public int TokensUsed { get; set; }
    }
}
