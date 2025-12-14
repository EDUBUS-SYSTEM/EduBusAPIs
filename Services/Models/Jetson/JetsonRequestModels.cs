using System;
using System.ComponentModel.DataAnnotations;

namespace Services.Models.Jetson
{
    public class SubmitAttendanceRequest
    {
        [Required]
        public Guid TripId { get; set; }

        [Required]
        public Guid PickupPointId { get; set; }

        [Required]
        public Guid StudentId { get; set; }

        public float Similarity { get; set; }

        public float LivenessScore { get; set; }

        public int FramesConfirmed { get; set; }

        [Required]
        public string DeviceId { get; set; }

        public DateTime RecognizedAt { get; set; }
    }
}
