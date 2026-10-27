using DiscussionBoard.Data;
using DiscussionBoard.Models;
using Microsoft.EntityFrameworkCore;

namespace DiscussionBoard.Services;

public class PostService
{
    private readonly ApplicationDbContext _context;

    public PostService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Post>> GetForThreadAsync(int threadId)
    {
        // One database query: retrieve all non-deleted posts
        // belonging to this discussion thread.
        var posts = await _context.Posts
            .Where(p =>
                p.DiscussionThreadId == threadId &&
                !p.IsDeleted)
            .Include(p => p.ApplicationUser)
            .ToListAsync();

        // Sort in memory, because SQLite does not support
        // ordering DateTimeOffset values directly.
        return posts
            .OrderBy(p => p.CreatedAt)
            .ToList();
    }

    public async Task AddAsync(Post post)
    {
        _context.Posts.Add(post);
        await _context.SaveChangesAsync();
    }
}
