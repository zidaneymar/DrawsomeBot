using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace NoteTaker.Model
{
    public class ComposerStep
    {
        [JsonProperty(PropertyName = "$type")]
        public string Type;

        [JsonProperty(PropertyName = "property")]
        public string Property = "";
    }

    public class SendActivity : ComposerStep
    {
        [JsonProperty(PropertyName = "activity")]
        public string Activity;

        public SendActivity(string text)
        {
            this.Activity = text;
            this.Type = "Microsoft.SendActivity";
        }
    }

    public class IfCondition: ComposerStep
    {
        [JsonProperty(PropertyName = "condition")]
        public string Condition;

        [JsonProperty(PropertyName = "steps")]
        public List<ComposerStep> Steps;

        [JsonProperty(PropertyName = "elseSteps")]
        public List<ComposerStep> ElseSteps;

        public IfCondition(string condition)
        {
            this.Condition = condition;
            this.Type = "Microsoft.IfCondition";
        }
    }

    public class SetProperty: ComposerStep
    {
        [JsonProperty(PropertyName = "value")]
        public string Value;

        public SetProperty(string expression)
        {
            var splitted = expression.Split(new char[] {'='});
            this.Property = splitted.FirstOrDefault();
            this.Value = splitted.LastOrDefault();
            this.Type = "Microsoft.SetProperty";
        }
    }

    public class HttpRequest: ComposerStep
    {
        public HttpRequest()
        {
            this.Type = "Microsoft.HttpRequest";
        }
    }
}
