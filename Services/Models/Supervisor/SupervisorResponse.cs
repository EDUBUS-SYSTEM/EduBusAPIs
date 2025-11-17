using Data.Models.Enums;
using System;

namespace Services.Models.Supervisor
{
    public class SupervisorResponse
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Address { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public Gender Gender { get; set; }
        public Guid? UserPhotoFileId { get; set; }
        public SupervisorStatus Status { get; set; }
        public DateTime? LastActiveDate { get; set; }
        public string? StatusNote { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}

