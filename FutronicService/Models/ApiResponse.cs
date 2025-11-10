namespace FutronicService.Models
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
    public string Message { get; set; }
   public T Data { get; set; }
   public string Error { get; set; }

        public static ApiResponse<T> SuccessResponse(string message, T data)
        {
            return new ApiResponse<T>
    {
   Success = true,
          Message = message,
    Data = data,
    Error = null
        };
        }

        public static ApiResponse<T> ErrorResponse(string message, string error = null)
     {
 return new ApiResponse<T>
     {
    Success = false,
      Message = message,
 Data = default(T),
        Error = error
        };
        }

        public static ApiResponse<T> ErrorResponse(string message, T data, string error = null)
     {
         return new ApiResponse<T>
         {
         Success = false,
     Message = message,
       Data = data,
        Error = error
       };
        }
    }
}
