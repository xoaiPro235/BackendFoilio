using BackEndFolio.Models;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

[Table("notifications")]
public class Notification : BaseModel
{
    [PrimaryKey("id", false)]
    public string Id { get; set; }

    [Column("message")]
    public string Message { get; set; }

    [Column("type")]
    public string Type { get; set; }

    [Column("title")]
    public string Title { get; set; }

    [Column("link")]
    public string Link { get; set; }

    [Column("is_read")]
    public bool IsRead { get; set; }

    [Column("user_id")]
    public string UserId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}