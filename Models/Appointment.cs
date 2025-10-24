using System;
using MyWebApp.Models.Common;

namespace MyWebApp.Models
{
    public enum AppointmentStatus
    {
        Scheduled,
        Completed,
        Cancelled,
        Rescheduled
    }

    public class Appointment : AuditableEntity
    {
        public Guid ApplicationId { get; set; }
        public Application? Application { get; set; }
        public DateTime AppointmentDate { get; set; }
        public AppointmentStatus Status { get; set; }
        public string? Comments { get; set; }
        public string ContactPerson { get; set; } = string.Empty;
        public string Place { get; set; } = string.Empty;
        public string RoomNumber { get; set; } = string.Empty;
        public Guid? ScheduledByOfficerId { get; set; }
        public Officer? ScheduledByOfficer { get; set; }
    }
}
