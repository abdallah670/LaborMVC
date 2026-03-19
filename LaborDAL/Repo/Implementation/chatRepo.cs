

    namespace LaborDAL.Repo.Implementation
    {
        public class chatRepo : Repository<ChatUsers>, IchatRepo
        {
            public chatRepo( ApplicationDbContext  context) : base(context)
            { }
            public async Task AddMessageAsync(ChatUsers Message)
            {
                await _dbSet.AddAsync(Message);
            }

        public async Task<List<ChatUsers>> GetConversationAsync(string userId1, string userId2)
        {
            return await _dbSet.Include(p => p.Sender).Include(o => o.Receiver).Where(m => m.SenderId == userId1 && m.ReceiverId == userId2 || m.SenderId == userId2 && m.ReceiverId == userId1).OrderBy(m => m.CreatedAt).ToListAsync();
        }
        public async Task<List<ChatUsers>> GetConversationResivedmeAsync(string userId1, string userId2)
        {
            return await _dbSet.Include(p => p.Sender).Include(o => o.Receiver).Where(m =>  m.SenderId == userId2 && m.ReceiverId == userId1).OrderBy(m => m.CreatedAt).ToListAsync();
        }

        public async Task<ChatUsers> GetLastmessageAsync(string userId1, string userId2)
            {
                return await _dbSet.Where(m => m.SenderId == userId1 && m.ReceiverId == userId2 || (m.SenderId == userId2 && m.ReceiverId == userId1)).OrderByDescending(m => m.CreatedAt).FirstOrDefaultAsync();

            }

            public async Task<List<ChatUsers>> GetmessageByIdAsync(string userId)
            {
               return await _dbSet.Include(m=>m.Sender).Include(m=>m.Receiver) .Where(m=>m.SenderId==userId||m.ReceiverId==userId).OrderBy(p=>p.CreatedAt).ToListAsync();
            }

        public async Task<List<AppUser>> GetNewContact(string userId)
        {
            var existingChatUserIds = await _dbSet
                .Where(m => m.SenderId == userId || m.ReceiverId == userId)
                .Select(m => m.SenderId == userId ? m.ReceiverId : m.SenderId)
                .Distinct()
                .ToListAsync();

            var newContactIds = await _context.Bookings
                .Where(b => b.PosterId == userId || b.WorkerId == userId)
                .Select(b => b.PosterId == userId ? b.WorkerId : b.PosterId)
                .Where(id => id != userId)
                .Distinct()
                .ToListAsync();

            var filteredIds = newContactIds
                .Where(id => !existingChatUserIds.Contains(id))
                .ToList();

            var bookingContacts = await _context.Users
                .Where(u => filteredIds.Contains(u.Id))
                .ToListAsync();

            var adminRoleId = await _context.Roles
                .Where(r => r.Name == "Admin")
                .Select(r => r.Id)
                .FirstOrDefaultAsync();

            var admins = await _context.Users
                .Where(u => u.Id != userId
                    && !existingChatUserIds.Contains(u.Id)
                    && _context.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == adminRoleId))
                .ToListAsync();

            return bookingContacts.Union(admins).ToList();
        }
        public async Task<List<AppUser>> GetAdminUsersAsync(string userId)
        {
            // جلب كل المستخدمين اللي عندهم صلاحية Admin (بأي درجة)
            return await _context.Users
                .Where(u => u.Id != userId &&
                           (u.Role.HasFlag(ClientRole.Admin) ||
                            u.Role.HasFlag(ClientRole.AdminWorker) ||
                            u.Role.HasFlag(ClientRole.AdminPoster) ||
                            u.Role.HasFlag(ClientRole.AdminBoth)))
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync(string userId)
            {
                return await  _dbSet.Where(m=>m.ReceiverId == userId&& m.isread!=true).CountAsync();

            }

            public async Task MarkAsReadAsync(List<int> t) 
            {
                var messages=await _dbSet.Where(m=>t.Contains(m.Id)).ToListAsync();
                messages.ForEach(m => m.isread = true);

            }
        }
    }
