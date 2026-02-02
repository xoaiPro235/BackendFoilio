using BackEndFolio.API.Hubs;
using BackEndFolio.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Supabase.Postgrest;
using BackEndFolio.Services;


[Route("api/[controller]")]
[ApiController]
[Authorize]
public class TaskController : ControllerBase
{
    private readonly Supabase.Client _supabase;
    private readonly IHubContext<AppHub> _hubContext;
    private readonly INotificationService _notificationService;
    private readonly IEmailService _emailService;
    private readonly IActivityLogService _activityLogService;

    public TaskController(Supabase.Client supabase, IHubContext<AppHub> hubContext, INotificationService notificationService, IEmailService emailService, IActivityLogService activityLogService)
    {
        _supabase = supabase;
        _hubContext = hubContext;
        _notificationService = notificationService;
        _emailService = emailService;
        _activityLogService = activityLogService;
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
                .SendAsync("TaskCreated", new {
                    id = createdTask.Id,
                    projectId = createdTask.ProjectId,
                    parentTaskId = createdTask.ParentTaskId,
                    title = createdTask.Title,
                    description = createdTask.Description,
                    status = createdTask.Status,
                    priority = createdTask.Priority,
                    assigneeId = createdTask.AssigneeId,
                    startDate = createdTask.StartDate,
                    dueDate = createdTask.DueDate,
                    createdAt = createdTask.CreatedAt
                });

            // Log activity
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            await _activityLogService.RecordActivityAsync(
                createdTask.ProjectId,
                createdTask.Id,
                currentUserId,
                "created task",
                createdTask.Title
            );

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
        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        try
        {
            // 1. Kiểm tra task có tồn tại không
            var exists = await _supabase
                .From<TaskItem>()
                .Select("id, project_id, title")
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

            string assigneeLogName = "";
            if (updates.AssigneeId != null)
            {
                var newAssigneeId = string.IsNullOrEmpty(updates.AssigneeId) ? null : updates.AssigneeId;
                query = query.Set(t => t.AssigneeId, newAssigneeId);

                if (!string.IsNullOrEmpty(newAssigneeId))
                {
                    var profile = await _supabase.From<Profile>().Where(p => p.Id == newAssigneeId).Single();
                    if (profile != null)
                    {
                        assigneeLogName = profile.Name;
                        string taskLink = $"/project/{task.ProjectId}/board?selectedIssue={id}";

                        await _notificationService.SendDirectNotification(
                            newAssigneeId,
                            "New Task Assigned",
                            $"You have been assigned a new task: {task.Title}",
                            "SUCCESS",
                            taskLink
                        );

                        if (profile.Email != null)
                            await _emailService.SendTaskNotificationEmailAsync(profile.Email, "New Task Assigned", task.Title, task.ProjectId, id);
                    }
                }
                else
                {
                    assigneeLogName = "Unassigned";
                }
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
                        id = id,
                        projectId = task.ProjectId,
                        title = updates.Title,
                        status = updates.Status,
                        priority = updates.Priority,
                        description = updates.Description,
                        startDate = updates.StartDate,
                        dueDate = updates.DueDate,
                        assigneeId = updates.AssigneeId
                    });
            }

            // Log activity
            List<string> changes = new List<string>();
            if (updates.Title != null) changes.Add("title");
            if (updates.Status != null) changes.Add($"status to {updates.Status}");
            if (updates.Priority != null) changes.Add($"priority to {updates.Priority}");
            if (updates.AssigneeId != null) changes.Add($"assignee to {assigneeLogName}");
            if (updates.Description != null) changes.Add("description");
            if (updates.StartDate != null || updates.DueDate != null) changes.Add("dates");

            string action = changes.Count > 0 ? "updated " + string.Join(", ", changes) : "updated task";

            await _activityLogService.RecordActivityAsync(
                task.ProjectId,
                id,
                currentUserId,
                action,
                task.Title
            );

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


    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTask(string id)
    {
        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        
        // Get details before delete for logging
        var taskResponse = await _supabase.From<TaskItem>().Where(t => t.Id == id).Get();
        var task = taskResponse.Models.FirstOrDefault();

        await _supabase
            .From<TaskItem>()
            .Where(t => t.Id == id)
            .Delete();

            // 5. Log & Broadcast
            if (task != null)
            {
                await _activityLogService.RecordActivityAsync(
                    task.ProjectId,
                    id,
                    currentUserId,
                    "deleted task",
                    task.Title
                );
            }
            return NoContent();
    }
}