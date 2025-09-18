using System.Text.Json;
using Newtonsoft.Json.Linq;

namespace UKHO.ADDS.EFS.FunctionalTests.Services
{
    public static class JsonAssertionHelper
    {
        public static JToken ConvertToJToken(JsonDocument document)
        {
            string jsonString = JsonSerializer.Serialize(document.RootElement);  //JsonConvert.SerializeObject(document.RootElement);
            return JToken.Parse(jsonString);
        }
    }
}
