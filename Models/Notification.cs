using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
namespace BackEndFolio.Models
{
    [Table("notifications")]
    public class Notification : BaseModel
    {
        [PrimaryKey("id", false)]
        [Column("id")]
        public string Id { get; set; }

        [Column("recipient_id")]
        public string RecipientId { get; set; } // Người nhận thông báo

        [Column("actor_id")]
        public string ActorId { get; set; } // Người gây ra (Người comment/assign)

        [Column("resource_id")]
        public string ResourceId { get; set; } // ID của Task hoặc Project liên quan

        [Column("type")]
        public string Type { get; set; } // Vd: ASSIGNED, COMMENT

        [Column("message")]
        public string Message { get; set; }

        [Column("is_read")]
        public bool IsRead { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        // Join để lấy Avatar của người gửi thông báo
        // Lưu ý: Cần chỉ định Foreign Key vì bảng này có 2 cột user (recipient/actor)
        // Tuy nhiên thư viện Supabase C# hiện tại join tự động hơi kém với 2 FK cùng bảng,
        // nên ta chỉ cần map Actor (Người gửi) là quan trọng nhất để hiển thị.
        [Reference(typeof(Profile))]
        public Profile Actor { get; set; }
    }
}
