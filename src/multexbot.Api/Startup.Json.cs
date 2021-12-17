using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace multexBot.Api
{
    public partial class Startup
    {
        public void ConfigureJson()
        {
            
            JsonConvert.DefaultSettings = () =>
            {
                var settings = new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                };

                settings.Converters.Add(new StringEnumConverter());

                return settings;
            };

        }
    }
}