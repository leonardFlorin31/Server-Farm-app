using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Server_Licenta.Controllers
{
    public class AssignRoleRequest
    {
        public Guid CreatedBy { get; set; }
        public string Username { get; set; }
        public string RoleName { get; set; }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class UserRoleController : ControllerBase
    {
        private readonly AppDbContext _context;
        public UserRoleController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("assign")]
        public async Task<IActionResult> Assign([FromBody] AssignRoleRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.RoleName))
                return BadRequest("Username and RoleName are required.");

            // 1) find-or-create the requested role
            var role = await _context.Roles
                .FirstOrDefaultAsync(r => r.RoleName == req.RoleName);
            if (role == null)
            {
                role = new Role
                {
                    Id = Guid.NewGuid(),
                    RoleName = req.RoleName,
                    CreatedByUserId = req.CreatedBy   // no creator tracked
                };
                _context.Roles.Add(role);
                await _context.SaveChangesAsync();
            }

            // 2) load the target user
            var target = await _context.User
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Username == req.Username);
            if (target == null)
                return NotFound($"User '{req.Username}' not found.");

            // 3) assign or overwrite
            var existing = target.UserRoles.FirstOrDefault();
            if (existing == null)
            {
                // no role yet → assign new
                _context.UserRoles.Add(new UserRole
                {
                    UserId = target.Id,
                    RoleId = role.Id
                });
            }
            else
            {
                // overwrite unconditionally
                existing.RoleId = role.Id;
                _context.UserRoles.Update(existing);
            }

            await _context.SaveChangesAsync();
            return Ok(new
            {
                Message = $"Role '{req.RoleName}' assigned to '{req.Username}'."
            });
        }
    }
}
