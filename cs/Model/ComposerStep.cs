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
        public string Type { get; set; }

        [JsonProperty(PropertyName = "property")]
        public string Property = "";

        [JsonIgnore]
        public DrawsomeShape RelatedShape { get; set; }

        public ComposerStep()
        {

        }

        public ComposerStep(DrawsomeObj shape)
        {
            this.RelatedShape = shape as DrawsomeShape;
        }
    }

    public class SendActivity : ComposerStep
    {
        [JsonProperty(PropertyName = "activity")]
        public string Activity { get; set; }

        public SendActivity(string text, DrawsomeShape shape): base(shape)
        {
            this.Activity = text;
            this.Type = "Microsoft.SendActivity";
        }
    }

    public class IfCondition: ComposerStep
    {
        [JsonProperty(PropertyName = "condition")]
        public string Condition { get; set; }

        [JsonProperty(PropertyName = "steps")]
        public List<ComposerStep> Steps { get; set; } = new List<ComposerStep>();

        [JsonProperty(PropertyName = "elseSteps")]
        public List<ComposerStep> ElseSteps { get; set; } = new List<ComposerStep>();

        public IfCondition(string condition, DrawsomeShape shape) : base(shape)
        {
            this.Condition = condition;
            this.Type = "Microsoft.IfCondition";
        }
    }

    public class TextInput : ComposerStep
    {
        [JsonProperty(PropertyName = "prompt")]
        public string Prompt { get; set; }

        public TextInput(string content, DrawsomeShape shape) : base(shape)
        {
            this.Prompt = content;
            this.Type = "Microsoft.TextInput";
        }
        public TextInput(string content, string property, DrawsomeShape shape) : base(shape)
        {
            this.Prompt = content;
            this.Type = "Microsoft.TextInput";
            this.Property = property;
        }
    }


    public class SetProperty: ComposerStep
    {
        [JsonProperty(PropertyName = "value")]
        public string Value { get; set; }

        public SetProperty(string expression, DrawsomeShape shape) : base(shape)
        {
            var splitted = expression.Split(new char[] {'='});
            this.Property = splitted.FirstOrDefault();
            this.Value = splitted.LastOrDefault();
            this.Type = "Microsoft.SetProperty";
        }
    }

    public class HttpRequest: ComposerStep
    {
        [JsonProperty("method")]
        public string Method { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("body")]
        public string Body { get; set; }

        public HttpRequest(string content, DrawsomeShape shape) : base(shape)
        {
            this.Type = "Microsoft.HttpRequest";
            if (content.ToLower().Contains("weather"))
            {
                this.Url = "https://api.openweathermap.org/data/2.5/weather?q=Suzhou&appid=f6abd7e76544272a97a0f1e9c2188219";
                this.Method = "GET";
            }
        }

        public HttpRequest(string url, string method, string body, DrawsomeShape shape) : base(shape)
        {
            this.Type = "Microsoft.HttpRequest";
            this.Url = url;
            this.Method = method;
            this.Body = body;
        }
    }

   
}
