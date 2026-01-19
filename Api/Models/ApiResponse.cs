namespace API.Models
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public int Code { get; set; }
        public int SubCode { get; set; }

        public string? UserMessage { get; set; }
        public string? SystemMessage { get; set; }

        public IReadOnlyList<string> ValidateInfo { get; set; } = [];
        public T Data { get; set; } = default!;

        public bool GetLastData { get; set; }
        public DateTimeOffset ServerTime { get; set; }
    }
}
