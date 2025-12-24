using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Supabase.Postgrest;
using BackEndFolio.API.Hubs;
using BackEndFolio.Models;
using System.Security.Claims;



[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ProjectController : ControllerBase
{
    private readonly Supabase.Client _supabase;
    //private readonly IHubContext<AppHub> _hubcontext;

    public ProjectController(Supabase.Client supabaseClient)
    {
        _supabase = supabaseClient;
        //_hubcontext = hubContext;
    }

    // GET: api/projects
    [HttpGet]
    public async Task<IActionResult> GetProjects()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var response = await _supabase
            .From<ProjectMember>()
            .Select("*, projects(*)")
            .Where(x => x.UserId == userId)
            .Get();
        var projects = response.Models.Select(m => m.Project).ToList();
        return Ok(projects);
    }

    // POST: api/projects (Create new project)
    [HttpPost]
    public async Task<IActionResult> CreateProject([FromBody] Project project)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        project.OwnerId = userId;

        // 1. Tạo Project
        var resProject = await _supabase.From<Project>().Insert(project);
        var newProject = resProject.Models.FirstOrDefault();


        // 2. Add Owner vào bảng Member luôn
        if (newProject != null)
        {
            var member = new ProjectMember { ProjectId = newProject.Id, UserId = userId, Role = "OWNER" };
            await _supabase.From<ProjectMember>().Insert(member);
        }

        return Ok(newProject);
    }


    // GET: api/projects/{id}/members
    [HttpGet("{id}/members")]
    public async Task<IActionResult> GetProjectMembers(string id)
    {
        var respone = await _supabase
            .From<ProjectMember>()
            .Select("*, profile(*)")
            .Where(x => x.ProjectId == id)
            .Get();

        var members = respone.Models.Select(m => new
        {
            m.Profile.Id,
            m.Profile.Name,
            m.Profile.Email,
            m.Profile.AvatarUrl,
            Role = m.Role
        });

        return Ok(members);
    }

    // POST: api/projects/{id}/members (Add member to project)
    [HttpPost("{id}/members")]
    public async Task<IActionResult> InviteMember( string id, [FromBody] ProjectMember member)
    {
        member.ProjectId = id;
        var response = await _supabase.From<ProjectMember>().Insert(member);
        var newMember = response.Models.FirstOrDefault();
        // Bắn SignalR báo user đó biết (Optional)
        // await _hubContext.Clients.User(member.UserId).SendAsync("Notification", "You were invited...");
        return Ok(newMember);
    }
}

