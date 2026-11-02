namespace DiscussionBoard.Services;

public interface IUserProfileService
{
    /// <summary>
    /// True only when the trimmed name is 2-60 characters and no other user already has it
    /// (case-insensitive). Pass excludeUserId so a user can keep their own name.
    /// </summary>
    Task<bool> IsDisplayNameAvailableAsync(string displayName, string? excludeUserId = null);
}
