using BackEndFolio.API.Hubs;
using BackEndFolio.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Supabase.Postgrest;


[Route("api/[controller]")]
[ApiController]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly Supabase.Client _supabase;
    private readonly IHubContext<AppHub> _hubContext;

    public TasksController(Supabase.Client supabase, IHubContext<AppHub> hubContext)
    {
        _supabase = supabase;
        _hubContext = hubContext;
    }

    // GET: api/tasks?projectId=...
    [HttpGet]
    public async Task<IActionResult> GetTasks([FromQuery] string projectId)
    {
        // Lấy Task + Join Comments + Join Attachments + Join Assignee(Profile)
        var response = await _supabase
            .From<TaskItem>()
            .Select("*, comments(*), attachments(*), profiles(*)")
            .Where(x => x.ProjectId == projectId)
            .Get();

        return Ok(response.Models);
    }

    // POST: api/tasks
    [HttpPost]
    public async Task<IActionResult> CreateTask([FromBody] TaskItem task)
    {
        var response = await _supabase
            .From<TaskItem>()
            .Insert(task);

        var createdTask = response.Models.FirstOrDefault();

        if (createdTask != null)
        {
            // Bắn SignalR
            await _hubContext.Clients.Group(task.ProjectId).SendAsync("TaskCreated", createdTask);
        }

        return Ok(createdTask);
    }

    // PATCH: api/tasks/{id} (Smart Update)
    [HttpPatch("{id}")]
    public async Task<IActionResult> UpdateTask(string id, [FromBody] TaskItem updates)
    {
        // 1. Lấy cũ
        TaskItem existing;
        try
        {
            existing = await _supabase
                .From<TaskItem>()
                .Where(x => x.Id == id)
                .Single();

        } catch (Exception)
        {
            return NotFound();
        }


        // 2. Merge
        if (updates.Title != null) existing.Title = updates.Title;
        if (updates.Status != null) existing.Status = updates.Status;
        if (updates.Priority != null) existing.Priority = updates.Priority;
        if (updates.Description != null) existing.Description = updates.Description;
        if (updates.StartDate != null) existing.StartDate = updates.StartDate;
        if (updates.DueDate != null) existing.DueDate = updates.DueDate;
        if (updates.AssigneeId != null) existing.AssigneeId = updates.AssigneeId == "" ? null : updates.AssigneeId;

        // 3. Update
        await _supabase.From<TaskItem>().Update(existing);

        // 4. SignalR
        await _hubContext.Clients.Group(existing.ProjectId).SendAsync("TaskUpdated", existing);

        return Ok(existing);
    }

    // GET: api/tasks/{id}/activities
    [HttpGet("{id}/activities")]
    public async Task<IActionResult> GetActivities(string id)
    {
        var response = await _supabase
            .From<ActivityLog>()
            .Select("*, profiles(name, avatar_url)") // Join Profile
            .Where(x => x.TaskId == id)
            .Order("created_at", Constants.Ordering.Descending)
            .Get();

        return Ok(response.Models);
    }
}