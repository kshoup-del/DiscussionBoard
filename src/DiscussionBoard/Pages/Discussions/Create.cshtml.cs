using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using DiscussionBoard.Models;
using DiscussionBoard.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DiscussionBoard.Pages.Discussions;

[Authorize]
public class CreateModel : PageModel
{
    private readonly DiscussionThreadService _threads;
    private readonly ForumService _forums;
    private readonly ILogger<CreateModel> _logger;

    public CreateModel(
        DiscussionThreadService threads,
        ForumService forums,
        ILogger<CreateModel> logger)
    {
        _threads = threads;
        _forums = forums;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public int ForumId { get; set; }

    public class InputModel
    {
        [Required]
        [StringLength(200, MinimumLength = 3)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(1000, MinimumLength = 1)]
        public string Content { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var forum = await _forums.GetByIdAsync(ForumId);

        if (forum == null || forum.IsDeleted)
            return NotFound();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var forum = await _forums.GetByIdAsync(ForumId);

        if (forum == null || forum.IsDeleted)
            return NotFound();

        if (!ModelState.IsValid)
            return Page();

        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
            return Challenge();

        var thread = new DiscussionThread
        {
            Title = Input.Title,
            Content = Input.Content,
            ForumId = forum.Id,
            ApplicationUserId = userId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        try
        {
            await _threads.AddAsync(thread);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex,
                "Failed to create a discussion thread.");

            ModelState.AddModelError(string.Empty,
                "The discussion thread could not be created.");

            return Page();
        }

        return RedirectToPage("/Forums/Details",
            new { id = forum.Id });
    }
}
