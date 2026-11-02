using DiscussionBoard.Services;
using Microsoft.AspNetCore.Identity;

namespace DiscussionBoard.Models
{
    public class ApplicationUser : IdentityUser
    {
        public ICollection<DiscussionThread> DiscussionThreads { get; set; }
            = new List<DiscussionThread>();

        public ICollection<Post> Posts { get; set; }
            = new List<Post>();

        public string DisplayName { get; set; }= string.Empty;
        public string AvatarId { get; set; } = AvatarCatalog.DefaultId;


    }
}
