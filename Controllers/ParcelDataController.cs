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
    public class ParcelDataController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ParcelDataController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/ParcelData
        [HttpGet]
        public async Task<ActionResult<IEnumerable<GrainParcelData>>> GetAllParcelData()
        {
            var parcels = await _context.GrainParcelData.ToListAsync();
            return Ok(parcels);
        }

        // GET: api/ParcelData/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<GrainParcelData>> GetParcelDataById(Guid id)
        {
            var parcel = await _context.GrainParcelData.FindAsync(id);

            if (parcel == null)
            {
                return NotFound(new { message = "Parcel data not found" });
            }

            return Ok(parcel);
        }

        [HttpGet("polygon/{polygonId}")]
        public async Task<ActionResult<IEnumerable<GrainParcelData>>> GetParcelsDataByPolygonId(Guid polygonId)
        {
            var parcels = await _context.GrainParcelData
                .Where(p => p.PolygonId == polygonId)
                .ToListAsync();

            if (parcels == null || parcels.Count == 0)
            {
                return NotFound(new { message = "No parcels found for the given polygonId" });
            }

            return Ok(parcels);
        }

        [HttpDelete("polygon/{polygonId}")]
        public async Task<IActionResult> DeleteParcelsByPolygonId(Guid polygonId)
        {
            var parcels = await _context.GrainParcelData
                .Where(p => p.PolygonId == polygonId)
                .ToListAsync();

            if (parcels == null || parcels.Count == 0)
            {
                return NotFound(new { message = "No parcels found for the given polygonId" });
            }

            _context.GrainParcelData.RemoveRange(parcels);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Parcels successfully deleted" });
        }

        // POST: api/ParcelData
        [HttpPost]
        public async Task<ActionResult<GrainParcelData>> CreateParcelData([FromBody] GrainParcelData parcelData)
        {
            if (parcelData == null)
            {
                return BadRequest(new { message = "Invalid data" });
            }

            parcelData.Id = Guid.NewGuid();
            parcelData.CreatedDate = DateTime.UtcNow;

            _context.GrainParcelData.Add(parcelData);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetParcelDataById), new { id = parcelData.Id }, parcelData);
        }

        // DELETE: api/ParcelData/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteParcelData(Guid id)
        {
            var parcel = await _context.GrainParcelData.FindAsync(id);
            if (parcel == null)
            {
                return NotFound(new { message = "Parcel data not found" });
            }

            _context.GrainParcelData.Remove(parcel);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}

public class CreateGrainParcelDataRequest
{
    public Guid PolygonId { get; set; }
    public string? CropType { get; set; }
    public decimal? ParcelArea { get; set; }
    public string? IrrigationType { get; set; }
    public decimal? FertilizerUsed { get; set; }
    public decimal? PesticideUsed { get; set; }
    public decimal? Yield { get; set; }
    public string? SoilType { get; set; }
    public string? Season { get; set; }
    public decimal? WaterUsage { get; set; }
}
