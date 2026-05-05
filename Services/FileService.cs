using MessagingApp.Services.Interfaces;

namespace MessagingApp.Services
{
    public class FileService : IFileService
    {
        private readonly IWebHostEnvironment _env;
        private const string UploadFolder = "uploads/profile-pictures";

        public FileService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public async Task<string?> SaveProfilePictureAsync(IFormFile file)
        {
            if (file == null || file.Length == 0) return null;

            var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext)) return null;

            var folder = Path.Combine(_env.WebRootPath, UploadFolder);
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var fileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(folder, fileName);

            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            return fileName;
        }

        public void DeleteProfilePicture(string? fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return;
            var path = Path.Combine(_env.WebRootPath, UploadFolder, fileName);
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}