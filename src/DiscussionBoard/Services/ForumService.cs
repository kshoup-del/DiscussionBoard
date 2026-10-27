using DiscussionBoard.Data;
using DiscussionBoard.Models;
using Microsoft.EntityFrameworkCore;

namespace DiscussionBoard.Services
{
    public class ForumService
    {
        private readonly ApplicationDbContext _context;

        public ForumService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Forum>> GetAllAsync()
        {
            return await _context.Forums
                .Where(forum => !forum.IsDeleted)
                .ToListAsync();
        }


        public async Task<Forum?> GetByIdAsync(int id)
        {
            return await _context.Forums
                .Where(forum => forum.Id == id && !forum.IsDeleted)
                .Include(forum => forum.DiscussionThreads
                    .Where(thread => !thread.IsDeleted))
                .ThenInclude(thread => thread.ApplicationUser)
                .FirstOrDefaultAsync();
        }
    }
}
