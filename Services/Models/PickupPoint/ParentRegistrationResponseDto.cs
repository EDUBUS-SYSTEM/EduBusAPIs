namespace Services.Models.PickupPoint
{
    public class ParentRegistrationResponseDto
    {
        public Guid RegistrationId { get; set; }
        public string Email { get; set; } = string.Empty;
        public bool EmailExists { get; set; }
        public bool OtpSent { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
