using LaborDAL.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaborDAL.Repo
{
    public interface IchatRepo
    {
        Task<List<ChatUsers>> GetmessageByIdAsync(string userId);
        Task<List<ChatUsers>> GetConversationAsync(string userId1, string userId2);
        Task<List<ChatUsers>> GetConversationResivedmeAsync(string userId1, string userId2);
        Task<ChatUsers> GetLastmessageAsync(string userId1, string userId2);
        Task<List<AppUser>> GetNewContact(string userId);
        Task AddMessageAsync(ChatUsers Message);
        Task<int> GetUnreadCountAsync(string userId);
        Task MarkAsReadAsync(List<int> messageIds);
          Task<List<AppUser>> GetAdminUsersAsync(string userId);

    }
}