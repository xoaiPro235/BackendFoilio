using BackEndFolio.API.Hubs;
using BackEndFolio.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Supabase.Postgrest;
using System.Security.Claims;

[Route("api/task/{taskId}/comments")]
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
    public async Task<IActionResult> AddComment(string taskId, [FromBody] CommentCreateRequest content)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var comment = new Comment
        {
            TaskId = taskId,
            UserId = userId,
            Content = content.Content,
            CreatedAt = DateTime.UtcNow
        };

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

    [HttpDelete("{commentId}")]
    public async Task<IActionResult> DeleteComment(string taskId, string commentId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        try
        {
            // Kiểm tra quyền: Chỉ người tạo comment mới được xóa
            var existingCommentRes = await _supabase
                .From<Comment>()
                .Select("*")
                .Where(c => c.Id == commentId && c.UserId == userId)
                .Single();
            if (existingCommentRes == null)
            {
                return Forbid("You can't do that!");
            }
            // Xóa comment
            await _supabase
                .From<Comment>()
                .Where(c => c.Id == commentId)
                .Delete();
            // Lấy ProjectId của Task để bắn thông báo cho đúng nhóm
            var taskRes = await _supabase.From<TaskItem>().Select("project_id").Where(x => x.Id == taskId).Single();
            var projectId = taskRes?.ProjectId;
            if (projectId != null)
            {
                await _hubContext.Clients.Group(projectId).SendAsync("CommentDeleted", taskId, commentId);
            }
            return Ok(new { message = "Comment deleted successfully." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Error deleting comment: " + ex.Message });
        }
    }
}