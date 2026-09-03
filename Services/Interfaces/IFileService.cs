namespace MessagingApp.Services.Interfaces
{
    public interface IFileService
    {
        Task<string?> SaveProfilePictureAsync(IFormFile file);
        Task DeleteProfilePictureAsync(string? fileName);
        string? GetProfilePictureUrl(string? fileName);
    }
}