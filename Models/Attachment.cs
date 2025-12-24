using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
namespace BackEndFolio.Models
{
    [Table("attachments")]
    public class Attachment : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; }

        [Column("task_id")]
        public string TaskId { get; set; }

        [Column("user_id")]
        public string UserId { get; set; } // Người upload

        [Column("file_name")]
        public string FileName { get; set; }

        [Column("file_url")]
        public string FileUrl { get; set; } // Link public từ Storage

        [Column("file_type")]
        public string FileType { get; set; }

        [Column("file_size")]
        public long FileSize { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
