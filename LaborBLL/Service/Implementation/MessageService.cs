using LaborBLL.ModelVM;
using LaborDAL.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaborBLL.Service.Implementation
{
    public class MessageService : IMessageService
    {
        private readonly IUnitOfWork unitOfWork;
        private readonly IMapper mapper;

        public MessageService(IUnitOfWork unitOfWork,IMapper mapper)
        {
            this.unitOfWork = unitOfWork;
            this.mapper = mapper;
        }
        public async Task<Response<IEnumerable<MessageViewMode>>> GetMessagesByBookingIdAsync(int bookingId, string userId)
        {
            var booking = await unitOfWork.Bookings.GetByIdAsync(bookingId);
            if(booking == null)
            {
                return new Response<IEnumerable<MessageViewMode>>(null,false,"Booking NotFound");
            }
            if(booking.WorkerId!=userId && booking.PosterId!=userId)
            {
                return new Response<IEnumerable<MessageViewMode>>(null, false, "You are not part of this booking");
            }
            var message = await unitOfWork.Messages.GetMessagesByBookingIdAsync(bookingId);
            var mes=mapper.Map<IEnumerable<MessageViewMode>>(message);
            return new Response<IEnumerable<MessageViewMode>>(mes, true, null);
        }

        public async Task<Response<int>> GetUnreadCountAsync(string userId)
        {
            var count =await unitOfWork.Messages.GetUnreadCountAsync(userId);
            return new Response<int>(count, false, null);
        }

        public async Task<Response<bool>> MarkAsReadAsync(int bookingId, string userId)
        {
            await unitOfWork.Messages.MarkAsReadAsync(bookingId,userId);
            await unitOfWork.SaveAsync();   
            return new Response<bool>(true,false, null);
        }

        public async Task<Response<bool>> SendMessageAsync(int bookingId, string SenderId, string Content)
        {
            var booking=await unitOfWork.Bookings.GetByIdAsync(bookingId);
            if(booking==null)
            {
                return new Response<bool>(false, false, "Booking Not Found");
            }
            if (booking.WorkerId != SenderId && booking.PosterId != SenderId)
            {
                return new Response<bool>(false, false, "You are not part of this booking");
            }
            var message = new Message
            {
                bookingId = bookingId,
                SenderId = SenderId,
                Content = Content,
                SentAt = DateTime.UtcNow,
                IsRead = false
            };
            await unitOfWork.Messages.AddMessageAsync(message);
            await unitOfWork.SaveAsync();
            return new Response<bool>(true, true, null);

        }
    }
}
