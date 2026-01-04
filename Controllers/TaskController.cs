using BackEndFolio.API.Hubs;
using BackEndFolio.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Supabase.Postgrest;


[Route("api/[controller]")]
[ApiController]
[Authorize]
public class TaskController : ControllerBase
{
    private readonly Supabase.Client _supabase;
    private readonly IHubContext<AppHub> _hubContext;

    public TaskController(Supabase.Client supabase, IHubContext<AppHub> hubContext)
    {
        _supabase = supabase;
        _hubContext = hubContext;
    }

    // GET: api/tasks?projectId=...
    [HttpGet]
    public async Task<IActionResult> GetTasks([FromQuery] string projectId)
    {
        var response = await _supabase
            .From<TaskItem>()
            .Select("*")
            .Filter("project_id", Constants.Operator.Equals, projectId)
            .Get();

        //var result = response.Models.Select(task => new TaskItemResponse
        //{
        //    Id = task.Id,
        //    ProjectId = task.ProjectId,
        //    ParentTaskId = task.ParentTaskId,
        //    Title = task.Title,
        //    Description = task.Description,
        //    Status = task.Status,
        //    Priority = task.Priority,
        //    AssigneeId = task.AssigneeId,
        //    StartDate = task.StartDate,
        //    DueDate = task.DueDate,
        //    CreatedAt = task.CreatedAt,
        //    Comments = task.Comments,
        //    Attachments = task.Attachments,
        //});

        return Ok(response.Models);
    }



    // POST: api/task
    [HttpPost]
    public async Task<IActionResult> CreateTask([FromBody] CreateTaskRequest request)
    {
        var task = new TaskItem
        {
            ProjectId = request.ProjectId,
            ParentTaskId = request.ParentTaskId,
            Title = request.Title,
            Description = request.Description,
            Status = request.Status,
            Priority = request.Priority,
            AssigneeId = request.AssigneeId,
            StartDate = request.StartDate,
            DueDate = request.DueDate,
            CreatedAt = DateTime.UtcNow,
        };

        var response = await _supabase
            .From<TaskItem>()
            .Insert(task);

        var createdTask = response.Models.FirstOrDefault();

        if (createdTask != null)
        {
            //var responseDto = new TaskItemResponse
            //{
            //    Id = createdTask.Id,
            //    ProjectId = createdTask.ProjectId,
            //    ParentTaskId = createdTask.ParentTaskId,
            //    Title = createdTask.Title,
            //    Description = createdTask.Description,
            //    Status = createdTask.Status,
            //    Priority = createdTask.Priority,
            //    AssigneeId = createdTask.AssigneeId,
            //    StartDate = createdTask.StartDate,
            //    DueDate = createdTask.DueDate,
            //    CreatedAt = createdTask.CreatedAt,
            //    Comments = new List<Comment>(),
            //    Attachments = new List<Attachment>()
            //};

            await _hubContext.Clients
                .Group(createdTask.ProjectId)
                .SendAsync("TaskCreated", createdTask);

            return Ok(createdTask);
        }
        return StatusCode(500, "Failed to create task.");
    }

    // PATCH: api/tasks/{id}
    [HttpPatch("{id}")]
    public async Task<IActionResult> UpdateTask(string id, [FromBody] PatchTaskRequest updates)
    {
        if (updates == null)
            return BadRequest("Patch body is required");

        try
        {
            // 1. Kiểm tra task có tồn tại không
            var exists = await _supabase
                .From<TaskItem>()
                .Select("id, project_id")
                .Where(t => t.Id == id)
                .Get();

            var task = exists.Models.FirstOrDefault();
            if (task == null)
                return NotFound();

            // 2. Bắt đầu query update
            var query = _supabase
                .From<TaskItem>()
                .Where(t => t.Id == id);

            // 3. Chỉ Set những field có trong request
            if (updates.Title != null)
                query = query.Set(t => t.Title, updates.Title);

            if (updates.Status != null)
                query = query.Set(t => t.Status, updates.Status);

            if (updates.Priority != null)
                query = query.Set(t => t.Priority, updates.Priority);

            if (updates.Description != null)
                query = query.Set(t => t.Description, updates.Description);

            if (updates.StartDate != null)
                query = query.Set(t => t.StartDate, updates.StartDate);

            if (updates.DueDate != null)
                query = query.Set(t => t.DueDate, updates.DueDate);

            if (updates.AssigneeId != null)
            {
                query = query.Set(
                    t => t.AssigneeId,
                    string.IsNullOrEmpty(updates.AssigneeId)
                        ? null
                        : updates.AssigneeId
                );
            }

            // 4. Thực thi UPDATE
            await query.Update();

            // 5. SignalR – chỉ gửi dữ liệu cần thiết
            if (!string.IsNullOrEmpty(task.ProjectId))
            {
                await _hubContext.Clients
                    .Group(task.ProjectId)
                    .SendAsync("TaskUpdated", new
                    {
                        id,
                        updates.Title,
                        updates.Status,
                        updates.Priority,
                        updates.Description,
                        updates.StartDate,
                        updates.DueDate,
                        updates.AssigneeId
                    });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }



    // GET: api/tasks/{id}/activities
    [HttpGet("{id}/activities")]
    public async Task<IActionResult> GetActivities(string id)
    {
        var response = await _supabase
            .From<ActivityLog>()
            .Select("*") // Join Profile
            .Where(x => x.TaskId == id)
            .Order("created_at", Constants.Ordering.Descending)
            .Get();

        return Ok(response.Models);
    }


    // DELETE: api/tasks/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTask(string id)
    {
        await _supabase
            .From<TaskItem>()
            .Where(t => t.Id == id)
            .Delete();

        return Ok(new { message = "task deleted successfully" });
    }
}