using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace LaborBLL.Hubs
{
    public class DirectChatHub : Hub
    {
        // قاموس لتخزين كل المستخدمين الأونلاين
        private static readonly ConcurrentDictionary<string, string> OnlineUsers = new();

        // لما المستخدم يفتح الموقع
        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;

            // لو المستخدم عامل login، خذي الـ UserId بتاعه
            if (Context.User?.Identity?.IsAuthenticated == true)
            {
                var claimsUserId = Context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(claimsUserId))
                {
                    userId = claimsUserId;
                }
            }

            if (!string.IsNullOrEmpty(userId))
            {
                // إضافة المستخدم للأونلاين
                OnlineUsers.TryAdd(userId, Context.ConnectionId);
                // إرسال إشعار للكل
                await Clients.All.SendAsync("UserOnline", userId);
                Console.WriteLine($"✅ {userId} is now ONLINE");
            }

            await base.OnConnectedAsync();
        }

        // لما المستخدم يقفل الموقع
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.UserIdentifier;

            if (Context.User?.Identity?.IsAuthenticated == true)
            {
                var claimsUserId = Context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(claimsUserId))
                {
                    userId = claimsUserId;
                }
            }

            if (!string.IsNullOrEmpty(userId))
            {
                // إزالة المستخدم من الأونلاين
                OnlineUsers.TryRemove(userId, out _);
                // إرسال إشعار للكل
                await Clients.All.SendAsync("UserOffline", userId);
                Console.WriteLine($"❌ {userId} is now OFFLINE");
            }

            await base.OnDisconnectedAsync(exception);
        }

        // الانضمام لغرفة المحادثة
        public async Task JoinDirectRoom(string currentUserId, string otherUserId)
        {
            // ترتيب الـ IDs عشان الغرفة تكون واحدة للاتنين
            var ordered = new[] { currentUserId, otherUserId }.OrderBy(x => x).ToArray();
            string groupName = $"chat_{ordered[0]}_{ordered[1]}";

            // إضافة المستخدم للغرفة
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

            Console.WriteLine($"📌 User {currentUserId} joined room {groupName}");
        }

        // إرسال إشعار الكتابة
        public async Task SendMessage(string receiverId, string content)
        {
            var senderId = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(senderId)) return;

            var ordered = new[] { senderId, receiverId }.OrderBy(x => x).ToArray();
            string groupName = $"chat_{ordered[0]}_{ordered[1]}";

            await Clients.Group(groupName).SendAsync("ReceiveMessage", senderId, content, DateTime.UtcNow);
        }
        public async Task SendTypingNotification(string receiverId)
        {
            var senderId = Context.UserIdentifier;
            if (Context.User?.Identity?.IsAuthenticated == true)
            {
                senderId = Context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            }

            var ordered = new[] { senderId, receiverId }.OrderBy(x => x).ToArray();
            string groupName = $"chat_{ordered[0]}_{ordered[1]}";
            await Clients.Group(groupName).SendAsync("UserTyping", senderId);
        }

        // دالة للتحقق إذا كان المستخدم أونلاين
        public static bool IsUserOnline(string userId)
        {
            return OnlineUsers.ContainsKey(userId);
        }
    }
}