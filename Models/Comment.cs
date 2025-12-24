using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
namespace BackEndFolio.Models
{
    [Table("comments")]
    public class Comment : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; }

        [Column("task_id")]
        public string TaskId { get; set; }

        [Column("user_id")]
        public string UserId { get; set; }

        [Column("content")]
        public string Content { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        // Join để lấy thông tin người comment (Tên, Avatar)
        [Reference(typeof(Profile))]
        public Profile User { get; set; }
    }
}
