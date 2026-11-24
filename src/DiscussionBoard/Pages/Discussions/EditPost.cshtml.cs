using DiscussionBoard.Authorization;
using DiscussionBoard.Models;
using DiscussionBoard.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiscussionBoard.Pages.Discussions;

[Authorize]
public class EditPostModel : PageModel
{
    private readonly PostService _posts;
    private readonly IApplicationAuthorizationService _authorization;
    private readonly ILogger<EditPostModel> _logger;

    public EditPostModel(
        PostService posts,
        IApplicationAuthorizationService authorization
        , ILogger<EditPostModel> logger)
    {
        _posts = posts;
        _authorization = authorization;
        _logger = logger;
    }

    [BindProperty] public DiscussionEditViewModel Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int threadId, int postId)
    {
        var post = await _posts.GetAsync(postId);

        if (post == null)
            return NotFound();

        if (post.DiscussionThreadId != threadId || post.Id != postId)
            return NotFound();

        if (!await _authorization.CanEditAsync(User, post))
        {
            _logger.LogWarning("User {UserId} cannot edit post {PostId} in thread {ThreadId}.",
                User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, post.Id, threadId);
            return Forbid();
        }


        Input = new DiscussionEditViewModel
        {
            Title = post.Title,
            Content = post.Content
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int threadId, int postId)
    {
        var post = await _posts.GetAsync(postId);

        if (post == null)
            return NotFound();

        if (post.DiscussionThreadId != threadId || post.Id != postId)
            return NotFound();

        if (!await _authorization.CanEditAsync(User, post))
        {
            _logger.LogWarning("User {UserId} cannot edit post {PostId} in thread {ThreadId}.",
                User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, post.Id, threadId);
            return Forbid();
        }

        if (!ModelState.IsValid)
            return Page();

        post.Title = Input.Title;
        post.Content = Input.Content;

        await _posts.UpdateAsync(post);

        return RedirectToPage(
            "/Discussions/Details",
            new { threadId });
    }
}
