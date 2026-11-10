
using DiscussionBoard.Authorization;
using DiscussionBoard.Models;
using DiscussionBoard.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DiscussionBoard.Pages.Discussions;

[Authorize]
public class ThreadEditModel : PageModel
{
    private readonly DiscussionThreadService _threads;
    private readonly IApplicationAuthorizationService _authorization;
    private readonly ILogger<ThreadEditModel> _logger;

    public ThreadEditModel(
        DiscussionThreadService threads,
        IApplicationAuthorizationService authorization,
        ILogger<ThreadEditModel> logger)
    {
        _threads = threads;
        _authorization = authorization;
        _logger = logger;
    }

    [BindProperty]
    public DiscussionEditViewModel Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var thread = await _threads.GetAsync(id);

        if (thread == null)
            return NotFound();

        if (!await _authorization.CanEditAsync(User, thread))
            return Forbid();

        Input = new DiscussionEditViewModel
        {
            Title = thread.Title,
            Content = thread.Content
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var thread = await _threads.GetAsync(id);

        if (thread == null)
            return NotFound();

        if (!await _authorization.CanEditAsync(User, thread))
            return Forbid();

        if (!ModelState.IsValid)
            return Page();

        thread.Title = Input.Title;
        thread.Content = Input.Content;

        try
        {
            await _threads.UpdateAsync(thread);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(
                ex,
                "Failed to update discussion thread {ThreadId}.",
                id);

            ModelState.AddModelError(
                string.Empty,
                "The discussion thread could not be updated.");

            return Page();
        }

        return RedirectToPage(
            "/Discussions/Details",
            new { threadId = id });
    }
}
