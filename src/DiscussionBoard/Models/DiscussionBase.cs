namespace DiscussionBoard.Models
{
    public class DiscussionBase : EntityBase
    {
        public string Title;
        public string Content;
        public DateTimeOffset CreatedAt;
        public string ApplicationUserId;
        public ApplicationUser ApplicationUser;

    }
}
