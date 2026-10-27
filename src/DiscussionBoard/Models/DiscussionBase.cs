using System.ComponentModel.DataAnnotations;

namespace DiscussionBoard.Models
{
    public class DiscussionBase : EntityBase
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(1000,MinimumLength=1)]
        public string Content { get; set; } = string.Empty;

        public DateTimeOffset CreatedAt{ get; set; } = DateTimeOffset.Now;

        [Required]
        public string ApplicationUserId{ get; set; }= string.Empty;

        [Required]
        public ApplicationUser ApplicationUser{ get; set; }= null!;

    }
}
