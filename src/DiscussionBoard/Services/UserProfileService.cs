using DiscussionBoard.Data;
using Microsoft.EntityFrameworkCore;

namespace DiscussionBoard.Services
{
    public class UserProfileService : IUserProfileService
    {
        public const int MinDisplayNameLength = 2;
        public const int MaxDisplayNameLength = 60;

        private readonly ApplicationDbContext _db;

        public UserProfileService(ApplicationDbContext db) => _db = db;

        public async Task<bool> IsDisplayNameAvailableAsync(string displayName, string? excludeUserId = null)
        {
            var name = displayName?.Trim();

            if (name is null || name.Length < MinDisplayNameLength || name.Length > MaxDisplayNameLength)
                return false;

            var lowered = name.ToLower();

            var query = _db.Users.AsNoTracking()
                .Where(u => u.DisplayName.Trim().ToLower() == lowered);

            if (excludeUserId is not null)
                query = query.Where(u => u.Id != excludeUserId);

            return !await query.AnyAsync();
        }


    }
}
