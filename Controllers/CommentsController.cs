using BackEndFolio.API.Hubs;
using BackEndFolio.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Supabase.Postgrest;
using System.Security.Claims;

[Route("api/tasks/{taskId}/comments")]
[ApiController]
[Authorize]
public class CommentsController : ControllerBase
{
    private readonly Supabase.Client _supabase;
    private readonly IHubContext<AppHub> _hubContext;

    public CommentsController(Supabase.Client supabase, IHubContext<AppHub> hubContext)
    {
        _supabase = supabase;
        _hubContext = hubContext;
    }

    // POST: api/tasks/{taskId}/comments
    [HttpPost]
    public async Task<IActionResult> AddComment(string taskId, [FromBody] Comment comment)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        comment.TaskId = taskId;
        comment.UserId = userId;

        // Insert
        var response = await _supabase
            .From<Comment>()
            .Insert(comment);
        var newComment = response.Models.FirstOrDefault();

        if (newComment != null)
        {
            // Lấy ProjectId của Task để bắn thông báo cho đúng nhóm
            var taskRes = await _supabase.From<TaskItem>().Select("project_id").Where(x => x.Id == taskId).Single();
            var projectId = taskRes?.ProjectId;

            if (projectId != null)
            {
                await _hubContext.Clients.Group(projectId).SendAsync("CommentAdded", taskId, newComment);
            }
        }

        return Ok(newComment);
    }
}