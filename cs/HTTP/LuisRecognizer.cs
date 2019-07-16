using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime;
using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoteTaker.HTTP
{
    public class LuisRecognizer
    {
        public LuisRecognizer(string key, string appId)
        {
            this.appId = appId;
            this.creds = new ApiKeyServiceClientCredentials(key);
            this.luClient = new LUISRuntimeClient(creds, new System.Net.Http.DelegatingHandler[] { });
            this.luClient.Endpoint = endPoint;
        }

        private readonly ApiKeyServiceClientCredentials creds;

        private LUISRuntimeClient luClient;

        private readonly string appId;

        private readonly string endPoint = "https://westus.api.cognitive.microsoft.com";

        public async Task<LuisResult> GetPrediction(string query)
        {
            var prediction = new Prediction(luClient);
            return await prediction.ResolveAsync(appId, query);
        }

    }
}
