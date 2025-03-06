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
                var polygons = await _context.Polygon
                    .Include(p => p.Points)
                    .Where(p => p.CreatedByUserId == userId ||
                        _context.UserRoles
                            .Include(ur => ur.Role) // Include Role navigation property
                            .Any(ur =>
                                ur.UserId == userId &&
                                ur.Role.CreatedByUserId == p.CreatedByUserId))
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
                return StatusCode(500, $"Internal server error: {ex.Message}");
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
                var polygon = await _context.Polygon
                    .Include(p => p.Points)
                    .FirstOrDefaultAsync(p => p.PolygonName == polygonName && p.CreatedByUserId == userId);

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


}