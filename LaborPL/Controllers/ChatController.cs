using LaborBLL.Hubs;
using LaborBLL.ModelVM;
using LaborBLL.Service;
using LaborBLL.Service.Abstract;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace LaborPL.Controllers
{
    public class ChatController : Controller
    {
        private readonly IchatService _chatService;
        private readonly IHubContext<DirectChatHub> _hubContext;

        public ChatController(IchatService chatService, IHubContext<DirectChatHub> hubContext)
        {
            _chatService = chatService;
            _hubContext = hubContext;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var contactsResponse = await _chatService.GetContactAsync(userId);
            var newcontacts = await _chatService.GetNewContact(userId);
            ViewBag.Newcontacts = newcontacts?.Result ?? new List<ContactViewModel>();

            var countResponse = await _chatService.GetCountUnReadAsync(userId);
            ViewBag.Conversation = countResponse?.Result ?? 0;

            return View(contactsResponse?.Result ?? new List<ContactViewModel>());
        }

        [HttpGet("Chat/Conversation/{otherUserId}")]
        public async Task<IActionResult> Conversation(string otherUserId)
        {
            if (string.IsNullOrEmpty(otherUserId))
            {
                return RedirectToAction("Index");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var messageIds = await _chatService.GetMessageRecivedmyinconversationAsync(userId, otherUserId);
            if (messageIds != null && messageIds.Any())
            {
                await _chatService.MarkAsReadAsync(messageIds);
            }

            var conversation = await _chatService.GetConversationAsync(userId, otherUserId);

            if (conversation?.Result == null)
            {
                return RedirectToAction("Index");
            }
            ViewBag.Contacts = await _chatService.GetContactAsync(userId);
            return View(conversation.Result);
        }

        [HttpPost("Chat/SendMessage")]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageModel model)
        {
            try
            {
                Console.WriteLine($"📨 SendMessage called at {DateTime.Now}");
                Console.WriteLine($"📝 Model: receiverId={model?.receiverId}, content={model?.content}");

                if (model == null || string.IsNullOrEmpty(model.receiverId) || string.IsNullOrEmpty(model.content))
                {
                    return Json(new { success = false, error = "البيانات ناقصة" });
                }

                var senderId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(senderId))
                {
                    return Json(new { success = false, error = "المستخدم غير مسجل" });
                }

                // حفظ الرسالة في قاعدة البيانات
                var result = await _chatService.SendMessageAsync(senderId, model.receiverId, model.content);

                if (!result.Success)
                {
                    return Json(new { success = false, error = result.ErrorMessage ?? "فشل في إرسال الرسالة" });
                }

                var roomName = GetDirectRoomName(senderId, model.receiverId);
                await _hubContext.Clients.Group(roomName)
                    .SendAsync("ReceiveMessage", senderId, model.content, DateTime.UtcNow);

                Console.WriteLine("✅ Message sent successfully");
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception: {ex.Message}");
                return Json(new { success = false, error = ex.Message });
            }
        }

        private string GetDirectRoomName(string user1, string user2)
        {
            var ordered = new[] { user1, user2 }.OrderBy(x => x).ToArray();
            return $"chat_{ordered[0]}_{ordered[1]}";
        }

        [HttpGet("Chat/GetUnreadCount")]
        public async Task<IActionResult> GetUnreadCount()
        {
            try
            {
                Console.WriteLine("📊 GetUnreadCount called");
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var countResponse = await _chatService.GetCountUnReadAsync(userId);
                Console.WriteLine($"📊 Unread count: {countResponse?.Result ?? 0}");
                return Json(new { count = countResponse?.Result ?? 0 });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ GetUnreadCount error: {ex.Message}");
                return Json(new { count = 0 });
            }
        }

        [HttpPost("Chat/MarkAsRead")]
        public async Task<IActionResult> MarkAsRead([FromBody] List<int> messageIds)
        {
            try
            {
                Console.WriteLine($"📌 MarkAsRead called with {messageIds?.Count} messages");
                var result = await _chatService.MarkAsReadAsync(messageIds ?? new List<int>());
                return Json(new { success = result.Success });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ MarkAsRead error: {ex.Message}");
                return Json(new { success = false });
            }
        }
        [HttpGet]
        [Route("Chat/GetUserStatus")]
        public async Task<IActionResult> GetUserStatus(string userId)
        {
            try
            {
                // التحقق من حالة المستخدم من SignalR Hub
                bool isOnline = DirectChatHub.IsUserOnline(userId);

                // جلب آخر ظهور من قاعدة البيانات
              

                return Ok(new
                {
                    isOnline = isOnline,
                });
            }
            catch (Exception ex)
            {
                return Ok(new { isOnline = false });
            }
        }
        [HttpGet("Chat/GetConversationMessages")]
        public async Task<IActionResult> GetConversationMessages(string otherUserId)
        {
            try
            {
                Console.WriteLine($"📜 GetConversationMessages called for otherUserId: {otherUserId}");

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, messages = new List<object>() });
                }

                var conversation = await _chatService.GetConversationAsync(userId, otherUserId);

                if (conversation?.Result?.Messages == null)
                {
                    return Json(new { success = true, messages = new List<object>() });
                }

                var messages = conversation.Result.Messages
                    .OrderBy(m => m.SentAt)
                    .Select(m => new
                    {
                        m.SenderId,
                        m.Content,
                        SentAt = m.SentAt.ToString("yyyy-MM-dd HH:mm:ss")
                    });

                Console.WriteLine($"✅ Found {messages.Count()} messages");

                return Json(new
                {
                    success = true,
                    messages = messages
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ GetConversationMessages error: {ex.Message}");
                return Json(new { success = false, messages = new List<object>() });
            }
        }
       
    }

    public class SendMessageModel
    {
        public string receiverId { get; set; } = string.Empty;
        public string content { get; set; } = string.Empty;
    }
}