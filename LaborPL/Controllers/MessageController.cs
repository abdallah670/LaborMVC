
using LaborBLL.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace LaborPL.Controllers
{
    public class MessageController : Controller
    {
        private readonly UserManager<AppUser> userManager;
        private readonly IMessageService messageService;
        private readonly IHubContext<ChatHub> hubContext;

        public MessageController( UserManager<AppUser> userManager ,IMessageService messageService ,IHubContext<ChatHub> hubContext)
        {
            this.userManager = userManager;
            this.messageService = messageService;
            this.hubContext = hubContext;
        }
        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] MessageSendViewModel model)
        {
            // أضف السطر ده للتأكد
            if (model == null || string.IsNullOrEmpty(model.Content))
                return Json(new { success = false, ErrorMessage = "Content is empty" });

            var senderId = userManager.GetUserId(User);
            var message = await messageService.SendMessageAsync(model.BookingId, senderId, model.Content);
            if (!message.Success)
                return Json(new { success = false, message.ErrorMessage });

            await hubContext.Clients.Group($"booking-{model.BookingId}").SendAsync("ReceiveMessage", new
            {
                senderId = senderId,
                senderName = User.Identity.Name,
                content = model.Content,
                sentAt = DateTime.UtcNow.ToString("hh:mm tt")
            });

            return Json(new { success = true });
        
        }
        [HttpGet ]
        public async Task<IActionResult> GetMessages(int bookingId)
            {
            var userId = userManager.GetUserId(User);
            var messages = await messageService.GetMessagesByBookingIdAsync(bookingId,userId);
            if (!messages.Success)
                return Json(new { success = false, messages.ErrorMessage });

            await messageService.MarkAsReadAsync(bookingId, userId);
            return Json(new { success = true, messages=messages.Result });
        }
        [HttpGet]
        public async Task<IActionResult> UnReadCount()
        {
            var userId = userManager.GetUserId(User);
            var count = await messageService.GetUnreadCountAsync(userId);
            return Json(new {  count=count.Result});
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}
