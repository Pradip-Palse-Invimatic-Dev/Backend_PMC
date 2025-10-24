using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyWebApp.Models;
using MyWebApp.ViewModels;

namespace MyWebApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SeedController : ControllerBase
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<SeedController> _logger;

        public SeedController(
            RoleManager<IdentityRole> roleManager,
            UserManager<ApplicationUser> userManager,
            ILogger<SeedController> logger)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _logger = logger;
        }

        [HttpPost("add-role")]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddRole([FromBody] AddRoleViewModel model)
        {
            try
            {
                // Check if role already exists
                if (await _roleManager.RoleExistsAsync(model.RoleName))
                {
                    return BadRequest(new RoleResponseViewModel
                    {
                        Success = false,
                        Message = $"Role '{model.RoleName}' already exists",
                        RoleName = model.RoleName
                    });
                }

                // Create the new role
                var role = new IdentityRole(model.RoleName);
                var result = await _roleManager.CreateAsync(role);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Created new role {RoleName} successfully", model.RoleName);
                    return Ok(new RoleResponseViewModel
                    {
                        Success = true,
                        Message = $"Role '{model.RoleName}' created successfully",
                        RoleName = model.RoleName
                    });
                }

                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError("Failed to create role {RoleName}. Errors: {Errors}", model.RoleName, errors);
                return BadRequest(new RoleResponseViewModel
                {
                    Success = false,
                    Message = $"Failed to create role: {errors}",
                    RoleName = model.RoleName
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating role {RoleName}", model.RoleName);
                return StatusCode(500, new RoleResponseViewModel
                {
                    Success = false,
                    Message = "An error occurred while creating the role",
                    RoleName = model.RoleName
                });
            }
        }

        [HttpPost("roles")]
        public async Task<IActionResult> SeedRoles()
        {
            try
            {
                string[] roles = new[]
                {
                    "Admin",
                    "User",
                    "JuniorArchitect",
                    "AssistantArchitect",
                    "JuniorLicenceEngineer",
                    "AssistantLicenceEngineer",
                    "JuniorStructuralEngineer",
                    "AssistantStructuralEngineer",
                    "JuniorSupervisor1",
                    "AssistantSupervisor1",
                    "JuniorSupervisor2",
                    "AssistantSupervisor2",
                    "ExecutiveEngineer",
                    "CityEngineer",
                    "Clerk"
                };

                foreach (var roleName in roles)
                {
                    // Check if role already exists
                    if (!await _roleManager.RoleExistsAsync(roleName))
                    {
                        // Create role if it doesn't exist
                        var role = new IdentityRole(roleName);
                        var result = await _roleManager.CreateAsync(role);

                        if (result.Succeeded)
                        {
                            _logger.LogInformation("Created role {RoleName} successfully", roleName);
                        }
                        else
                        {
                            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                            _logger.LogError("Failed to create role {RoleName}. Errors: {Errors}", roleName, errors);
                            return BadRequest($"Failed to create role {roleName}: {errors}");
                        }
                    }
                    else
                    {
                        _logger.LogInformation("Role {RoleName} already exists", roleName);
                    }
                }

                return Ok("Roles seeded successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while seeding roles");
                return StatusCode(500, "An error occurred while seeding roles");
            }
        }


        //get roles endpoint allowed to admin only
        [HttpGet("roles")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetRoles()
        {
            var roles = await _roleManager.Roles.ToListAsync();
            return Ok(roles);
        }

        [HttpPost("admin")]
        public async Task<IActionResult> SeedAdmin([FromBody] CreateAdminViewModel model)
        {
            try
            {
                // Check if admin role exists
                if (!await _roleManager.RoleExistsAsync("Admin"))
                {
                    return BadRequest("Admin role does not exist. Please seed roles first.");
                }

                // Check if admin already exists
                var existingAdmin = await _userManager.GetUsersInRoleAsync("Admin");
                if (existingAdmin.Any())
                {
                    return BadRequest("An admin user already exists.");
                }

                // Create admin user
                var admin = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    EmailAddress = model.Email,
                    Role = "Admin",
                    PhoneNumber = model.PhoneNumber,
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(admin, model.Password);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogError("Failed to create admin user. Errors: {Errors}", errors);
                    return BadRequest($"Failed to create admin user: {errors}");
                }

                // Assign admin role
                result = await _userManager.AddToRoleAsync(admin, "Admin");
                if (!result.Succeeded)
                {
                    await _userManager.DeleteAsync(admin); // Rollback user creation
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogError("Failed to assign admin role. Errors: {Errors}", errors);
                    return BadRequest($"Failed to assign admin role: {errors}");
                }

                _logger.LogInformation("Admin user created successfully");
                return Ok("Admin user created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while seeding admin user");
                return StatusCode(500, "An error occurred while seeding admin user");
            }
        }

        // [HttpDelete("cleanup")]
        // [Authorize(Roles = "Admin")]
        // public async Task<IActionResult> CleanupSeededData()
        // {
        //     try
        //     {
        //         var deletionLog = new List<string>();

        //         // Delete all users except the current admin who is making the request
        //         var currentUserId = _userManager.GetUserId(User);
        //         var allUsers = await _userManager.Users.ToListAsync();

        //         foreach (var user in allUsers)
        //         {
        //             if (user.Id != currentUserId) // Don't delete the current admin
        //             {
        //                 var deleteResult = await _userManager.DeleteAsync(user);
        //                 if (deleteResult.Succeeded)
        //                 {
        //                     deletionLog.Add($"Deleted user: {user.Email}");
        //                     _logger.LogInformation("Deleted user {Email}", user.Email);
        //                 }
        //                 else
        //                 {
        //                     var errors = string.Join(", ", deleteResult.Errors.Select(e => e.Description));
        //                     deletionLog.Add($"Failed to delete user {user.Email}: {errors}");
        //                     _logger.LogError("Failed to delete user {Email}. Errors: {Errors}", user.Email, errors);
        //                 }
        //             }
        //         }

        //         // Delete all roles except Admin (to keep the current admin functional)
        //         var allRoles = await _roleManager.Roles.ToListAsync();
        //         foreach (var role in allRoles)
        //         {
        //             if (role.Name != "Admin")
        //             {
        //                 var deleteResult = await _roleManager.DeleteAsync(role);
        //                 if (deleteResult.Succeeded)
        //                 {
        //                     deletionLog.Add($"Deleted role: {role.Name}");
        //                     _logger.LogInformation("Deleted role {RoleName}", role.Name);
        //                 }
        //                 else
        //                 {
        //                     var errors = string.Join(", ", deleteResult.Errors.Select(e => e.Description));
        //                     deletionLog.Add($"Failed to delete role {role.Name}: {errors}");
        //                     _logger.LogError("Failed to delete role {RoleName}. Errors: {Errors}", role.Name, errors);
        //                 }
        //             }
        //         }

        //         return Ok(new
        //         {
        //             Message = "Cleanup completed",
        //             Details = deletionLog,
        //             UsersDeleted = deletionLog.Count(log => log.StartsWith("Deleted user:")),
        //             RolesDeleted = deletionLog.Count(log => log.StartsWith("Deleted role:")),
        //             Failures = deletionLog.Count(log => log.StartsWith("Failed"))
        //         });
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger.LogError(ex, "Error occurred while cleaning up seeded data");
        //         return StatusCode(500, "An error occurred while cleaning up seeded data");
        //     }
        // }

        // [HttpDelete("cleanup-all")]
        // [Authorize(Roles = "Admin")]
        // public async Task<IActionResult> CleanupAllSeededData([FromQuery] bool confirmDeletion = false)
        // {
        //     try
        //     {
        //         if (!confirmDeletion)
        //         {
        //             return BadRequest("This action will delete ALL users and roles including admins. Add '?confirmDeletion=true' to proceed.");
        //         }

        //         var deletionLog = new List<string>();

        //         // Delete ALL users (including admins)
        //         var allUsers = await _userManager.Users.ToListAsync();
        //         foreach (var user in allUsers)
        //         {
        //             var deleteResult = await _userManager.DeleteAsync(user);
        //             if (deleteResult.Succeeded)
        //             {
        //                 deletionLog.Add($"Deleted user: {user.Email}");
        //                 _logger.LogInformation("Deleted user {Email}", user.Email);
        //             }
        //             else
        //             {
        //                 var errors = string.Join(", ", deleteResult.Errors.Select(e => e.Description));
        //                 deletionLog.Add($"Failed to delete user {user.Email}: {errors}");
        //                 _logger.LogError("Failed to delete user {Email}. Errors: {Errors}", user.Email, errors);
        //             }
        //         }

        //         // Delete ALL roles
        //         var allRoles = await _roleManager.Roles.ToListAsync();
        //         foreach (var role in allRoles)
        //         {
        //             var deleteResult = await _roleManager.DeleteAsync(role);
        //             if (deleteResult.Succeeded)
        //             {
        //                 deletionLog.Add($"Deleted role: {role.Name}");
        //                 _logger.LogInformation("Deleted role {RoleName}", role.Name);
        //             }
        //             else
        //             {
        //                 var errors = string.Join(", ", deleteResult.Errors.Select(e => e.Description));
        //                 deletionLog.Add($"Failed to delete role {role.Name}: {errors}");
        //                 _logger.LogError("Failed to delete role {RoleName}. Errors: {Errors}", role.Name, errors);
        //             }
        //         }

        //         return Ok(new
        //         {
        //             Message = "Complete cleanup finished - ALL users and roles deleted",
        //             Details = deletionLog,
        //             UsersDeleted = deletionLog.Count(log => log.StartsWith("Deleted user:")),
        //             RolesDeleted = deletionLog.Count(log => log.StartsWith("Deleted role:")),
        //             Failures = deletionLog.Count(log => log.StartsWith("Failed")),
        //             Warning = "You will need to re-seed roles and admin to continue using the system"
        //         });
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger.LogError(ex, "Error occurred while performing complete cleanup");
        //         return StatusCode(500, "An error occurred while performing complete cleanup");
        //     }
        // }
    }
}
