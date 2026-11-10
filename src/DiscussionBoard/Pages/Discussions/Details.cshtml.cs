using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using DiscussionBoard.Models;
using DiscussionBoard.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using DiscussionBoard.Authorization;

namespace DiscussionBoard.Pages.Discussions;

[Authorize]
public class DetailsModel : PageModel
{
    private readonly DiscussionThreadService _threads;
    private readonly PostService _posts;
    private readonly ILogger<DetailsModel> _logger;
    private readonly IApplicationAuthorizationService _authorization;
    public bool CanEditThread { get; set; }

    public DetailsModel(
        DiscussionThreadService threads,
        PostService posts,
        ILogger<DetailsModel> logger, IApplicationAuthorizationService authorization)
    {
        _threads = threads;
        _posts = posts;
        _logger = logger;
        _authorization = authorization;
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

        public int? ParentPostId { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(int threadId)
    {
        if (!await LoadThreadAsync(threadId))
            return NotFound();

        return Page();
    }

    public async Task<IActionResult> OnPostReplyAsync(int threadId)
    {
        if (User.Identity?.IsAuthenticated != true)
            return Challenge();

        if (!await LoadThreadAsync(threadId))
            return NotFound();

        if (!ModelState.IsValid)
            return Page();

        var existingPosts = await _posts.GetForThreadAsync(threadId);

        if (Input.ParentPostId.HasValue &&
            !existingPosts.Any(p => p.Id == Input.ParentPostId.Value))
        {
            ModelState.AddModelError(
                nameof(Input.ParentPostId),
                "The selected parent post does not belong to this discussion.");

            return Page();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
            return Challenge();

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
            _logger.LogError(ex,
                "Failed to save a reply in thread {ThreadId}.", threadId);

            ModelState.AddModelError(
                string.Empty,
                "Your reply could not be saved.");

            return Page();
        }

        return RedirectToPage("/Discussions/Details", new { threadId });
    }

    private async Task<bool> LoadThreadAsync(int threadId)
    {
        Thread = await _threads.GetAsync(threadId);

        if (Thread == null)
            return false;

        var posts = await _posts.GetForThreadAsync(threadId);

        PostTree = PostTreeBuilder.Build(posts);
        CanEditThread = await _authorization.CanEditAsync(User, Thread);

        return true;
    }


}
