using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace BackEndFolio.API.Hubs
{
    public class OnlineUser
    {
        public string ConnectionId { get; set; }
        public string UserId { get; set; }
        public string FullName { get; set; }
        public string AvatarUrl { get; set; }
    }

    [Authorize]
    public class AppHub : Hub
    {
        // Dictionary chính: ProjectId -> List User
        private static readonly ConcurrentDictionary<string, List<OnlineUser>> _projectUsers = new();

        // Dictionary phụ (Tra cứu ngược): ConnectionId -> ProjectId
        // Giúp tìm ProjectId cực nhanh khi User ngắt kết nối
        private static readonly ConcurrentDictionary<string, string> _connectionProjectMap = new();

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var connectionId = Context.ConnectionId;

            // 1. Tra cứu xem ConnectionId này đang ở Project nào (O(1) thay vì loop)
            if (_connectionProjectMap.TryRemove(connectionId, out var projectId))
            {
                await RemoveUserFromProject(projectId, connectionId);
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task JoinProject(string projectId, string fullName, string avatarUrl)
        {
            var userId = Context.UserIdentifier;
            var connectionId = Context.ConnectionId;

            // 1. Map ConnectionId vào ProjectId ngay
            _connectionProjectMap[connectionId] = projectId;

            // 2. Add vào Group SignalR
            await Groups.AddToGroupAsync(connectionId, projectId);

            var newUser = new OnlineUser
            {
                ConnectionId = connectionId,
                UserId = userId,
                FullName = fullName,
                AvatarUrl = avatarUrl
            };

            // 3. Thêm vào danh sách (Dùng lock để an toàn đa luồng)
            _projectUsers.AddOrUpdate(projectId,
                // Nếu chưa có thì tạo mới
                _ => new List<OnlineUser> { newUser },
                // Nếu có rồi thì update
                (key, currentList) =>
                {
                    lock (currentList) // <--- QUAN TRỌNG: Khóa list lại để không bị conflict
                    {
                        if (!currentList.Any(u => u.ConnectionId == connectionId))
                        {
                            currentList.Add(newUser);
                        }
                    }
                    return currentList;
                });

            // 4. Lấy danh sách để trả về cho người mới (Cũng phải lock để copy an toàn)
            List<OnlineUser> currentUsersInRoom;
            if (_projectUsers.TryGetValue(projectId, out var users))
            {
                lock (users)
                {
                    currentUsersInRoom = users.ToList(); // Copy ra list mới để gửi đi
                }
            }
            else
            {
                currentUsersInRoom = new List<OnlineUser>();
            }

            await Clients.Caller.SendAsync("GetOnlineUsers", currentUsersInRoom);
            await Clients.OthersInGroup(projectId).SendAsync("UserJoined", newUser);
        }

        public async Task LeaveProject(string projectId)
        {
            var connectionId = Context.ConnectionId;

            // Xóa mapping
            _connectionProjectMap.TryRemove(connectionId, out _);

            await RemoveUserFromProject(projectId, connectionId);
        }

        // Hàm helper tách riêng để tái sử dụng
        private async Task RemoveUserFromProject(string projectId, string connectionId)
        {
            await Groups.RemoveFromGroupAsync(connectionId, projectId);

            if (_projectUsers.TryGetValue(projectId, out var users))
            {
                OnlineUser? userToRemove = null;
                bool isEmpty = false;

                lock (users) // <--- QUAN TRỌNG: Lock khi xóa
                {
                    userToRemove = users.FirstOrDefault(u => u.ConnectionId == connectionId);
                    if (userToRemove != null)
                    {
                        users.Remove(userToRemove);
                    }
                    isEmpty = users.Count == 0;
                }

                // Nếu phòng trống thì xóa luôn key khỏi Dictionary cho nhẹ RAM
                if (isEmpty)
                {
                    _projectUsers.TryRemove(projectId, out _);
                }

                if (userToRemove != null)
                {
                    await Clients.Group(projectId).SendAsync("UserLeft", userToRemove.UserId);
                }
            }
        }
    }

}
