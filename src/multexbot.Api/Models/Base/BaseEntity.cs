
namespace multexBot.Api.Models.Base
{
    public class BaseEntity
    {
        //Auto increment
        public long Id { get; set; }

        public long CreatedTime { get; set; }

        public long? UpdatedTime { get; set; }
    }
}