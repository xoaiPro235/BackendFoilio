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
