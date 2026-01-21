using BackEndFolio.API.Hubs;
using BackEndFolio.Models;
using Microsoft.AspNetCore.SignalR;

namespace BackEndFolio.Services
{
    public interface INotificationService
    {
        // Gửi cho nhiều người trong dự án (ví dụ: khi có comment)
        Task SendProjectNotification(string projectId, string actorId, string title, string message, string type, string link, bool skipActor = true);

        // Gửi đích danh cho 1 người (ví dụ: khi được assign)
        Task SendDirectNotification(string recipientId, string title, string message, string type, string link);
    }

    public class NotificationService : INotificationService
    {
        private readonly Supabase.Client _supabase;
        private readonly IHubContext<AppHub> _hubContext;

        public NotificationService(Supabase.Client supabase, IHubContext<AppHub> hubContext)
        {
            _supabase = supabase;
            _hubContext = hubContext;
        }

        public async Task SendProjectNotification(string projectId, string actorId, string title, string message, string type, string link, bool skipActor = true)
        {
            // 1. Lấy danh sách thành viên trong project
            var membersResponse = await _supabase.From<ProjectMember>()
                .Where(x => x.ProjectId == projectId)
                .Get();

            var members = membersResponse.Models;

            // 2. Vòng lặp gửi thông báo cho từng thành viên
            foreach (var member in members)
            {
                // Nếu skipActor = true, bỏ qua người thực hiện hành động
                if (skipActor && !string.IsNullOrEmpty(actorId) && 
                    member.UserId.Equals(actorId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Gọi SendDirectNotification 
                await SendDirectNotification(member.UserId, title, message, type, link);
            }
        }

        public async Task SendDirectNotification(string recipientId, string title, string message, string type, string link)
        {
            // Khớp với Model Notification mới bạn đã chốt
            var notification = new Notification
            {
                UserId = recipientId,
                Title = title,
                Message = message,
                Type = type,   // INFO, SUCCESS, WARNING, ERROR
                Link = link,   // Direct URL ví dụ: /projects/1/tasks/10
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            // 1. Lưu vào Database
            var res = await _supabase.From<Notification>().Insert(notification);
            var inserted = res.Models.FirstOrDefault();

            if (inserted != null)
            {
                // 2. Bắn SignalR Real-time tới đúng User nhận thông báo
                // Gửi Anonymous Object để tránh lỗi Serialization với Supabase Models
                await _hubContext.Clients.User(recipientId).SendAsync("ReceiveNotification", new {
                    id = inserted.Id,
                    userId = inserted.UserId,
                    title = inserted.Title,
                    message = inserted.Message,
                    type = inserted.Type,
                    link = inserted.Link,
                    isRead = inserted.IsRead,
                    createdAt = inserted.CreatedAt
                });
            }
        }
    }
}