using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server_Licenta;
using Server_Licenta.Controllers;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class TaskController : ControllerBase
{
    private readonly AppDbContext _context;

    public TaskController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateTask([FromBody] TaskCreateRequest request)
    {
        // Validare de bază
        if (request == null || string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.AssignedToUsername))
        {
            return BadRequest(new { Message = "Titlul și username-ul persoanei asignate sunt obligatorii." });
        }

        // 1. Găsește utilizatorul care creează task-ul
        var createdByUser = await _context.User.FirstOrDefaultAsync(u => u.Username == request.CreatedByUsername);
        if (createdByUser == null)
        {
            return NotFound(new { Message = $"Utilizatorul creator '{request.CreatedByUsername}' nu a fost găsit." });
        }

        // 2. Găsește utilizatorul căruia i se asignează task-ul după USERNAME
        var assignedToUser = await _context.User.FirstOrDefaultAsync(u => u.Username == request.AssignedToUsername);
        if (assignedToUser == null)
        {
            return NotFound(new { Message = $"Utilizatorul asignat cu username-ul '{request.AssignedToUsername}' nu a fost găsit." });
        }

        // 3. Creează entitatea Task
        var newTask = new Task
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            Status = request.Status ?? "Asignat", // Valoare default
            CreatedByUserId = createdByUser.Id,
            AssignedToUserId = assignedToUser.Id,
            CreatedAt = DateTime.UtcNow
        };

        // 4. Salvează în baza de date
        _context.Tasks.Add(newTask);
        await _context.SaveChangesAsync();

        // 5. Returnează un răspuns de succes
        return Ok(new
        {
            Message = "Task creat cu succes!",
            TaskId = newTask.Id,
            Title = newTask.Title,
            AssignedTo = assignedToUser.Username
        });
    }
}


public class Task
{
    [Key] // Marchează Id ca cheie primară
    public Guid Id { get; set; }

    [Required] // Titlul este obligatoriu
    [MaxLength(200)]
    public string Title { get; set; }

    public string Description { get; set; }

    [Required]
    public string Status { get; set; }

    // Relația cu utilizatorul care a creat task-ul
    public Guid CreatedByUserId { get; set; }
    [ForeignKey("CreatedByUserId")]
    public User CreatedByUser { get; set; }

    // Relația cu utilizatorul căruia i-a fost asignat task-ul
    public Guid AssignedToUserId { get; set; }
    [ForeignKey("AssignedToUserId")]
    public User AssignedToUser { get; set; }

    public DateTime CreatedAt { get; set; }
}

public class TaskCreateRequest
{
    public string Title { get; set; }
    public string Description { get; set; }
    public string Status { get; set; }
    public string CreatedByUsername { get; set; }
    public string AssignedToUsername { get; set; } // MODIFICAT
}