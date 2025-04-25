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
        public UserRoleController(AppDbContext ctx) => _context = ctx;

        [HttpPost("assign")]
        public async Task<IActionResult> Assign([FromBody] AssignRoleRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Username) ||
                string.IsNullOrWhiteSpace(req.RoleName))
                return BadRequest("Username + RoleName + CreatedBy are required.");

            var user = await _context.User
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Username == req.Username);

            if (user == null)
                return NotFound($"User '{req.Username}' not found.");

            var hasOtherAdminRole = user.UserRoles
       .Any(ur => ur.Role.CreatedByUserId != req.CreatedBy);

            if (hasOtherAdminRole)
                return BadRequest(
                  "This user already has a role assigned by another admin and cannot be reassigned.");

            // 1) Find-or-create the Role FOR THIS ADMIN
            var role = await _context.Roles
                .FirstOrDefaultAsync(r =>
                   r.RoleName == req.RoleName
                && r.CreatedByUserId == req.CreatedBy);

            if (role == null)
            {
                role = new Role
                {
                    Id = Guid.NewGuid(),
                    RoleName = req.RoleName,
                    CreatedByUserId = req.CreatedBy
                };
                _context.Roles.Add(role);
                await _context.SaveChangesAsync();
            }

            

            // 3) Remove any existing link this admin made
            var oldLinks = user.UserRoles
                .Where(ur => ur.Role.CreatedByUserId == req.CreatedBy)
                .ToList();

            if (oldLinks.Any())
                _context.UserRoles.RemoveRange(oldLinks);

            // 4) Add the new one
            _context.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = role.Id
            });

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = $"[{req.CreatedBy}] '{req.RoleName}' assigned to '{req.Username}'."
            });
        }
    }
}
