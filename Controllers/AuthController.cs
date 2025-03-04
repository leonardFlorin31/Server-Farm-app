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
            return Ok(new { Message = "Autentificare reușită!", Username = user.Username,
                                                                UserId = user.Id });
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
}