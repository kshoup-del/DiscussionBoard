using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using DiscussionBoard.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DiscussionBoard.Pages.Discussions;

[Authorize]
public class EditModel : PageModel
{
    private readonly DiscussionThreadService _threads;
    private readonly ILogger<EditModel> _logger;

    public EditModel(
        DiscussionThreadService threads,
        ILogger<EditModel> logger)
    {
        _threads = threads;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required]
        [StringLength(200, MinimumLength = 3)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(1000, MinimumLength = 1)]
        public string Content { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var thread = await _threads.GetAsync(id);

        if (thread == null || thread.IsDeleted)
            return NotFound();

        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (thread.ApplicationUserId != userId)
            return Forbid();

        Input = new InputModel
        {
            Title = thread.Title,
            Content = thread.Content
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var thread = await _threads.GetAsync(id);

        if (thread == null || thread.IsDeleted)
            return NotFound();

        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (thread.ApplicationUserId != userId)
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
            _logger.LogError(ex,
                "Failed to update discussion thread {ThreadId}.", id);

            ModelState.AddModelError(string.Empty,
                "The discussion thread could not be updated.");

            return Page();
        }

        return RedirectToPage("/Forums/Details",
            new { id = thread.ForumId });
    }
}
