using Server_Licenta.Controllers;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

public class AnimalParcelData
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid PolygonId { get; set; }

    [Required]
    public string AnimalType { get; set; } = string.Empty;

    public int NumberOfAnimals { get; set; }

    public string FeedType { get; set; } = string.Empty;

    public decimal WaterConsumption { get; set; }

    public int VeterinaryVisits { get; set; }

    public string WasteManagement { get; set; } = string.Empty;

    public DateTime? CreatedDate { get; set; }

    [JsonIgnore]
    [ForeignKey("PolygonId")]
    public Polygon Polygon { get; set; } = null!;
}