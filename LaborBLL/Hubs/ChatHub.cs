using Microsoft.AspNetCore.SignalR;


namespace LaborBLL.Hubs
{
    public class ChatHub : Hub
    {
        private readonly IMessageService messageService;

        public ChatHub(IMessageService messageService)
        {
            this.messageService = messageService;
        }

        // لما الـ User يفتح صفحة الحجز
        public async Task JoinBookingGroup(int bookingId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"booking-{bookingId}");
        }

        // لما الـ User يبعت رسالة
        public async Task SendMessage(int bookingId, string senderId, string content)
        {
            // احفظ الرسالة في الـ Database
            await messageService.SendMessageAsync(bookingId, senderId, content);

            // ابعت الرسالة لكل الناس في الـ Group
            await Clients.Group($"booking-{bookingId}").SendAsync("ReceiveMessage", new
            {
                senderId,
                content,
                sentAt = DateTime.UtcNow
            });
        }

        // لما الـ User يخرج من الصفحة
        public async Task LeaveBookingGroup(int bookingId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"booking-{bookingId}");
        }
    }
}