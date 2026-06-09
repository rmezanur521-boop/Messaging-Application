// Models/DTOs/Common/ApiResponse.cs
namespace MessagingApp.Models.DTOs.Common
{
    // Flutter side-এ সব response একই structure-এ পাবে
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }

        public static ApiResponse<T> Ok(T data, string message = "Success")
            => new() { Success = true, Message = message, Data = data };

        public static ApiResponse<T> Fail(string message)
            => new() { Success = false, Message = message };
    }
}