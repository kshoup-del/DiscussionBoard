using DiscussionBoard.Data;
using DiscussionBoard.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace DiscussionBoard.Services

{
    public class DiscussionThreadService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DiscussionThreadService> _logger;

        public DiscussionThreadService(ApplicationDbContext context)
            : this(context, NullLogger<DiscussionThreadService>.Instance)
        {
        }
        public DiscussionThreadService(
            ApplicationDbContext context,
            ILogger<DiscussionThreadService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<DiscussionThread?> GetAsync(int id)
        {
            return await _context.DiscussionThreads
                .Include(thread => thread.ApplicationUser)
                .FirstOrDefaultAsync(thread =>
                    thread.Id == id && !thread.IsDeleted);
        }

        public async Task<List<DiscussionThread>> GetForForumAsync(int forumId)
        {
            var threads = await _context.DiscussionThreads
                .Where(thread =>
                    thread.ForumId == forumId && !thread.IsDeleted)
                .Include(thread => thread.ApplicationUser)
                .ToListAsync();

            return threads
                .OrderByDescending(thread => thread.CreatedAt)
                .ToList();
        }
        public async Task<DiscussionThread> AddAsync(
            DiscussionThread thread)
        {
            try
            {
                await _context.DiscussionThreads.AddAsync(thread);
                await _context.SaveChangesAsync();

                return thread;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Database error while adding a discussion thread.");

                throw;
            }
        }

        public async Task<DiscussionThread?> UpdateAsync(
            DiscussionThread thread)
        {
            var existingThread = await _context.DiscussionThreads
                .FirstOrDefaultAsync(existing =>
                    existing.Id == thread.Id && !existing.IsDeleted);

            if (existingThread == null)
            {
                return null;
            }

            // Only update fields the author is permitted to change.
            existingThread.Title = thread.Title;
            existingThread.Content = thread.Content;

            try
            {
                await _context.SaveChangesAsync();
                return existingThread;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Database error while updating discussion thread {ThreadId}.",
                    existingThread.Id);

                throw;
            }
        }
    }
}
