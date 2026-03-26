using LaborBLL.ModelVM;
using LaborBLL.Response;
using LaborDAL.Entities;
using LaborDAL.Repo.Implementation;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LaborBLL.Service
{
    public class ChatService : IchatService
    {
     
            private readonly IUnitOfWork unitOfWork;
            private readonly UserManager<AppUser> _userManager; // ✅ أضف

            public ChatService(IUnitOfWork unitOfWork, UserManager<AppUser> userManager) // ✅ أضف
            {
                this.unitOfWork = unitOfWork;
                _userManager = userManager; // ✅ أضف
            }

            // دالة مساعدة للتحويل من ChatUsers إلى MessageViewMode
            private MessageViewMode MapToMessageViewModel(ChatUsers message)
        {
            return new MessageViewMode
            {
                Id = message.Id,
                Content = message.Content,
                SenderId = message.SenderId,
                ReceiverId = message.ReceiverId,
                SenderName = message.Sender != null ? $"{message.Sender.FirstName} {message.Sender.LastName}" : "مستخدم",
                ReceiverName = message.Receiver != null ? $"{message.Receiver.FirstName} {message.Receiver.LastName}" : "مستخدم",
                SentAt = message.CreatedAt,
                IsRead = message.isread ?? false
            };
        }

        // جلب جهات الاتصال (المستخدمين اللي تواصلت معاهم)
        public async Task<Response<List<ContactViewModel>>> GetContactAsync(string userId)
        {
            try
            {
                var messages = await unitOfWork.chatrepo.GetmessageByIdAsync(userId);
                if (messages == null || !messages.Any())
                {
                    return new Response<List<ContactViewModel>>(new List<ContactViewModel>(), true, "لا توجد رسائل");
                }

                // ✅ جيب الـ Admin IDs
                var admins = await _userManager.GetUsersInRoleAsync("Admin");
                var adminIds = admins.Select(a => a.Id).ToHashSet();

                var contacts = messages
                    .GroupBy(m => m.SenderId == userId ? m.ReceiverId : m.SenderId)
                    .Select(g =>
                    {
                        var lastMessage = g.OrderByDescending(m => m.CreatedAt).FirstOrDefault();
                        if (lastMessage == null) return null;

                        var otherUser = lastMessage.SenderId == userId ? lastMessage.Receiver : lastMessage.Sender;

                        string fullName = "مستخدم";
                        if (otherUser != null)
                        {
                            fullName = $"{otherUser.FirstName ?? ""} {otherUser.LastName ?? ""}".Trim();
                            if (string.IsNullOrEmpty(fullName))
                                fullName = "مستخدم";
                        }

                        return new ContactViewModel
                        {
                            OtherUserId = g.Key,
                            FullName = fullName,
                            LastMessage = lastMessage.Content ?? "",
                            LastMessageAt = lastMessage.CreatedAt,
                            UnreadCount = g.Count(m => m.SenderId != userId && m.isread != true),
                            IsAdmin = adminIds.Contains(g.Key) ,// ✅
                                ProfilePictureUrl = otherUser?.ProfilePictureUrl // ✅ أضف ده

                        };
                    })
                    .Where(c => c != null)
                    .OrderByDescending(c => c.LastMessageAt)
                    .ToList();

                return new Response<List<ContactViewModel>>(contacts, true, null);
            }
            catch (Exception ex)
            {
                return new Response<List<ContactViewModel>>(new List<ContactViewModel>(), false, ex.Message);
            }
        }

        // جلب جهات اتصال جديدة
        public async Task<Response<List<ContactViewModel>>> GetNewContact(string userId)
        {
            try
            {
                var newContactsUsers = await unitOfWork.chatrepo.GetNewContact(userId);
                var newContacts = new List<ContactViewModel>();

                // ✅ جيب الـ Admin IDs
                var admins = await _userManager.GetUsersInRoleAsync("Admin");
                var adminIds = admins.Select(a => a.Id).ToHashSet();

                if (newContactsUsers != null && newContactsUsers.Any())
                {
                    newContacts = newContactsUsers
                        .Where(u => u != null && u.Id != userId)
                        .Select(u => new ContactViewModel
                        {
                            OtherUserId = u.Id,
                            FullName = $"{u.FirstName ?? ""} {u.LastName ?? ""}".Trim(),
                            LastMessage = "",
                            LastMessageAt = DateTime.Now,
                            UnreadCount = 0,

                            IsAdmin = adminIds.Contains(u.Id) // ✅
                        })
                        .ToList();
                }

                return new Response<List<ContactViewModel>>(newContacts, true, null);
            }
            catch (Exception ex)
            {
                return new Response<List<ContactViewModel>>(new List<ContactViewModel>(), false, ex.Message);
            }
        }
        // جلب محادثة كاملة بين مستخدمين
        public async Task<Response<ChatViewModel>> GetConversationAsync(string userId, string otherUserId)
        {
            try
            {
                var messages = await unitOfWork.chatrepo.GetConversationAsync(userId, otherUserId);

                // تحويل الرسائل من ChatUsers إلى MessageViewMode
                var messageViewModels = messages.Select(m => MapToMessageViewModel(m)).ToList();

                var otherUser = messages.FirstOrDefault()?.Receiver;
                if (otherUser == null)
                {
                    otherUser = messages.FirstOrDefault()?.Sender;
                }

                var chatViewModel = new ChatViewModel
                {
                    OtherUserId = otherUserId,
                    OtherUserName = otherUser != null ? $"{otherUser.FirstName} {otherUser.LastName}" : "مستخدم",
                    Messages = messageViewModels // هنا بقينا نستخدم MessageViewMode
                };

                return new Response<ChatViewModel>(chatViewModel, true, null);
            }
            catch (Exception ex)
            {
                return new Response<ChatViewModel>(new ChatViewModel(), false, ex.Message);
            }
        }

        // إرسال رسالة جديدة
        public async Task<Response<bool>> SendMessageAsync(string senderId, string receiverId, string content)
        {
            try
            {
                var message = new ChatUsers
                {
                    SenderId = senderId,
                    ReceiverId = receiverId,
                    Content = content,
                    CreatedAt = DateTime.UtcNow,
                    isread = false
                };

                await unitOfWork.chatrepo.AddMessageAsync(message);
                await unitOfWork.SaveAsync();

                return new Response<bool>(true, true, null);
            }
            catch (Exception ex)
            {
                return new Response<bool>(false, false, ex.Message);
            }
        }

        // جلب عدد الرسائل غير المقروءة
        public async Task<Response<int>> GetCountUnReadAsync(string userId)
        {
            try
            {
                var count = await unitOfWork.chatrepo.GetUnreadCountAsync(userId);
                return new Response<int>(count, true, null);
            }
            catch (Exception ex)
            {
                return new Response<int>(0, false, ex.Message);
            }
        }
        public async Task<List<int>>GetMessageRecivedmyinconversationAsync(string userId,string otheuseid)
        {
            var messsage= await unitOfWork.chatrepo.GetConversationResivedmeAsync(userId, otheuseid);
           List<int> l=new List<int> ();
            foreach (var item in messsage)
            {
                l.Add(item.Id);
            }
            return l;
        }
        // وضع علامة مقروء على رسائل
        public async Task<Response<bool>> MarkAsReadAsync(List<int> messageIds)
        {
            try
            {
                await unitOfWork.chatrepo.MarkAsReadAsync(messageIds);
                await unitOfWork.SaveAsync();
                return new Response<bool>(true, true, null);
            }
            catch (Exception ex)
            {
                return new Response<bool>(false, false, ex.Message);
            }
        }

    }
}