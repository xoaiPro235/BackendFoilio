using System.Net;
using System.Net.Mail;

namespace BackEndFolio.Services
{
    public interface IEmailService
    {
        // Hàm gửi mail chung
        Task SendEmailAsync(string toEmail, string subject, string body);

        // Hàm tiện ích để tạo link task
        Task SendTaskNotificationEmailAsync(string toEmail, string subject, string title, string projectId, string taskId, string actionType = "update");
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // 1. Hàm thực hiện gửi mail vật lý qua SMTP
        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            // Lấy thông tin cấu hình từ appsettings.json
            var host = _configuration["Email:Host"];
            var port = int.Parse(_configuration["Email:Port"] ?? "587");
            var fromEmail = _configuration["Email:From"];
            var password = _configuration["Email:Password"];

            using var client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(fromEmail, password),
                EnableSsl = true
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(fromEmail!),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            mailMessage.To.Add(toEmail);

            await client.SendMailAsync(mailMessage);
        }

        // 2. Hàm xây dựng nội dung mail có chứa Link Task
        public async Task SendTaskNotificationEmailAsync(string toEmail, string subject, string title, string projectId, string taskId, string actionType = "update")
        {
            var frontendUrl = _configuration["AppSettings:FrontendUrl"] ?? "http://localhost:3000";
            string taskUrl = $"{frontendUrl}/project/{projectId}/board?selectedIssue={taskId}";

            string actionMessage = actionType.ToLower() switch
            {
                "assigned" => "You have been assigned a new task.",
                "overdue" => "Your task is overdue. Please check and update the progress.",
                "upcoming" => "You have a task due soon. Please complete it as soon as possible.",
                "commented" => "A new comment has been added to your task.",
                _ => "There is a new update for your task."
            };

            string body = $@"
<div style=""max-width: 600px; margin: 0 auto; font-family: 'Segoe UI', Tahoma, sans-serif; border: 1px solid #e0e0e0; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 12px rgba(0,0,0,0.05);"">
    <div style=""background: linear-gradient(135deg, #6366f1 0%, #a855f7 100%); padding: 30px 20px; text-align: center; color: white;"">
        <h1 style=""margin: 0; font-size: 28px; font-weight: 700; letter-spacing: -0.5px;"">Folio</h1>
        <p style=""margin: 10px 0 0; opacity: 0.9; font-size: 16px;"">Smart Work Management</p>
    </div>
    
    <div style=""padding: 40px 30px; background-color: #ffffff;"">
        <h2 style=""color: #1f2937; margin-top: 0; font-size: 22px;"">{subject}</h2>
        <p style=""color: #4b5563; font-size: 16px; line-height: 1.6;"">Hello,</p>
        <p style=""color: #4b5563; font-size: 16px; line-height: 1.6;"">{actionMessage}</p>
        
        <div style=""background-color: #f9fafb; border-left: 4px solid #6366f1; padding: 20px; margin: 25px 0; border-radius: 4px;"">
            <div style=""margin-bottom: 8px;"">
                <span style=""color: #9ca3af; font-size: 12px; font-weight: 600; text-transform: uppercase;"">Task</span>
                <div style=""color: #111827; font-weight: 600; font-size: 16px;"">{title}</div>
            </div>
            <div>
                <span style=""color: #9ca3af; font-size: 12px; font-weight: 600; text-transform: uppercase;"">Project ID</span>
                <div style=""color: #4b5563; font-size: 14px;"">{projectId}</div>
            </div>
        </div>

        <div style=""text-align: center; margin-top: 35px;"">
            <a href=""{taskUrl}"" style=""display: inline-block; background-color: #6366f1; color: #ffffff; padding: 14px 28px; text-decoration: none; border-radius: 8px; font-weight: 600; font-size: 16px; transition: background-color 0.2s;"">
                View Task Details
            </a>
        </div>
        
        <p style=""margin-top: 30px; color: #9ca3af; font-size: 13px; text-align: center;"">
            Or copy this link: <br>
            <a href=""{taskUrl}"" style=""color: #6366f1; text-decoration: none; word-break: break-all;"">{taskUrl}</a>
        </p>
    </div>
    
    <div style=""background-color: #f9fafb; padding: 20px; text-align: center; border-top: 1px solid #f3f4f6;"">
        <p style=""margin: 0; color: #9ca3af; font-size: 12px;"">
            &copy; {DateTime.UtcNow.Year} Folio System. This is an automated email, please do not reply.
        </p>
    </div>
</div>";

            // Gọi hàm gửi mail thực tế
            await SendEmailAsync(toEmail, subject, body);
        }
    }
}