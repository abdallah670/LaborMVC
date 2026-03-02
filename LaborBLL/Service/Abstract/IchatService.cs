using LaborBLL.ModelVM;
using LaborBLL.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaborBLL.Service
{
    public interface IchatService
    {
        // جلب جميع جهات الاتصال (المحادثات النشطة)
        Task<Response<List<ContactViewModel>>> GetContactAsync(string userId);

        // جلب جهات اتصال جديدة
        Task<Response<List<ContactViewModel>>> GetNewContact(string userId);

        // جلب محادثة كاملة بين مستخدمين
        Task<Response<ChatViewModel>> GetConversationAsync(string userId, string otherUserId);
        Task<List<int>> GetMessageRecivedmyinconversationAsync(string userId, string otherUserId);

        // إرسال رسالة جديدة
        Task<Response<bool>> SendMessageAsync(string senderId, string receiverId, string content);

        // جلب عدد الرسائل غير المقروءة
        Task<Response<int>> GetCountUnReadAsync(string userId);

        // وضع علامة مقروء على مجموعة رسائل
        Task<Response<bool>> MarkAsReadAsync(List<int> messageIds);
    }
}