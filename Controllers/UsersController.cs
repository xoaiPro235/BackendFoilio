using BackEndFolio.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Supabase.Postgrest;
using System.Security.Claims;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly Supabase.Client _supabase;
    private readonly string _supabaseServiceKey;

    public UsersController(Supabase.Client supabaseClient, IConfiguration configuration)
    {
        _supabase = supabaseClient;
        _supabaseServiceKey = configuration["Supabase:Key"];
    }

    // GET: api/users/exists?email=test@example.com
    [HttpGet("exists")]
    [AllowAnonymous]
    public async Task<IActionResult> CheckEmailExists([FromQuery] string email)
    {
        if (string.IsNullOrEmpty(email)) return BadRequest(new { message = "Email is required" });

        var response = await _supabase
            .From<Profile>()
            .Where(p => p.Email == email)
            .Get();

        return Ok(new { exists = response.Models.Any() });
    }

    // GET: api/users/search?q=alice
    [HttpGet("search")]
    public async Task<IActionResult> SearchUser([FromQuery] string q)
    {
        if (string.IsNullOrEmpty(q)) return Ok(new List<object>());

        var response = await _supabase
          .From<Profile>()
          .Filter("email", Constants.Operator.ILike, $"%{q}%")
          .Get();

        return Ok(response.Models);
    }

    // GET: api/users/all
    [HttpGet("all")]
    public async Task<IActionResult> GetAllUsers()
    {
        var response = await _supabase
            .From<Profile>()
            .Select("*")
            .Get();
        return Ok(response.Models);
    }

    // PATCH: api/users/me
    [HttpPatch("me")]
    public async Task<IActionResult> UpdateProfile([FromBody] ProfileUpdateRequest updatedProfile)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }
        try
        {
            var query = _supabase.From<Profile>().Where(p => p.Id == userId);
            
            if (updatedProfile.Name != null)
            {
                query = query.Set(p => p.Name, updatedProfile.Name.Trim());
            }
            if (updatedProfile.Bio != null)
            {
                query = query.Set(p => p.Bio, updatedProfile.Bio.Trim());
            }
            if (updatedProfile.AvatarUrl != null)
            {
                query = query.Set(p => p.AvatarUrl, updatedProfile.AvatarUrl.Trim());
            }

            var response = await query.Update();
            var profile = response.Models.FirstOrDefault();
            return Ok(profile);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    // DELETE: api/users/me
    [HttpDelete("me")]
    public async Task<IActionResult> DeleteAccount()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        try
        {
            await _supabase.AdminAuth(_supabaseServiceKey).DeleteUser(userId);
            return Ok(new { message = "Account deleted successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }
}
