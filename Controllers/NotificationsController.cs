using BackEndFolio.API.Hubs;
using BackEndFolio.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Supabase.Postgrest;
using System.Security.Claims;


[ApiController]
[Route("api/notifications")]
[Authorize] 
public class NotificationsController : ControllerBase
{
    private readonly Supabase.Client _supabase;

    public NotificationsController(Supabase.Client supabase)
    {
        _supabase = supabase;
    }

    // 1. Lấy danh sách thông báo của TÔI
    // GET: api/notifications?limit=20
    [HttpGet]
    public async Task<IActionResult> GetMyNotifications([FromQuery] int limit = 20)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        try
        {
            var response = await _supabase.From<Notification>()
                // Lấy thông báo + Thông tin người gửi (Actor)
                // Cú pháp: profiles!actor_id nghĩa là join bảng profiles thông qua khóa ngoại actor_id
                .Select("*")
                .Where(x => x.UserId == userId) // Chỉ lấy của mình
                .Order("created_at", Constants.Ordering.Descending) // Mới nhất lên đầu
                .Limit(limit)
                .Get();

            return Ok(response.Models);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // 2. Đánh dấu một thông báo là "Đã đọc"
    // PUT: api/notifications/{id}/read
    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkAsRead(string id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        try
        {
            // Update cột IsRead = true
            await _supabase.From<Notification>()
                .Where(x => x.Id == id && x.UserId == userId) // Security: Chỉ chủ sở hữu mới được đánh dấu
                .Set(x => x.IsRead, true)
                .Update();

            return Ok(new { message = "Marked as read" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // 3. Đánh dấu TẤT CẢ là "Đã đọc" (Nút "Mark all as read")
    // PUT: api/notifications/read-all
    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        try
        {
            await _supabase.From<Notification>()
                .Where(x => x.UserId == userId && x.IsRead == false)
                .Set(x => x.IsRead, true)
                .Update();

            return Ok(new { message = "All notifications marked as read" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
