using DiscussionBoard.Models;
using DiscussionBoard.Services;
using DiscussionBoard.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiscussionBoard.Pages;

[Authorize]
public class MyProfileModel : PageModel
{
    private readonly IUserProfileService _profileService;
    private readonly UserManager<ApplicationUser> _userManager;

    public MyProfileModel(UserManager<ApplicationUser> userManager, IUserProfileService profileService)
    {
        _userManager = userManager;
        _profileService = profileService;
    }

    [BindProperty] public UserProfile Input { get; set; } = new();

    public class AvatarOption
    {
        public string Id { get; set; } = string.Empty;
        public string ImagePath { get; set; } = string.Empty;
        public string AltText { get; set; } = string.Empty;
    }

    public List<AvatarOption> AvatarOptions { get; } =
    [
        new() { Id = "default", ImagePath = "/images/avatars/default.svg", AltText = "Default avatar" },
        new() { Id = "astronaut", ImagePath = "/images/avatars/astronaut.svg", AltText = "Astronaut Avatar" },
        new() { Id = "fox", ImagePath = "/images/avatars/fox.svg", AltText = "Fox Avatar" },
        new() { Id = "owl", ImagePath = "/images/avatars/owl.svg", AltText = "Owl Avatar" },
        new() { Id = "robot", ImagePath = "/images/avatars/robot.svg", AltText = "Robot Avatar" }

    ];

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null)
        {
            return Challenge();
        }

        Input = new UserProfile
        {
            DisplayName = user.DisplayName,
            AvatarId = user.AvatarId
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null)
            return Challenge();

        Input.DisplayName = Input.DisplayName?.Trim() ?? string.Empty;

        if (Input.DisplayName.Length < 2 ||
            Input.DisplayName.Length > 60)
        {
            ModelState.AddModelError(
                "Input.DisplayName",
                "Display name must be between 2 and 60 characters.");
        }

        if (!AvatarCatalog.IsValid(Input.AvatarId))
        {
            ModelState.AddModelError(
                "Input.AvatarId",
                "Please select a valid avatar.");
        }

        if (Input.DisplayName.Length >= 2 &&
            Input.DisplayName.Length <= 60)
        {
            var available = await _profileService
                .IsDisplayNameAvailableAsync(Input.DisplayName, user.Id);

            if (!available)
            {
                ModelState.AddModelError(
                    "Input.DisplayName",
                    "That display name is already in use.");
            }
        }

        if (!ModelState.IsValid)
            return Page();

        user.DisplayName = Input.DisplayName;
        user.AvatarId = Input.AvatarId;

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return Page();
        }

        TempData["StatusMessage"] = "Your profile was updated successfully.";

        return RedirectToPage();
    }
}
