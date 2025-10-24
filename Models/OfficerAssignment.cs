using System;
using MyWebApp.Models.Common;
using MyWebApp.Models.Enums;

namespace MyWebApp.Models
{
    public class OfficerAssignment : AuditableEntity
    {
        public Guid ApplicationId { get; set; }
        public Application? Application { get; set; }
        public Guid? OfficerId { get; set; }
        public Officer? Officer { get; set; }
        public ApplicationStage Stage { get; set; }
        public DateTime AssignedDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public string? Comments { get; set; }
        public bool IsDigitallySigned { get; set; }
    }
}
