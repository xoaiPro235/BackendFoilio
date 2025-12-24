using BackEndFolio.API.Hubs;
using BackEndFolio.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Supabase.Postgrest; 
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

// Lấy Log của Project
[Route("api/projects/{projectId}/activities")]
[ApiController]
[Authorize]
public class ActivityLogsController : ControllerBase
{
    private readonly Supabase.Client _supabase;

    public ActivityLogsController(Supabase.Client supabase)
    {
        _supabase = supabase;
    }

    // GET: api/projects/{projectId}/activities?limit=50
    // Lấy lịch sử hoạt động của TOÀN BỘ dự án
    [HttpGet]
    public async Task<IActionResult> GetProjectActivities(string projectId, [FromQuery] int limit = 50)
    {
        try
        {
            // Logic: Lấy ActivityLog JOIN với Tasks. Chỉ giữ lại dòng nào mà Tasks có project_id == projectId
            var response = await _supabase.From<ActivityLog>()
                // 1. Select tất cả cột của Log
                // 2. Join profiles (*) để lấy tên/avatar người làm
                // 3. Join tasks (*) và dùng !inner để bắt buộc task phải tồn tại và thuộc project này
                .Select("*, profiles(*), tasks!inner(id, title, project_id)")

                // 4. Lọc: Cột project_id bên trong bảng tasks phải bằng projectId gửi lên
                .Filter("tasks.project_id", Constants.Operator.Equals, projectId)

                .Order("created_at", Constants.Ordering.Descending)
                .Limit(limit)
                .Get();

            return Ok(response.Models);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Lỗi khi lấy logs: " + ex.Message });
        }
    }
}