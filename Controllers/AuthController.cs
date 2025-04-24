using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Server_Licenta.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;

        // Injectați AppDbContext prin constructor
        public AuthController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            // Verifică dacă utilizatorul există
            var user = _context.User
                .FirstOrDefault(u => u.Username == request.Username && u.Password == request.Password);

            if (user == null)
            {
                return Unauthorized(new { Message = "Nume de utilizator sau parolă incorectă." });
            }

            // Returnează un răspuns de succes
            return Ok(new
            {
                Message = "Autentificare reușită!",
                Username = user.Username,
                UserId = user.Id
            });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            // 1) Validări simple
            if (string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password) ||
                string.IsNullOrWhiteSpace(request.Name) ||
                string.IsNullOrWhiteSpace(request.LastName))
            {
                return BadRequest(new { Message = "Toate câmpurile (Username, Email, Password) sunt obligatorii." });
            }

            // 2) Verifică unicitatea
            if (await _context.User.AnyAsync(u => u.Username == request.Username))
                return Conflict(new { Message = "Nume de utilizator deja folosit." });

            if (await _context.User.AnyAsync(u => u.Email == request.Email))
                return Conflict(new { Message = "Email deja folosit." });

            // 3) Creează entitatea User (poți adăuga aici hashing pentru parolă)
            var user = new User
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                LastName = request.LastName,
                Username = request.Username,
                Email = request.Email,
                Password = request.Password

            };

            // 4) Salvează în baza de date
            _context.User.Add(user);
            await _context.SaveChangesAsync();

            if (request.Role == true)
            {
                Console.WriteLine("User is admin");

                var adminRole = new Role
                {
                    Id = Guid.NewGuid(),
                    RoleName = "admin",
                    CreatedByUserId = user.Id
                };
                _context.Roles.Add(adminRole);

                user.UserRoles.Add(new UserRole
                {
                    UserId = user.Id,
                    RoleId = adminRole.Id
                });

            }
            else
            {
                Console.WriteLine("User is not admin");

            }

            // 5) Salvează în baza de date
            await _context.SaveChangesAsync();

            // 6) Returnează 201 Created cu locația noului resource
            return CreatedAtAction(
                nameof(GetUserByUsername),
                new { username = user.Username },
                new { user.Id, user.Username, user.Email }
            );
        }



        [HttpGet("{username}")]
        public async Task<IActionResult> GetUserByUsername(string username)
        {
            // Folosiți _context pentru a interoga baza de date
            var user = await _context.User
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
            {
                return NotFound();
            }

            // Nu returnam parola utilizatorului
            var userDto = new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Name = user.Name,
                LastName = user.LastName,
                Email = user.Email,

            };

            return Ok(userDto);
        }

    }



    public class UserDto
    {
        public Guid Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public string LastName { get; set; }
    }

    public class User
    {
        public Guid Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Name { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }

        // Navigation property for UserRoles
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }

    public class LoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    // DTO pentru înregistrare
    public class RegisterRequest
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Name { get; set; }
        public string LastName { get; set; }

        public bool Role { get; set; } // true pentru admin, false pentru user

    }
}