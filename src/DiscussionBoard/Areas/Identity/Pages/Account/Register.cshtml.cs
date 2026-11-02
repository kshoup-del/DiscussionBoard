// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using DiscussionBoard.Models;
using DiscussionBoard.Services;
using DiscussionBoard.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace DiscussionBoard.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserStore<ApplicationUser> _userStore;
        private readonly IUserEmailStore<ApplicationUser> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;
        private readonly IUserProfileService _profileService;

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            IUserStore<ApplicationUser> userStore,
            SignInManager<ApplicationUser> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender,
            IUserProfileService profileService)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
            _profileService = profileService;
        }

        [BindProperty] public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public IReadOnlyList<AvatarCatalog.AvatarOption> AvatarOptions =>
            AvatarCatalog.Options;

        public class InputModel
        {
            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.",
                MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }

            public UserProfile UserProfile { get; set; } = new();
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            Input = new InputModel
            {
                UserProfile = new UserProfile
                {
                    AvatarId = AvatarCatalog.DefaultId
                }
            };

            ExternalLogins =
                (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins =
                (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            Input ??= new InputModel();
            Input.UserProfile ??= new UserProfile();

            // Trim the submitted display name before validating or saving it.
            Input.UserProfile.DisplayName =
                Input.UserProfile.DisplayName?.Trim() ?? string.Empty;

            var displayName = Input.UserProfile.DisplayName;
            var avatarId = Input.UserProfile.AvatarId;

            if (displayName.Length < 2 || displayName.Length > 60)
            {
                ModelState.AddModelError(
                    "Input.UserProfile.DisplayName",
                    "Display name must be between 2 and 60 characters.");
            }
            else
            {
                var available =
                    await _profileService.IsDisplayNameAvailableAsync(displayName);

                if (!available)
                {
                    ModelState.AddModelError(
                        "Input.UserProfile.DisplayName",
                        "That display name is already in use.");
                }
            }

            if (!AvatarCatalog.IsValid(avatarId))
            {
                ModelState.AddModelError(
                    "Input.UserProfile.AvatarId",
                    "Please select a valid avatar.");
            }

            if (ModelState.IsValid)
            {
                var user = CreateUser();

                user.DisplayName = displayName;
                user.AvatarId = avatarId;

                await _userStore.SetUserNameAsync(
                    user, Input.Email, CancellationToken.None);

                await _emailStore.SetEmailAsync(
                    user, Input.Email, CancellationToken.None);

                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account with password.");

                    var userId = await _userManager.GetUserIdAsync(user);
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

                    var callbackUrl = Url.Page(
                        "/Account/ConfirmEmail",
                        pageHandler: null,
                        values: new
                        {
                            area = "Identity",
                            userId = userId,
                            code = code,
                            returnUrl = returnUrl
                        },
                        protocol: Request.Scheme);

                    await _emailSender.SendEmailAsync(
                        Input.Email,
                        "Confirm your email",
                        $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

                    if (_userManager.Options.SignIn.RequireConfirmedAccount)
                    {
                        return RedirectToPage(
                            "RegisterConfirmation",
                            new { email = Input.Email, returnUrl = returnUrl });
                    }

                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return LocalRedirect(returnUrl);
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // Keep the entered email and profile values when redisplaying the form.
            return Page();
        }

        private ApplicationUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<ApplicationUser>();
            }
            catch
            {
                throw new InvalidOperationException(
                    $"Can't create an instance of '{nameof(ApplicationUser)}'. " +
                    $"Ensure that '{nameof(ApplicationUser)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                    "override the register page in /Areas/Identity/Pages/Account/Register.cshtml");
            }
        }

        private IUserEmailStore<ApplicationUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException(
                    "The default UI requires a user store with email support.");
            }

            return (IUserEmailStore<ApplicationUser>)_userStore;
        }
    }
}
