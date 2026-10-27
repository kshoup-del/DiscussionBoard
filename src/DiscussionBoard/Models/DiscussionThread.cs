using System.ComponentModel.DataAnnotations;

namespace DiscussionBoard.Models
{
    public class DiscussionThread : DiscussionBase
    {
        public int ForumId { get; set; }

        [Required]
        public Forum Forum { get; set; } = null!;

        public ICollection<Post> Posts { get; set; }
            = new List<Post>();
    }
}
