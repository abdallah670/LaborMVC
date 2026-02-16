using LaborDAL.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaborDAL.Repo.Abstract
{
    public interface IBookingRepo : IRepository<Booking>
    {
        Task<List<Booking>> GetBookingsByWorkerIdAsync(string workerId);
        Task<List<Booking>> GetBookingsByPosterIdAsync(string posterId);
   

        Task<List<Booking>> GetOverlappingBookingsAsync(int workerId, DateTime start, DateTime end);

    }
}
