using LaborBLL.Hubs;
using LaborBLL.ModelVM;
using LaborBLL.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace LaborPL.Controllers
{
    public class ChatController : Controller
    {
        private readonly IchatService chatService;
        private readonly IHubContext<DirectChatHub> hubContext;

        public ChatController(IchatService chatService, IHubContext<DirectChatHub> hubContext)
        {
            this.chatService = chatService;
            this.hubContext = hubContext;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var contactsResponse = await chatService.GetContactAsync(userId);
            var newcontacts = await chatService.GetNewContact(userId);
            ViewBag.Newcontacts = newcontacts.Result;
            var countResponse = await chatService.GetCountUnReadAsync(userId);
            ViewBag.Conversation = countResponse.Result;
            return View(contactsResponse.Result);
        }

        public async Task<IActionResult> Conversation(string otherUserId)
        {
            if (string.IsNullOrEmpty(otherUserId))
            {
                return RedirectToAction("Index");
            }
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var message = await chatService.GetMessageRecivedmyinconversationAsync(userId, otherUserId);
            await chatService.MarkAsReadAsync(message);


            var conversation = await chatService.GetConversationAsync(userId, otherUserId);
            return View(conversation.Result);
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageModel model)
        {
            try
            {
                if (model == null || string.IsNullOrEmpty(model.receiverId) || string.IsNullOrEmpty(model.content))
                {
                    return Json(new { success = false, error = "البيانات ناقصة" });
                }

                var senderId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrEmpty(senderId))
                {
                    return Json(new { success = false, error = "المستخدم غير مسجل" });
                }

                await chatService.SendMessageAsync(senderId, model.receiverId, model.content);

                var roomName = GetDirectRoomName(senderId, model.receiverId);
                await hubContext.Clients.Group(roomName)
                    .SendAsync("ReceiveMessage", senderId, model.content, DateTime.UtcNow);

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var countResponse = await chatService.GetCountUnReadAsync(userId);
            return Json(countResponse.Result);
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsRead(List<int> messageIds)
        {
            await chatService.MarkAsReadAsync(messageIds);
            return Json(new { success = true });
        }

        private string GetDirectRoomName(string user1, string user2)
        {
            var ordered = new[] { user1, user2 }.OrderBy(x => x).ToList();
            return $"direct-{ordered[0]}-{ordered[1]}";
        }
    }

    public class SendMessageModel
    {
        public string receiverId { get; set; } = string.Empty;
        public string content { get; set; } = string.Empty;
    }
}