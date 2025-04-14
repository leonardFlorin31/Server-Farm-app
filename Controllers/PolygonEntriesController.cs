using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Server_Licenta.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PolygonEntriesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PolygonEntriesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/PolygonEntries?polygonId=<guid>
        // Dacă parametrul "polygonId" este specificat, se vor returna intrările care au acel PolygonID.
        // Dacă se dorește obținerea intrărilor unde PolygonID este NULL, se poate trimite polygonId=null.
        [HttpGet]
        public async Task<IActionResult> GetEntries([FromQuery] Guid? polygonId, [FromQuery] Guid userId)
        {
            //var query = _context.PolygonEntries.AsQueryable();

            //if (polygonId.HasValue)
            //{
            //    query = query.Where(pe => pe.PolygonID == polygonId);
            //}

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

            var entries = await _context.PolygonEntries
                    .Where(p =>
                        // Own polygons
                        p.CreatedByUserID == userId ||
                        // Polygons from users with roles created by the same creator
                        relatedUsers.Contains(p.CreatedByUserID))
                    .Select(p => new PolygonEntryDTO
                    {
                        Id = p.PolygonEntryID,
                        PolygonId = p.PolygonID,
                        CreatedByUserId = p.CreatedByUserID,
                        Categorie = p.Categorie,
                        Valoare = p.Valoare,
                        DataCreare = p.DataCreare,
                    })
                    .ToListAsync();

            return Ok(entries);
        }

        // GET: api/PolygonEntries/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetEntry(Guid id)
        {
            var entry = await _context.PolygonEntries.FindAsync(id);
            if (entry == null)
            {
                return NotFound();
            }

            return Ok(entry);
        }

        // POST: api/PolygonEntries
        // La POST, câmpul PolygonID poate fi null.
        // Se adaugă și câmpul CreatedByUserID, care reprezintă ID-ul creatorului.
        [HttpPost]
        public async Task<IActionResult> CreateEntry([FromBody] CreatePolygonEntryRequest request)
        {
            if (request == null)
            {
                return BadRequest("Datele introduse sunt invalide.");
            }

            var entry = new PolygonEntry
            {
                PolygonEntryID = request.PolygonEntryID,
                // Permitem ca PolygonID să fie null, conform cerinței:
                PolygonID = request.PolygonID,
                CreatedByUserID = request.CreatedByUserID,
                Categorie = request.Categorie,
                Valoare = request.Valoare,
                DataCreare = DateTime.UtcNow
            };

            _context.PolygonEntries.Add(entry);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetEntry), new { id = entry.PolygonEntryID }, entry);
        }

        // DELETE: api/PolygonEntries/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEntry(Guid id)
        {
            var entry = await _context.PolygonEntries.FindAsync(id);
            if (entry == null)
            {
                return NotFound();
            }

            _context.PolygonEntries.Remove(entry);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }

    public class PolygonEntryDTO
    {
        public Guid Id { get; set; }
        public Guid? PolygonId { get; set; }
        public Guid CreatedByUserId { get; set; }
        public string Categorie { get; set; }
        public decimal Valoare { get; set; }
        public DateTime DataCreare { get; set; }
    }

    // DTO pentru cererea POST
    public class CreatePolygonEntryRequest
    {
        // Permite null: acesta este identificatorul poligonului asociat (opțional)
        public Guid PolygonEntryID { get; set; }

        public Guid? PolygonID { get; set; }
        
        // ID-ul utilizatorului care a creat intrarea (nu este opțional)
        public Guid CreatedByUserID { get; set; }
        
        public string Categorie { get; set; }
        
        public decimal Valoare { get; set; }
    }

    // Entitatea EF pentru tabelul PolygonEntries
    public class PolygonEntry
    {
        [Key]
        public Guid PolygonEntryID { get; set; }
        
        // Poate fi null dacă nu este asociat niciun poligon
        public Guid? PolygonID { get; set; }
        
        // ID-ul utilizatorului care a creat intrarea
        public Guid CreatedByUserID { get; set; }
        
        public string Categorie { get; set; }
        
        public decimal Valoare { get; set; }
        
        public DateTime DataCreare { get; set; }
    }
}
