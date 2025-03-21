using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Server_Licenta.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AnimalParcelDataController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AnimalParcelDataController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<AnimalParcelData>>> GetAllAnimalParcels()
        {
            var parcels = await _context.AnimalParcelData.ToListAsync();
            return Ok(parcels);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<AnimalParcelData>> GetAnimalParcelById(Guid id)
        {
            var parcel = await _context.AnimalParcelData.FindAsync(id);

            if (parcel == null)
            {
                return NotFound(new { message = "Animal parcel data not found" });
            }

            return Ok(parcel);
        }

        [HttpGet("polygon/{polygonId}")]
        public async Task<ActionResult<IEnumerable<AnimalParcelData>>> GetAnimalParcelsByPolygonId(Guid polygonId)
        {
            var parcels = await _context.AnimalParcelData
                .Where(p => p.PolygonId == polygonId)
                .ToListAsync();

            if (parcels == null || parcels.Count == 0)
            {
                return NotFound(new { message = "No animal parcels found for the given polygonId" });
            }

            return Ok(parcels);
        }

        [HttpDelete("polygon/{polygonId}")]
        public async Task<IActionResult> DeleteAnimalParcelsByPolygonId(Guid polygonId)
        {
            var parcels = await _context.AnimalParcelData
                .Where(p => p.PolygonId == polygonId)
                .ToListAsync();

            if (parcels == null || parcels.Count == 0)
            {
                return NotFound(new { message = "No animal parcels found for the given polygonId" });
            }

            _context.AnimalParcelData.RemoveRange(parcels);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Animal parcels successfully deleted" });
        }

        [HttpPost]
        public async Task<ActionResult<AnimalParcelData>> CreateOrUpdateAnimalParcel([FromBody] CreateAnimalParcelDataRequest request)
        {
            if (request == null)
            {
                return BadRequest(new { message = "Invalid data" });
            }

            var existingParcels = await _context.AnimalParcelData
                .Where(p => p.PolygonId == request.PolygonId)
                .ToListAsync();

            if (existingParcels.Any())
            {
                _context.AnimalParcelData.RemoveRange(existingParcels);
                await _context.SaveChangesAsync();
            }

            var parcelData = new AnimalParcelData
            {
                Id = Guid.NewGuid(),
                PolygonId = request.PolygonId,
                AnimalType = request.AnimalType,
                NumberOfAnimals = request.NumberOfAnimals,
                FeedType = request.FeedType,
                WaterConsumption = request.WaterConsumption,
                VeterinaryVisits = request.VeterinaryVisits,
                WasteManagement = request.WasteManagement,
                CreatedDate = DateTime.UtcNow
            };

            _context.AnimalParcelData.Add(parcelData);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetAnimalParcelById), new { id = parcelData.Id }, parcelData);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAnimalParcel(Guid id)
        {
            var parcel = await _context.AnimalParcelData.FindAsync(id);
            if (parcel == null)
            {
                return NotFound(new { message = "Animal parcel data not found" });
            }

            _context.AnimalParcelData.Remove(parcel);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }

    public class CreateAnimalParcelDataRequest
    {
        public Guid PolygonId { get; set; }
        public string AnimalType { get; set; } = string.Empty;
        public int NumberOfAnimals { get; set; }
        public string FeedType { get; set; } = string.Empty;
        public decimal WaterConsumption { get; set; }
        public int VeterinaryVisits { get; set; }
        public string WasteManagement { get; set; } = string.Empty;
    }
}