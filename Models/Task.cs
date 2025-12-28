using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System.Net.Mail;
using System.Xml.Linq;
namespace BackEndFolio.Models
{
    [Table("tasks")]
    public class TaskItem : BaseModel
    {
        [PrimaryKey("id", false)]
        [Column("id")]
        public string Id { get; set; }

        [Column("project_id")]
        public string ProjectId { get; set; }

        [Column("parent_task_id")]
        public string? ParentTaskId { get; set; }

        [Column("title")]
        public string Title { get; set; }

        [Column("description")]
        public string? Description { get; set; }

        [Column("status")]
        public string? Status { get; set; }

        [Column("priority")]
        public string? Priority { get; set; }

        [Column("assignee_id")]
        public string? AssigneeId { get; set; }

        [Column("start_date")]
        public DateTime? StartDate { get; set; }

        [Column("due_date")]
        public DateTime? DueDate { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        // Để join lấy thông tin người được gán (nếu cần)
        [Reference(typeof(Profile))]
        public Profile Assignee { get; set; }

        // Để hiển thị file đính kèm
        [Reference(typeof(Attachment))]
        public List<Attachment> Attachments { get; set; }

        // Để hiển thị comment
        [Reference(typeof(Comment))]
        public List<Comment> Comments { get; set; }
    }
}
