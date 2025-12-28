using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace BackEndFolio.Models
{
    [Table("project_members")]
    
    public class ProjectMember : BaseModel
    {
        [PrimaryKey("project_id", false)]
        [Column("project_id")]
        public string ProjectId { get; set; }

        [PrimaryKey("user_id", false)]
        [Column("user_id")]
        public string UserId { get; set; }

        [Column("role")]
        public string Role { get; set; }

        // Join bảng Profile
        [Reference(typeof(Profile))]
        public Profile Profile { get; set; }

        // Join bảng Project (để lấy danh sách dự án của user)
        [Reference(typeof(Project))]
        public Project Project { get; set; }
    }

    public class InviteMemberRequest
    {
        public string UserId { get; set; }
        public string? Role { get; set; }
    }

    public class UpdateMemberRoleRequest
    {
        public string Role { get; set; }
    }
}
