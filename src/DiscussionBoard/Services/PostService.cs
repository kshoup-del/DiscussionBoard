using DiscussionBoard.Data;
using DiscussionBoard.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DiscussionBoard.Services;

public class PostService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PostService> _logger;

    public PostService(ApplicationDbContext context)
        : this(context, NullLogger<PostService>.Instance)
    {
    }

    public PostService(
        ApplicationDbContext context,
        ILogger<PostService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<Post>> GetForThreadAsync(int threadId)
    {
        return await _context.Posts
            .Where(p => p.DiscussionThreadId == threadId && !p.IsDeleted)
            .Include(p => p.ApplicationUser)
            .ToListAsync();
    }

    public async Task<Post?> GetAsync(int id)
    {
        return await _context.Posts
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
    }

    public async Task<Post> AddAsync(Post post)
    {
        try
        {
            await _context.Posts.AddAsync(post);
            await _context.SaveChangesAsync();
            return post;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(
                ex,
                "Error adding post to discussion {ThreadId}.",
                post.DiscussionThreadId);

            throw;
        }
    }

    public async Task UpdateAsync(Post post)
    {
        var existingPost = await GetAsync(post.Id);

        if (existingPost == null)
        {
            throw new InvalidOperationException("Post not found.");
        }

        existingPost.Title = post.Title;
        existingPost.Content = post.Content;

        await _context.SaveChangesAsync();
    }
}
