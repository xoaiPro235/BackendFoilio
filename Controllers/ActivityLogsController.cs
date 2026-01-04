using BackEndFolio.API.Hubs;
using BackEndFolio.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Supabase.Postgrest; 
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

// Lấy Log của Project
[Route("api/project/{projectId}/activities")]
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
            var response = await _supabase.From<ActivityLog>()

                .Select("*")
                .Where(x => x.ProjectId == projectId)
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