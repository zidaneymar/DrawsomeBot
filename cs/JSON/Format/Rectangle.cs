using Newtonsoft.Json;
using System.Windows;
using Windows.UI.Xaml;

namespace Contoso.NoteTaker.JSON.Format
{
    public class Rectangle
    {
        [JsonProperty(PropertyName = "topX")]
        public float TopX { get; set; }

        [JsonProperty(PropertyName = "topY")]
        public float TopY { get; set; }

        [JsonProperty(PropertyName = "width")]
        public float Width { get; set; }

        [JsonProperty(PropertyName = "height")]
        public float Height { get; set; }

        private bool ValueInRange(float value, float min, float max)
        { return (value >= min) && (value <= max); }

        public bool IntersectWith(Rectangle other)
        {
            var res = RectHelper.Intersect(new Windows.Foundation.Rect(this.TopX, this.TopY, this.Width, this.Height), new Windows.Foundation.Rect(other.TopX, other.TopY, other.Width, other.Height));
            return !res.IsEmpty;
        }

        public float OverlapSize(Rectangle other)
        {
            var res = RectHelper.Intersect(new Windows.Foundation.Rect(this.TopX, this.TopY, this.Width, this.Height), new Windows.Foundation.Rect(other.TopX, other.TopY, other.Width, other.Height));
            return res.IsEmpty ? 0 : (float)(res.Width * res.Height);
        }
    }
}
