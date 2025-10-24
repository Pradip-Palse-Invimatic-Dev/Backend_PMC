using System.ComponentModel.DataAnnotations;

namespace MyWebApp.Models
{
    public class SmtpSettings
    {
        [Required]
        public string Server { get; set; } = string.Empty;

        [Required]
        public int Port { get; set; }

        [Required]
        public string User { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        public bool UseSsl { get; set; } = true;

        public bool RequiresAuthentication { get; set; } = true;

        public string PreferredEncoding { get; set; } = "utf-8";

        public string SenderName { get; set; } = string.Empty;

        public string SenderEmail { get; set; } = string.Empty;
    }
}
