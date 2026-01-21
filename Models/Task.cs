using Microsoft.AspNetCore.Mvc;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System.Net.Mail;
using System.Text.Json.Serialization;
using System.Xml.Linq;
namespace BackEndFolio.Models
{
    [Table("tasks")]
    public class TaskItem : BaseModel
    {
        [PrimaryKey("id", false)]
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
        public DateOnly? StartDate { get; set; }

        [Column("due_date")]
        public DateOnly? DueDate { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Reference(typeof(Attachment), ReferenceAttribute.JoinType.Left)]
        public List<Attachment> Attachments { get; set; } = new List<Attachment>();

        [Reference(typeof(Comment), ReferenceAttribute.JoinType.Left)]
        public List<Comment> Comments { get; set; } = new List<Comment>();
    }

    public class TaskItemResponse
    {
        public string Id { get; set; }
        public string ProjectId { get; set; }
        public string? ParentTaskId { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public string? Status { get; set; }
        public string? Priority { get; set; }
        public string? AssigneeId { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? DueDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<Attachment> Attachments { get; set; }
        public List<Comment> Comments { get; set; }

    }

    public class CreateTaskRequest
    {
        public string ProjectId { get; set; }
        public string? ParentTaskId { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public string? Status { get; set; } = "TODO";
        public string? Priority { get; set; } = "MEDIUM";
        public string? AssigneeId { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? DueDate { get; set; }
    }

    public class PatchTaskRequest
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Status { get; set; }
        public string? Priority { get; set; }
        public string? AssigneeId { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? DueDate { get; set; }
    }
}
