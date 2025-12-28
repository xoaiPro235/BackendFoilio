using Newtonsoft.Json;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System.Text.Json.Serialization;
namespace BackEndFolio.Models
{
    [Table("projects")]
    public class Project : BaseModel
    {
        [PrimaryKey("id", false)]
        public string? Id { get; set; }

        [Column("name")]
        public string Name { get; set; }

        [Column("description")]
        public string Description { get; set; }

        [Column("owner_id")]
        public string OwnerId { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }

    public class CreateProject
    {
        public string Name { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
