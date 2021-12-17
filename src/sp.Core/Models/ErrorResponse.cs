using sp.Core.Constants;

namespace sp.Core.Models
{
    public class ErrorResponse
    {
        public bool Success = false;
        public Error Error { get; set; }
      
    }

    public class Error
    {
        public AppError Code { get; set; }

        public string Msg { get; set; }
    }
}