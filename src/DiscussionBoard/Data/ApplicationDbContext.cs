using DiscussionBoard.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DiscussionBoard.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
    {
        public DbSet<DiscussionThread> DiscussionThreads { get; set; } = null!;

        public DbSet<Forum> Forums { get; set; } = null!;

        public DbSet<Post> Posts { get; set; } = null!;


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<DiscussionThread>()
                .HasOne(thread => thread.Forum)
                .WithMany(forum => forum.DiscussionThreads)
                .HasForeignKey(thread => thread.ForumId);

            builder.Entity<Post>()
                .HasOne(post => post.ParentPost)
                .WithMany(post => post.Replies)
                .HasForeignKey(post => post.ParentPostId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<DiscussionThread>()
                .HasOne(thread => thread.ApplicationUser)
                .WithMany(user => user.DiscussionThreads)
                .HasForeignKey(thread => thread.ApplicationUserId);

            builder.Entity<Post>()
                .HasOne(post => post.ApplicationUser)
                .WithMany(user => user.Posts)
                .HasForeignKey(post => post.ApplicationUserId);


        }
    }
}
