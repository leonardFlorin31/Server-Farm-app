using Server_Licenta.Controllers;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

public class GrainParcelData
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid PolygonId { get; set; }

    [Required]
    public string CropType { get; set; } = string.Empty;

    public decimal? ParcelArea { get; set; } // Opțional
    public string? IrrigationType { get; set; } // Opțional
    public decimal? FertilizerUsed { get; set; } // Opțional
    public decimal? PesticideUsed { get; set; } // Opțional
    public decimal? Yield { get; set; } // Opțional
    public string? SoilType { get; set; } // Opțional
    public string? Season { get; set; } // Opțional
    public decimal? WaterUsage { get; set; } // Opțional

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;


    [JsonIgnore]
    [ForeignKey("PolygonId")]
    public Polygon Polygon { get; set; } = null!;
}
