using System.ComponentModel.DataAnnotations;

namespace MyWebApp.ViewModels
{
    public class InviteOfficerViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = string.Empty;
    }

    public class InviteOfficerResponseViewModel
    {
        public string Email { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool Success { get; set; }
    }
}