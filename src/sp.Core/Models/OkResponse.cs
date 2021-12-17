namespace sp.Core.Models
{
    public class OkResponse
    {
        public OkResponse()
        {
        }

        public OkResponse(object data)
        {
            Data = data;
        }

        public bool Success = true;

        public object Data { get; set; }
    }
}