using DiscussionBoard.Models;
using DiscussionBoard.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiscussionBoard.Pages.Forums;

[Authorize]
public class DetailsModel : PageModel
{
    private readonly ForumService _forums;
    private readonly DiscussionThreadService _threads;

    public DetailsModel(
        ForumService forums,
        DiscussionThreadService threads)
    {
        _forums = forums;
        _threads = threads;
    }

    public Forum? Forum { get; set; }

    public List<DiscussionThread> Threads { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Forum = await _forums.GetByIdAsync(id);

        if (Forum == null)
        {
            return NotFound();
        }

        Threads = await _threads.GetForForumAsync(id);

        return Page();
    }
}
