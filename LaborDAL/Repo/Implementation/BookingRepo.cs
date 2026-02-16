using LaborDAL.DB;
using LaborDAL.Entities;
using LaborDAL.Repo.Abstract;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaborDAL.Repo.Implementation
{
    public class BookingRepo : Repository<Booking>, IBookingRepo
    {

        public BookingRepo(ApplicationDbContext context) : base(context)
        {
         
        }
      
       
        public Task<List<Booking>> GetBookingsByPosterIdAsync(string posterId)
        {
            throw new NotImplementedException();
        }

        public Task<List<Booking>> GetBookingsByWorkerIdAsync(string workerId)
        {
            throw new NotImplementedException();
        }

        public Task<List<Booking>> GetOverlappingBookingsAsync(int workerId, DateTime start, DateTime end)
        {
            throw new NotImplementedException();
        }

    }
}
