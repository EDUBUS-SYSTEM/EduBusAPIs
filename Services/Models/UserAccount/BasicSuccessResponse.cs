namespace Services.Models.UserAccount
{
    public class BasicSuccessResponse
    {
        public bool Success { get; set; }
        public object? Data { get; set; }
        public object? Error { get; set; }
    }
}
