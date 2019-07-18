using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
namespace NoteTaker.Model
{
    public class ComposerBot
    {
        [JsonProperty(PropertyName = "$type")]
        public string type = "Microsoft.AdaptiveDialog";

        [JsonProperty(PropertyName = "autoEndDialog")]
        public bool AutoEndDialog = true;

        [JsonProperty(PropertyName = "steps")]
        public List<ComposerStep> Steps;

        [JsonProperty(PropertyName = "$schema")]
        public string Schema = "../../app.schema";

        public ComposerBot(List<ComposerStep> steps)
        {
            this.Steps = steps;
        }
    }
}
