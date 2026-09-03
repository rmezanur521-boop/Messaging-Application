using Amazon.S3;
using Amazon.S3.Model;
using MessagingApp.Services.Interfaces;

namespace MessagingApp.Services
{
    public class FileService : IFileService
    {
        private readonly IAmazonS3 _s3;
        private readonly string _bucket;
        private readonly string _publicBaseUrl;

        public FileService(IAmazonS3 s3, IConfiguration config)
        {
            _s3 = s3;
            _bucket = config["ObjectStorage:BucketName"]!;
            _publicBaseUrl = config["ObjectStorage:PublicBaseUrl"]!;
        }

        public async Task<string?> SaveProfilePictureAsync(IFormFile file)
        {
            if (file == null || file.Length == 0) return null;

            var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext)) return null;

            var key = $"profile-pictures/{Guid.NewGuid()}{ext}";

            using var stream = file.OpenReadStream();
            await _s3.PutObjectAsync(new PutObjectRequest
            {
                BucketName = _bucket,
                Key = key,
                InputStream = stream,
                ContentType = file.ContentType,
                CannedACL = S3CannedACL.PublicRead
            });

            return key;
        }

        public async void DeleteProfilePicture(string? key)
        {
            if (string.IsNullOrEmpty(key)) return;
            await _s3.DeleteObjectAsync(_bucket, key);
        }

        public string? GetProfilePictureUrl(string? key) =>
            string.IsNullOrEmpty(key) ? null : $"{_publicBaseUrl}/{key}";
    }
}