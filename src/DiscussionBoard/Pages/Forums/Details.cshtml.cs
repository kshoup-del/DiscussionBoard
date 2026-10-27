using DiscussionBoard.Models;
using DiscussionBoard.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiscussionBoard.Pages.Forums;

public class DetailsModel : PageModel
{
    private readonly ForumService _forums;
    private readonly DiscussionThreadService _threads;
    private readonly ILogger<DetailsModel> _logger;

    public DetailsModel(
        ForumService forums,
        DiscussionThreadService threads,
        ILogger<DetailsModel> logger)
    {
        _forums = forums;
        _threads = threads;
        _logger = logger;
    }

    public Forum? Forum { get; private set; }

    public List<DiscussionThread> Threads { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(int id)
    {
        try
        {
            var forums = await _forums.GetAllAsync();
            Forum = forums.FirstOrDefault(f => f.Id == id);

            if (Forum is null)
            {
                return NotFound();
            }

            Threads = await _threads.GetForForumAsync(id);

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error loading forum details for forum {ForumId}",
                id);

            return StatusCode(500);
        }
    }
}
