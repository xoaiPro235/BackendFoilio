using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace BackEndFolio.Models
{
    [Table("activity_logs")]
    public class ActivityLog : BaseModel
    {
        [PrimaryKey("id", false)]
        [Column("id")]
        public string Id { get; set; }

        [Column("task_id")]
        public string TaskId { get; set; }

        [Column("user_id")]
        public string? UserId { get; set; } // Có thể null nếu user bị xóa (ON DELETE SET NULL)

        [Column("action")]
        public string Action { get; set; } // Vd: UPDATED, COMMENTED

        [Column("target")]
        public string Target { get; set; } // Vd: Status, Title

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        // Join để lấy tên người làm hành động này
        [Reference(typeof(Profile))]
        public Profile Profile { get; set; }

        [Reference(typeof(TaskItem))]
        public TaskItem Task { get; set; }
    }
}
