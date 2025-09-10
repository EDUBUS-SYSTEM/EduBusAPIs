namespace Services.Models.PickupPoint
{
    public class VerifyOtpWithStudentsResponseDto
    {
        public bool Verified { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<StudentBriefDto> Students { get; set; } = new();
        public bool EmailExists { get; set; }
    }
}
