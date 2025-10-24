using System.ComponentModel.DataAnnotations;

namespace MyWebApp.ViewModels
{
    public class AddRoleViewModel
    {
        [Required]
        [StringLength(50, MinimumLength = 2)]
        [RegularExpression(@"^[a-zA-Z][a-zA-Z0-9]*$", ErrorMessage = "Role name must start with a letter and contain only letters and numbers")]
        public string RoleName { get; set; } = string.Empty;

        public string? Description { get; set; }
    }

    public class RoleResponseViewModel
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? RoleName { get; set; }
    }
}