using Microsoft.AspNetCore.SignalR;

namespace LaborBLL.Hubs
{
    public class DirectChatHub : Hub
    {
        public async Task JoinDirectRoom(string user1, string user2)
        {
            var roomName = GetDirectRoomName(user1, user2);
            await Groups.AddToGroupAsync(Context.ConnectionId, roomName);
        }

        public async Task SendDirectMessage(string senderId, string receiverId, string message)
        {
            var roomName = GetDirectRoomName(senderId, receiverId);

            // تبعت الرسالة لكل الناس في الغرفة
            await Clients.Group(roomName)
                .SendAsync("ReceiveMessage", new
                {
                    senderId,
                    message,
                    sentAt = DateTime.UtcNow
                });
        }

        private string GetDirectRoomName(string user1, string user2)
        {
            var ordered = new[] { user1, user2 }.OrderBy(x => x).ToList();
            return $"direct-{ordered[0]}-{ordered[1]}";
        }
    }
}