using BackEndFolio.API.Hubs;
using BackEndFolio.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;



// Route: api/tasks/{taskId}/attachments
[Route("api/task/{taskId}/attachments")]
[ApiController]
[Authorize]
public class AttachmentsController : ControllerBase
{
    private readonly Supabase.Client _supabase;

    // Tên bucket phải khớp với tên bucket bạn tạo trên Supabase Storage
    private const string BUCKET_NAME = "attachments";

    public AttachmentsController(Supabase.Client supabase)
    {
        _supabase = supabase;
    }

    // 1. POST: Lưu thông tin file vào DB (Sau khi Frontend upload xong)
    [HttpPost]
    public async Task<IActionResult> SaveAttachmentMetadata(string taskId, [FromBody] AttachmentPayload payload)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        try
        {
            // 1. Tạo object Attachment để lưu DB
            var newFile = new Attachment
            {
                TaskId = taskId,
                UserId = userId, // Người upload
                FileName = payload.FileName,
                FileUrl = payload.FileUrl,
                FileType = payload.FileType,
                FileSize = payload.FileSize,
                CreatedAt = DateTime.UtcNow
            };

            // 2. Insert vào Database
            var response = await _supabase.From<Attachment>().Insert(newFile);
            var insertedFile = response.Models.FirstOrDefault();

            return Ok(insertedFile);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // 2. DELETE: Xóa file khỏi Storage và DB
    // https://[project_id].supabase.co/storage/v1/object/public/[bucket]/[asset-name]
    [HttpDelete("{fileId}")]
    public async Task<IActionResult> DeleteAttachment(string taskId, string fileId)
    {
        try
        {
            // 1. Lấy thông tin file từ DB
            var response = await _supabase
                .From<Attachment>()
                .Where(x => x.Id == fileId && x.TaskId == taskId) // Check đúng task cho an toàn
                .Get();

            var attachment = response.Models.FirstOrDefault();
            if (attachment == null) return NotFound(new { message = "File not found" });

            // 2. Parse URL để lấy đường dẫn thực trên Storage (Path)
            // URL VD: .../storage/v1/object/public/attachments/folder/abc.png
            // Cần lấy: folder/abc.png
            string storagePath = GetStoragePathFromUrl(attachment.FileUrl);

            // 3. Xóa trên Storage trước (Quan trọng: Xóa file vật lý trước)
            if (!string.IsNullOrEmpty(storagePath))
            {
                await _supabase.Storage
                    .From(BUCKET_NAME)
                    .Remove(new List<string> { storagePath });
            }

            // 4. Xóa record trong Database
            await _supabase.From<Attachment>().Where(x => x.Id == fileId).Delete();

            return Ok(new { message = "Deleted successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }


    // Hàm tách chuỗi URL (Như của bạn, đã tinh chỉnh một chút)
    private string GetStoragePathFromUrl(string url)
    {
        if (string.IsNullOrEmpty(url)) return string.Empty;

        // URL Supabase thường có dạng: .../public/{BUCKET_NAME}/{PATH}
        var token = $"/public/{BUCKET_NAME}/";
        var index = url.IndexOf(token);

        if (index != -1)
        {
            // Lấy phần phía sau token
            // VD: .../public/attachments/task-1/img.png -> task-1/img.png
            return url.Substring(index + token.Length);
        }
        return string.Empty;
    }
}