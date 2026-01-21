using BackEndFolio.API.Hubs;
using BackEndFolio.Models;
using BackEndFolio.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using static Supabase.Postgrest.Constants;



[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ProjectController : ControllerBase
{
    private readonly Supabase.Client _supabase;
    private readonly IHubContext<AppHub> _hubcontext;
    private readonly IActivityLogService _activityLogService;

    public ProjectController(Supabase.Client supabaseClient, IHubContext<AppHub> hubContext, IActivityLogService activityLogService)
    {
        _supabase = supabaseClient;
        _hubcontext = hubContext;
        _activityLogService = activityLogService;
    }

    // GET: api/projects
    [HttpGet]
    public async Task<IActionResult> GetProjects()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var myMemberships = await _supabase
        .From<ProjectMember>()
        .Select("project_id")
        .Filter("user_id", Operator.Equals, userId)
        .Get();

        var projectIds = myMemberships.Models
            .Select(m => m.ProjectId)
            .ToList();


        var res = await _supabase
        .From<ProjectMember>()
        .Select("role")
        .Filter("project_id", Operator.In, projectIds)
        .Get();

        var rows = res.Models;


        var result = rows
        .GroupBy(r => r.Project.Id)
        .Select(g => new
        {
            Project = g.First().Project,
            Members = g.Select(x => new
            {
                x.Profile.Id,
                x.Profile.Name,
                x.Profile.Email,
                x.Profile.AvatarUrl,
                x.Profile.Bio,
                x.Profile.IsOnline,
                x.Role
            }).ToList()
        })
        .ToList();

        return Ok(result);


    }

    // POST: api/projects (Create new project)
    [HttpPost]
    public async Task<IActionResult> CreateProject([FromBody] CreateProject projectCreateRequest)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var project = new Project
        {
            Name = projectCreateRequest.Name,
            Description = projectCreateRequest.Description,
            OwnerId = userId,
            CreatedAt = DateTime.UtcNow
        };

        // 1. Tạo Project và YÊU CẦU TRẢ VỀ DỮ LIỆU ĐẦY ĐỦ
        var resProject = await _supabase.From<Project>()
            .Insert(project);

        var newProject = resProject.Models.FirstOrDefault();

        var member = new ProjectMember
        {
            ProjectId = newProject.Id,
            UserId = userId,
            Role = "OWNER"
        };

        await _supabase.From<ProjectMember>().Insert(member);

        // Log activity
        await _activityLogService.RecordActivityAsync(
            newProject.Id,
            null,
            userId,
            "CREATED",
            "Project"
        );

        return Ok(newProject);
    }


    // GET: api/projects/{id}/members
    [HttpGet("{id}/members")]
    public async Task<IActionResult> GetProjectMembers(string id)
    {
        var respone = await _supabase
            .From<ProjectMember>()
            .Select("*")
            .Where(x => x.ProjectId == id)
            .Get();

        var members = respone.Models.Select(m => new
        {
            m.Profile.Id,
            m.Profile.Name,
            m.Profile.Email,
            m.Profile.AvatarUrl,
            m.Role
        })
        .ToList();
        return Ok(members);
    }

    // POST: api/projects/{id}/members (Add member to project)
    [HttpPost("{id}/members")]
    public async Task<IActionResult> InviteMember(string id, [FromBody] InviteMemberRequest rq)
    {
        var member = new ProjectMember()
        {
            UserId = rq.UserId,
            ProjectId = id,
            Role = rq.Role
        };
        
        var response = await _supabase.From<ProjectMember>().Insert(member);
        var newMember = response.Models.FirstOrDefault();
        // Bắn SignalR báo user đó biết (Optional)
        await _hubcontext.Clients.User(member.UserId).SendAsync("Notification", "You were invited...");

        // Log activity
        var inviterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        await _activityLogService.RecordActivityAsync(
            id,
            null,
            inviterId,
            "INVITED",
            $"User {rq.UserId}"
        );

        return Ok(newMember);
    }

    // DELETE: api/projects/{id}/members/{userId} (Remove member from project)
    [HttpDelete("{id}/members/{userId}")]
    public async Task<IActionResult> RemoveMember(string id, string userId)
    {
        await _supabase
            .From<ProjectMember>()
            .Where(x => x.ProjectId == id && x.UserId == userId)
            .Delete();

        // Log activity
        var removerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        await _activityLogService.RecordActivityAsync(
            id,
            null,
            removerId,
            "REMOVED",
            $"User {userId}"
        );

        return Ok(new { message = "Member removed successfully" });
    }

    // DELETE: api/projects/{id} (Delete project)
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProject(string id)
    {
        await _supabase
            .From<Project>()
            .Where(x => x.Id == id)
            .Delete();
        return Ok(new { message = "Project deleted successfully" });
    }

    // PATCH: api/projects/{id}/members/{userId} (Update member role)
    [HttpPatch("{id}/members/{userId}")]
    public async Task<IActionResult> UpdateMemberRole(string id, string userId,[FromBody] UpdateMemberRoleRequest request)
    {
        var response = await _supabase
            .From<ProjectMember>()
            .Where(x => x.UserId == userId && x.ProjectId == id)
            .Get();

        var member = response.Models.FirstOrDefault();

        if (member == null)
        {
            return NotFound();
        }

        member.Role = request.Role;

        await _supabase.From<ProjectMember>().Update(member);

        return Ok(member.Role);
    }
}

