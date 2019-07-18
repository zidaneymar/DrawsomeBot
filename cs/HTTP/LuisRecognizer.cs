using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime;
using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace NoteTaker.HTTP
{
    public class LuisRecognizer
    {
        public LuisRecognizer(string key, string appId)
        {
            this.appId = appId;
            this.key = key;
        }

        private readonly string appId;

        private readonly string key;

        public async Task<LuisResult> GetPrediction(string query)
        {
            var client = new HttpClient();
            var endpointUri = string.Format("https://westus.api.cognitive.microsoft.com/luis/v2.0/apps/{0}?verbose=true&timezoneOffset=-360&subscription-key={1}&q={2}", appId, key, query);
            var response = await client.GetAsync(endpointUri);

            var strResponseContent = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<LuisResult>(strResponseContent);
        }

    }
}
