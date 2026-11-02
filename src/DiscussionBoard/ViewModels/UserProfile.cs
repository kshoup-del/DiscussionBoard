using Microsoft.Build.Framework;

namespace DiscussionBoard.ViewModels
{
    public class UserProfile
    {
        [Required]
        public string DisplayName { get; set; } = string.Empty;

        [Required]
        public string AvatarId { get; set; } = string.Empty;
    }
}
