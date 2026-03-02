using LaborBLL.Hubs;
using LaborBLL.ModelVM;
using LaborDAL.DB;
using LaborDAL.Entities;
using LaborDAL.Repo.Abstract;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace LaborPL.Controllers
{
    public class MessageController : Controller
    {
        private readonly UserManager<AppUser> userManager;
        private readonly IMessageService messageService;
        private readonly IHubContext<ChatHub> hubContext;

        public MessageController(
            UserManager<AppUser> userManager,
            IMessageService messageService,
            IHubContext<ChatHub> hubContext)
        {
            this.userManager = userManager;
            this.messageService = messageService;
            this.hubContext = hubContext;
        }

        // ========== APIs للشات ==========

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] MessageSendViewModel model)
        {
            var senderId = userManager.GetUserId(User);

            // لو مفيش bookingId، جيب آخر حجز مشترك
            if (model.BookingId == 0 && !string.IsNullOrEmpty(model.OtherUserId))
            {
                model.BookingId = await messageService.GetLastBookingIdAsync(senderId, model.OtherUserId);
            }

            if (model.BookingId == 0)
                return Json(new { success = false, message = "No booking found" });

            var result = await messageService.SendMessageAsync(model.BookingId, senderId, model.Content);

            if (!result.Success)
                return Json(new { success = false, message = result.ErrorMessage });

            await hubContext.Clients.Group($"booking-{model.BookingId}").SendAsync("ReceiveMessage", new
            {
                senderId = senderId,
                senderName = User.Identity?.Name ?? "مستخدم",
                content = model.Content,
                sentAt = DateTime.Now,
                bookingId = model.BookingId
            });

            return Json(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> GetMessages(int bookingId)
        {
            try
            {
                var userId = userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                    return Json(new { success = false, message = "يجب تسجيل الدخول" });

                // استخدم الدالة الموجودة GetMessagesByBookingIdAsync
                var result = await messageService.GetMessagesByBookingIdAsync(bookingId, userId);

                if (!result.Success)
                    return Json(new { success = false, message = result.ErrorMessage });

                // تجهيز البيانات للإرسال
                var messages = result.Result?.Select(m => new
                {
                    id = m.Id,
                    content = m.Content,
                    senderId = m.SenderId,
                    senderName = m.SenderName ?? "مستخدم",
                    sentAt = m.SentAt,
                    isRead = m.IsRead
                });

                return Json(new { success = true, messages = messages });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> UnReadCount()
        {
            try
            {
                var userId = userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                    return Json(new { count = 0 });

                var result = await messageService.GetUnreadCountAsync(userId);

                int unreadCount = 0;
                if (result.Success)
                {
                    unreadCount = result.Result;
                }

                return Json(new { count = unreadCount });
            }
            catch
            {
                return Json(new { count = 0 });
            }
        }

        // ========== الصفحات ==========

       
        public async Task<IActionResult> Chat(string otherUserId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId))
                return RedirectToAction("Login", "Account");
            var otheruser=await userManager.FindByIdAsync(otherUserId);

            var messages = await messageService.GetConversationAsync(currentUserId, otherUserId);

            var messagesList = messages.Select(m => new MessageViewMode
            {
                Id = m.Id,
                Content = m.Content ?? "",
                SenderId = m.SenderId ?? "",
                SenderName = $"{m.Sender?.FirstName} {m.Sender?.LastName}",
                SentAt = m.SentAt,
                IsRead = m.IsRead
            }).ToList();

            var viewModel = new ChatViewModel
            {
                OtherUserId = otherUserId,
                OtherUserName = $"{otheruser?.FirstName}{otheruser?.LastName}",
                Messages = messagesList
            };

            return View(viewModel);
        }
        [HttpGet]
        public async Task<IActionResult> GetConversation(string otherUserId)
        {
            var userId = userManager.GetUserId(User);
            var messages = await messageService.GetConversationAsync(userId, otherUserId);

            var result = messages.Select(m => new
            {
                id = m.Id,
                content = m.Content,
                senderId = m.SenderId,
                senderName = $"{m.Sender?.FirstName} {m.Sender?.LastName}",
                sentAt = m.SentAt,
                isRead = m.IsRead
            });

            return Json(new { success = true, messages = result });
        }

    }
}