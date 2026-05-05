namespace MessagingApp.Services.Interfaces
{
    public interface IFileService
    {
        Task<string?> SaveProfilePictureAsync(IFormFile file);
        void DeleteProfilePicture(string? fileName);
    }
}