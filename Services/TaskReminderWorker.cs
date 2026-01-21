using BackEndFolio.Models;
using BackEndFolio.Services;
using System.Threading.Tasks;

namespace BackEndFolio.Services
{
    public class TaskReminderWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public TaskReminderWorker(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var supabase = scope.ServiceProvider.GetRequiredService<Supabase.Client>();
                    var notiService = scope.ServiceProvider.GetRequiredService<INotificationService>();
                    var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                    var today = DateOnly.FromDateTime(DateTime.UtcNow);
                    var tomorrow = today.AddDays(1);

                    // 1. Xử lý Task quá hạn
                    var overdueResponse = await supabase.From<TaskItem>()
                        .Where(t => t.Status != "DONE" && t.DueDate < today)
                        .Get();

                    foreach (var task in overdueResponse.Models)
                    {
                        // Truyền thêm Type thông báo là ERROR hoặc WARNING cho quá hạn
                        await ProcessNotification(supabase, notiService, emailService, task, "Task Overdue", "ERROR", "overdue");
                    }

                    // 2. Xử lý Task sắp đến hạn (trong 24h tới)
                    var upcomingResponse = await supabase.From<TaskItem>()
                        .Where(t => t.Status != "DONE" && t.DueDate == tomorrow)
                        .Get();

                    foreach (var task in upcomingResponse.Models)
                    {
                        // Truyền thêm Type thông báo là WARNING cho sắp đến hạn
                        await ProcessNotification(supabase, notiService, emailService, task, "Task Due Soon", "WARNING", "upcoming");
                    }
                }

                // Quét định kỳ mỗi 24 tiếng
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }

        private async Task ProcessNotification(Supabase.Client supabase, INotificationService notiService, IEmailService emailService, TaskItem task, string title, string type, string actionType)
        {
            string targetUserId = task.AssigneeId;

            // Nếu không có người nhận, lấy chủ dự án để nhắc nhở
            if (string.IsNullOrEmpty(targetUserId))
            {
                var projectRes = await supabase.From<Project>().Where(p => p.Id == task.ProjectId).Single();
                targetUserId = projectRes?.OwnerId;
            }

            if (!string.IsNullOrEmpty(targetUserId))
            {
                // Tạo link dẫn trực tiếp tới task
                string taskLink = $"/project/{task.ProjectId}/board?selectedIssue={task.Id}";
                string message = $"Task \"{task.Title}\" is due on {task.DueDate?.ToString("dd/MM/yyyy")}.";
                // 1. Gửi thông báo trong App (Khớp với INotificationService mới)
                await notiService.SendDirectNotification(
                    targetUserId,
                    title,
                    message,
                    type,
                    taskLink
                );

                // 2. Lấy email từ Profile để gửi mail nhắc nhở
                var profileRes = await supabase.From<Profile>().Where(u => u.Id == targetUserId).Single();
                if (profileRes?.Email != null)
                {
                    await emailService.SendTaskNotificationEmailAsync(
                        profileRes.Email,
                        title,
                        task.Title,
                        task.ProjectId,
                        task.Id,
                        actionType);
                }

                // 3. Cập nhật trạng thái đã nhắc nhở (Nếu bạn đã thêm cột reminder_sent vào DB)
                // await supabase.From<TaskItem>().Where(t => t.Id == task.Id).Set(t => t.ReminderSent, true).Update();
            }
        }
    }
}