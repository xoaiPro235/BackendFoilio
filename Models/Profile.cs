using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
namespace BackEndFolio.Models
{
    [Table("profiles")]
    public class Profile : BaseModel
    {
        [PrimaryKey("id", false)]
        [Column("id")]
        public string Id { get; set; }
        [Column("name")]
        public string? Name { get; set; }
        [Column("email")]
        public string? Email { get; set; }
        [Column("avatar_url")]
        public string? AvatarUrl { get; set; }
        [Column("bio")]
        public string? Bio { get; set; }
        [Column("is_online")]
        public bool IsOnline { get; set; } = false;
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }

    public class ProfileUpdateRequest
    {
        public string? Name { get; set; }
        public string? AvatarUrl { get; set; }
        public string? Bio { get; set; }
    }
}
