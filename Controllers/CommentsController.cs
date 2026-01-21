using BackEndFolio.API.Hubs;
using BackEndFolio.Models;
using BackEndFolio.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Supabase.Postgrest;
using System.Security.Claims;
using System.Threading.Tasks;

[Route("api/task/{taskId}/comments")]
[ApiController]
[Authorize]
public class CommentsController : ControllerBase
{
    private readonly Supabase.Client _supabase;
    private readonly IHubContext<AppHub> _hubContext;
    private readonly INotificationService _notificationService;
    private readonly IActivityLogService _activityLogService;

    public CommentsController(Supabase.Client supabase, IHubContext<AppHub> hubContext, INotificationService notificationService, IActivityLogService activityLogService)
    {
        _supabase = supabase;
        _hubContext = hubContext;
        _notificationService = notificationService;
        _activityLogService = activityLogService;
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
            var taskRes = await _supabase.From<TaskItem>().Select("project_id, title").Where(x => x.Id == taskId).Single();

            // Gửi thông báo cho tất cả thành viên trong project trừ người vừa comment
            if (taskRes != null)
            {
                // 2. Tạo Link dẫn trực tiếp tới Task ở Frontend
                string taskLink = $"/project/{taskRes.ProjectId}/board?selectedIssue={taskId}";

                // 3. Gửi thông báo cho mọi người trong Project (trừ người comment)
                await _notificationService.SendProjectNotification(
                    taskRes.ProjectId,
                    userId,
                    "New Comment",
                    $"A new comment has been added to task: {taskRes.Title}",
                    "INFO",
                    taskLink
                );

                // 4. Bắn SignalR Real-time để cập nhật UI comment ngay lập tức cho các user khác
                await _hubContext.Clients.Group(taskRes.ProjectId).SendAsync("CommentAdded", new {
                    id = newComment.Id,
                    taskId = newComment.TaskId,
                    userId = newComment.UserId,
                    content = newComment.Content,
                    createdAt = newComment.CreatedAt
                });

                // Log activity
                await _activityLogService.RecordActivityAsync(
                    taskRes.ProjectId,
                    taskId,
                    userId,
                    "added a comment",
                    taskRes.Title
                );
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
            var taskRes = await _supabase.From<TaskItem>().Select("project_id, title").Where(x => x.Id == taskId).Single();
            var projectId = taskRes?.ProjectId;
            if (projectId != null)
            {
                await _hubContext.Clients.Group(projectId).SendAsync("CommentDeleted", taskId, commentId);

                    // Log activity
                    await _activityLogService.RecordActivityAsync(
                        projectId,
                        taskId,
                        userId,
                        "deleted a comment",
                        taskRes.Title
                    );
            }
            return Ok(new { message = "Comment deleted successfully." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Error deleting comment: " + ex.Message });
        }
    }
}