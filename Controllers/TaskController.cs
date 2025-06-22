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

    [HttpGet("user/{username}")]
    public async Task<IActionResult> GetTasksForUser(string username)
    {
        var user = await _context.User.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null)
        {
            return NotFound(new { Message = $"Utilizatorul '{username}' nu a fost găsit." });
        }

        var tasks = await _context.Tasks
            .Include(t => t.AssignedToUser)
            .Where(t => t.CreatedByUserId == user.Id || t.AssignedToUserId == user.Id)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new
            {
                t.Id, 
                t.Title,
                t.Description,
                t.Status,
                AssignedToUsername = t.AssignedToUser.Name + " " + t.AssignedToUser.LastName,
                CanChangeStatus = true
            })
            .ToListAsync();

        return Ok(tasks);
    }

    // METODA UPDATE
    [HttpPut("update/{taskId}")]
    public async Task<IActionResult> UpdateTaskStatus(Guid taskId, [FromBody] TaskStatusUpdateRequest request)
    {
        if (request == null || string.IsNullOrEmpty(request.NewStatus))
        {
            return BadRequest(new { Message = "Noul status este obligatoriu." });
        }

        // Caută task-ul în baza de date după ID.
        var taskToUpdate = await _context.Tasks.FindAsync(taskId);

        if (taskToUpdate == null)
        {
            return NotFound(new { Message = $"Task-ul cu ID-ul '{taskId}' nu a fost găsit." });
        }

        // Actualizează proprietatea Status.
        taskToUpdate.Status = request.NewStatus;

        try
        {
            // Salvează modificările în baza de date.
            await _context.SaveChangesAsync();
            return Ok(new { Message = "Statusul a fost actualizat cu succes." });
        }
        catch (DbUpdateConcurrencyException)
        {
            // Gestionează cazul în care task-ul a fost modificat sau șters de altcineva între timp.
            return Conflict(new { Message = "Eroare de concurență. Datele au fost modificate de alt utilizator." });
        }
    }

    [HttpDelete("delete")]
    public async Task<IActionResult> DeleteTask([FromQuery] string title, [FromQuery] string username)
    {
        // Validare de bază pentru parametrii primiți
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(username))
        {
            return BadRequest(new { Message = "Titlul și username-ul angajatului sunt obligatorii pentru ștergere." });
        }

        // Găsește utilizatorul căruia îi este asignat task-ul
        var assignedToUser = await _context.User.FirstOrDefaultAsync(u => u.Username == username);
        if (assignedToUser == null)
        {
            return NotFound(new { Message = $"Utilizatorul asignat '{username}' nu a fost găsit." });
        }

        // Găsește task-ul de șters.
        // ATENȚIE: Această logică va șterge *primul* task care corespunde criteriilor. 
        // Dacă mai multe task-uri au același titlu și sunt asignate aceluiași utilizator, doar unul va fi șters.
        var taskToDelete = await _context.Tasks.FirstOrDefaultAsync(t => t.Title == title && t.AssignedToUserId == assignedToUser.Id);

        if (taskToDelete == null)
        {
            return NotFound(new { Message = $"Nu a fost găsit un task cu titlul '{title}' asignat lui '{username}'." });
        }

        // Șterge task-ul din contextul bazei de date
        _context.Tasks.Remove(taskToDelete);

        // Salvează modificările
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Task șters cu succes." });
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

public class TaskStatusUpdateRequest
{
    public string NewStatus { get; set; }
}