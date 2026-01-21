using BackEndFolio.API.Hubs;
using BackEndFolio.Models;
using Microsoft.AspNetCore.SignalR;
using Supabase;

namespace BackEndFolio.Services
{
    public interface IActivityLogService
    {
        Task RecordActivityAsync(string projectId, string? taskId, string? userId, string action, string target);
    }

    public class ActivityLogService : IActivityLogService
    {
        private readonly Client _supabase;
        private readonly IHubContext<AppHub> _hubContext;

        public ActivityLogService(Client supabase, IHubContext<AppHub> hubContext)
        {
            _supabase = supabase;
            _hubContext = hubContext;
        }

        public async Task RecordActivityAsync(string projectId, string? taskId, string? userId, string action, string target)
        {
            try
            {
                var log = new ActivityLog
                {
                    Id = Guid.NewGuid().ToString(),
                    ProjectId = projectId,
                    TaskId = taskId,
                    UserId = userId,
                    Action = action,
                    Target = target,
                    CreatedAt = DateTime.UtcNow
                };

                await _supabase.From<ActivityLog>().Insert(log);

                // Broadcast via SignalR - Only send data fields to avoid serializing BaseModel metadata
                await _hubContext.Clients.Group(projectId).SendAsync("ActivityLogAdded", new {
                    id = log.Id,
                    projectId = log.ProjectId,
                    taskId = log.TaskId,
                    userId = log.UserId,
                    action = log.Action,
                    target = log.Target,
                    createdAt = log.CreatedAt
                });
            }
            catch (Exception ex)
            {
                // Log error but don't break the main flow
                Console.WriteLine($"Error recording activity log: {ex.Message}");
            }
        }
    }
}
