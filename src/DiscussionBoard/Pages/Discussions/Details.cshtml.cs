using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using DiscussionBoard.Models;
using DiscussionBoard.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DiscussionBoard.Pages.Discussions;

public class DetailsModel : PageModel
{
    private readonly DiscussionThreadService _threads;
    private readonly PostService _posts;
    private readonly ILogger<DetailsModel> _logger;

    public DetailsModel(
        DiscussionThreadService threads,
        PostService posts,
        ILogger<DetailsModel> logger)
    {
        _threads = threads;
        _posts = posts;
        _logger = logger;
    }

    public DiscussionThread? Thread { get; private set; }

    public List<PostTreeBuilder.Node> PostTree { get; private set; } = new();

    [BindProperty]
    public ReplyInputModel Input { get; set; } = new();

    public class ReplyInputModel
    {
        [Required]
        [StringLength(10000, MinimumLength = 1)]
        public string Content { get; set; } = string.Empty;

        // Null means a top-level post.
        public int? ParentPostId { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(int threadId)
    {
        var loaded = await LoadThreadAsync(threadId);

        if (!loaded)
        {
            return NotFound();
        }

        return Page();
    }

    [Authorize]
    public async Task<IActionResult> OnPostReplyAsync(int threadId)
    {
        var loaded = await LoadThreadAsync(threadId);

        if (!loaded)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        // Get all active posts for this thread, then validate the parent
        // against that collection.
        var existingPosts = await _posts.GetForThreadAsync(threadId);

        if (Input.ParentPostId.HasValue &&
            !existingPosts.Any(post =>
                post.Id == Input.ParentPostId.Value))
        {
            ModelState.AddModelError(
                nameof(Input.ParentPostId),
                "The selected parent post does not belong to this discussion.");

            return Page();
        }

        var post = new Post
        {
            Title = string.Empty,
            Content = Input.Content,
            DiscussionThreadId = threadId,
            ApplicationUserId = userId,
            CreatedAt = DateTimeOffset.UtcNow,
            ParentPostId = Input.ParentPostId
        };

        try
        {
            await _posts.AddAsync(post);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(
                ex,
                "Failed to save a reply in thread {ThreadId}.",
                threadId);

            ModelState.AddModelError(
                string.Empty,
                "Your reply could not be saved.");

            return Page();
        }

        return RedirectToPage(
            "/Discussions/Details",
            new { threadId });
    }

    private async Task<bool> LoadThreadAsync(int threadId)
    {
        Thread = await _threads.GetAsync(threadId);

        if (Thread == null)
        {
            return false;
        }

        var posts = await _posts.GetForThreadAsync(threadId);

        // Use the instructor-provided PostTreeBuilder to construct
        // the parent-child hierarchy.
        PostTree = PostTreeBuilder.Build(posts);

        return true;
    }
}
