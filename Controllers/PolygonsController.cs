using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Server_Licenta.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PolygonsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PolygonsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Polygons (Get all polygons for current user)
        [HttpGet]
        public async Task<IActionResult> GetPolygons([FromQuery] Guid userId)
        {
            try
            {
                // Find the role creators for the user's roles
                var roleCreators = await _context.UserRoles
                    .Where(ur => ur.UserId == userId)
                    .Select(ur => ur.Role.CreatedByUserId)
                    .Distinct()
                    .ToListAsync();

                // Find all users who have roles created by the same creators
                var relatedUsers = await _context.UserRoles
                    .Where(ur => roleCreators.Contains(ur.Role.CreatedByUserId))
                    .Select(ur => ur.UserId)
                    .Distinct()
                    .ToListAsync();

                var polygons = await _context.Polygon
                    .Include(p => p.Points)
                    .Where(p =>
                        // Own polygons
                        p.CreatedByUserId == userId ||
                        // Polygons from users with roles created by the same creator
                        relatedUsers.Contains(p.CreatedByUserId))
                    .Select(p => new PolygonDto
                    {
                        Id = p.PolygonId,
                        Name = p.PolygonName,
                        Points = p.Points.Select(pt => new PointDto
                        {
                            Latitude = pt.Latitude,
                            Longitude = pt.Longitude,
                            Order = pt.Order
                        }).ToList()
                    })
                    .ToListAsync();

                return Ok(polygons);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }



        // GET: api/Polygons/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPolygon(Guid id, [FromQuery] Guid userId)
        {
            try
            {
                var polygon = await _context.Polygon
                    .Include(p => p.Points)
                    .FirstOrDefaultAsync(p => p.PolygonId == id &&
                        (p.CreatedByUserId == userId ||
                         _context.UserRoles
                             .Include(ur => ur.Role) // Include Role navigation property
                             .Any(ur =>
                                 ur.UserId == userId &&
                                 ur.Role.CreatedByUserId == p.CreatedByUserId)));

                if (polygon == null)
                {
                    return NotFound();
                }

                return Ok(new PolygonDto
                {
                    Id = polygon.PolygonId,
                    Name = polygon.PolygonName,
                    Points = polygon.Points.Select(pt => new PointDto
                    {
                        Latitude = pt.Latitude,
                        Longitude = pt.Longitude,
                        Order = pt.Order
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("names")]
        public async Task<IActionResult> GetPolygonNames([FromQuery] Guid userId)
        {
            try
            {
                // Găsim creatorii de roluri pentru utilizatorul dat
                var roleCreators = await _context.UserRoles
                    .Where(ur => ur.UserId == userId)
                    .Select(ur => ur.Role.CreatedByUserId)
                    .Distinct()
                    .ToListAsync();

                // Găsim toți utilizatorii care au roluri create de aceiași creatori
                var relatedUsers = await _context.UserRoles
                    .Where(ur => roleCreators.Contains(ur.Role.CreatedByUserId))
                    .Select(ur => ur.UserId)
                    .Distinct()
                    .ToListAsync();

                // Selectăm doar Id-ul și Numele poligoanelor pe baza aceleiași logici de acces
                var polygonNames = await _context.Polygon
                    .Where(p =>
                        p.CreatedByUserId == userId ||
                        relatedUsers.Contains(p.CreatedByUserId))
                    .Select(p => new
                    {
                        Id = p.PolygonId,
                        Name = p.PolygonName
                    })
                    .ToListAsync();

                Console.WriteLine($"Poligoane găsite: {polygonNames.Count}");

                return Ok(polygonNames);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }


        // POST: api/Polygons
        [HttpPost]
        public async Task<IActionResult> CreatePolygon([FromBody] CreatePolygonRequest request)
        {
            try
            {
                var polygon = new Polygon
                {
                    PolygonId = Guid.NewGuid(),
                    PolygonName = request.Name,
                    CreatedByUserId = request.UserId,
                    CreatedDate = DateTime.UtcNow,
                    Points = request.Points.Select((p, index) => new PolygonPoint
                    {
                        PointId = Guid.NewGuid(),
                        Latitude = p.Latitude,
                        Longitude = p.Longitude,
                        Order = index
                    }).ToList()
                };

                _context.Polygon.Add(polygon);
                await _context.SaveChangesAsync();

                // Map the entity to the DTO
                var responseDto = new PolygonResponseDto
                {
                    PolygonId = polygon.PolygonId,
                    Name = polygon.PolygonName,
                    CreatedByUserId = polygon.CreatedByUserId,
                    CreatedDate = polygon.CreatedDate,
                    Points = polygon.Points.Select(p => new PolygonPointDto
                    {
                        PointId = p.PointId,
                        Latitude = p.Latitude,
                        Longitude = p.Longitude,
                        Order = p.Order
                    }).ToList()
                };

                return CreatedAtAction(nameof(GetPolygon), new { id = polygon.PolygonId }, responseDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // DELETE: api/Polygons/
        [HttpDelete("{polygonName}")]
        public async Task<IActionResult> DeletePolygon([FromRoute] string polygonName, [FromQuery] Guid userId)
        {
            try
            {
                // Găsim creatorii de roluri pentru utilizatorul dat
                var roleCreators = await _context.UserRoles
                    .Where(ur => ur.UserId == userId)
                    .Select(ur => ur.Role.CreatedByUserId)
                    .Distinct()
                    .ToListAsync();

                // Găsim toți utilizatorii care au roluri create de aceiași creatori
                var relatedUsers = await _context.UserRoles
                    .Where(ur => roleCreators.Contains(ur.Role.CreatedByUserId))
                    .Select(ur => ur.UserId)
                    .Distinct()
                    .ToListAsync();

                // Căutăm poligonul după nume și verificăm dacă CreatedByUserId
                // este fie utilizatorul curent, fie unul din relatedUsers
                var polygon = await _context.Polygon
                    .Include(p => p.Points)
                    .FirstOrDefaultAsync(p => p.PolygonName == polygonName &&
                        (p.CreatedByUserId == userId || relatedUsers.Contains(p.CreatedByUserId)));

                if (polygon == null)
                {
                    return NotFound();
                }

                _context.Polygon.Remove(polygon);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePolygon(Guid id, [FromBody] UpdatePolygonRequest request, [FromQuery] Guid userId)
        {
            try
            {
                // Găsim creatorii de roluri pentru utilizatorul curent
                var roleCreators = await _context.UserRoles
                    .Where(ur => ur.UserId == userId)
                    .Select(ur => ur.Role.CreatedByUserId)
                    .Distinct()
                    .ToListAsync();

                // Găsim toți utilizatorii cu roluri create de aceiași creatori
                var relatedUsers = await _context.UserRoles
                    .Where(ur => roleCreators.Contains(ur.Role.CreatedByUserId))
                    .Select(ur => ur.UserId)
                    .Distinct()
                    .ToListAsync();

                // Căutăm poligonul și verificăm permisiunile
                var polygon = await _context.Polygon
                    .FirstOrDefaultAsync(p => p.PolygonId == id &&
                        (p.CreatedByUserId == userId || relatedUsers.Contains(p.CreatedByUserId)));

                if (polygon == null)
                {
                    return NotFound();
                }

                // Actualizăm numele poligonului
                polygon.PolygonName = request.Name;

                // Ștergem punctele existente
                var pointsToDelete = await _context.PolygonPoints
                    .Where(p => p.PolygonId == id)
                    .ToListAsync();

                _context.PolygonPoints.RemoveRange(pointsToDelete);

                // Adăugăm noile puncte, folosind Order-ul din request
                var newPoints = request.Points
                    .Select(p => new PolygonPoint
                    {
                        PointId = Guid.NewGuid(),
                        PolygonId = id,
                        Latitude = p.Latitude,
                        Longitude = p.Longitude,
                        Order = p.Order // Use the Order from the request
                    })
                    .ToList();

                await _context.PolygonPoints.AddRangeAsync(newPoints);

                // Salvăm modificările în baza de date
                await _context.SaveChangesAsync();

                // Reîncărcăm poligonul actualizat pentru răspuns
                var updatedPolygon = await _context.Polygon
                    .Include(p => p.Points)
                    .FirstOrDefaultAsync(p => p.PolygonId == id);

                return Ok(new PolygonDto
                {
                    Id = updatedPolygon.PolygonId,
                    Name = updatedPolygon.PolygonName,
                    Points = updatedPolygon.Points
                        .OrderBy(pt => pt.Order)
                        .Select(pt => new PointDto
                        {
                            Latitude = pt.Latitude,
                            Longitude = pt.Longitude,
                            Order = pt.Order
                        })
                        .ToList()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("id-by-name")]
        public async Task<IActionResult> GetPolygonIdByName([FromQuery] string polygonName, [FromQuery] Guid userId)
        {
            try
            {
                // Găsim creatorii de roluri pentru utilizatorul curent
                var roleCreators = await _context.UserRoles
                    .Where(ur => ur.UserId == userId)
                    .Select(ur => ur.Role.CreatedByUserId)
                    .Distinct()
                    .ToListAsync();

                // Găsim toți utilizatorii cu roluri create de aceiași creatori
                var relatedUsers = await _context.UserRoles
                    .Where(ur => roleCreators.Contains(ur.Role.CreatedByUserId))
                    .Select(ur => ur.UserId)
                    .Distinct()
                    .ToListAsync();

                // Căutăm poligonul după nume și verificăm permisiunile
                var polygon = await _context.Polygon
                    .FirstOrDefaultAsync(p => p.PolygonName == polygonName &&
                        (p.CreatedByUserId == userId || relatedUsers.Contains(p.CreatedByUserId)));

                if (polygon == null)
                {
                    return NotFound();
                }

                return Ok(new { Id = polygon.PolygonId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }

    // DTO Classes
    public class PolygonDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public List<PointDto> Points { get; set; }
    }

    public class PointDto
    {
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public int Order { get; set; }
    }

    public class CreatePolygonRequest
    {
        public Guid UserId { get; set; }
        public string Name { get; set; }
        public List<PointRequest> Points { get; set; }
    }

    public class PointRequest
    {
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }

        public int Order { get; set; }
    }

    public class PolygonResponseDto
    {
        public Guid PolygonId { get; set; }
        public string Name { get; set; }
        public Guid CreatedByUserId { get; set; }
        public DateTime CreatedDate { get; set; }
        public List<PolygonPointDto> Points { get; set; }
    }

    public class PolygonPointDto
    {
        public Guid PointId { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public int Order { get; set; }
    }

    // Entity Classes 
    public class Polygon
    {
        public Guid PolygonId { get; set; }
        public string PolygonName { get; set; }
        public Guid CreatedByUserId { get; set; }
        public DateTime CreatedDate { get; set; }
        // Navigation property for points
    public List<PolygonPoint> Points { get; set; } = new List<PolygonPoint>();


    }

    public class PolygonPoint
    {
        public Guid PointId { get; set; }
        public Guid PolygonId { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        public decimal Latitude { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        public decimal Longitude { get; set; }

        public int Order { get; set; }

        public Polygon Polygon { get; set; }
    }

    public class Role
    {
        public Guid Id { get; set; }
        public string RoleName { get; set; }
        public Guid CreatedByUserId { get; set; }

        // Navigation property for UserRoles
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }

    public class UserRole
    {
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }

        // Navigation properties
        public User User { get; set; }
        public Role Role { get; set; }
    }

    public class UpdatePolygonRequest
    {
        public string Name { get; set; }
        public List<PointRequest> Points { get; set; }
    }


}