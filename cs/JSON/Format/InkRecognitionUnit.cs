using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Windows.Foundation;

namespace Contoso.NoteTaker.JSON.Format
{
    abstract public class InkRecognitionUnit
    {
        [JsonProperty(PropertyName = "id")]
        public UInt64 Id { get; set; }

        [JsonProperty(PropertyName = "category")]
        public RecognitionUnitKind Kind { get; set; }

        [JsonProperty(PropertyName = "childIds")]
        public List<UInt64> ChildIds { get; set; }

        [JsonProperty(PropertyName = "class")]
        public RecognitionUnitType Type { get; set; }

        [JsonProperty(PropertyName = "parentId")]
        public UInt64 ParentId { get; set; }

        [JsonProperty(PropertyName = "boundingRectangle")]
        public Rectangle BoundingRect { get; set; }

        [JsonProperty(PropertyName = "rotatedBoundingRectangle")]
        public List<PointDetailsPattern> RotatedBoundingRect { get; set; }

        [JsonProperty(PropertyName = "strokeIds")]
        public List<UInt64> StrokeIds { get; set; }

        public bool Contains(InkRecognitionUnit other)
        {
            if (this.BoundingRect == null || other.BoundingRect == null)
            {
                return false;
            }

            var rect = new Rect(this.BoundingRect.TopX, this.BoundingRect.TopY, this.BoundingRect.Width, this.BoundingRect.Height);
            var p1 = new Point(other.BoundingRect.TopX, other.BoundingRect.TopY);
            var p2 = new Point(other.BoundingRect.TopX + other.BoundingRect.Width, other.BoundingRect.TopY);
            var p3 = new Point(other.BoundingRect.TopX + other.BoundingRect.Width, other.BoundingRect.TopY + other.BoundingRect.Height);
            var p4 = new Point(other.BoundingRect.TopX, other.BoundingRect.TopY + other.BoundingRect.Height);

            return rect.Contains(p1) && rect.Contains(p2) && rect.Contains(p3) && rect.Contains(p4);
        }

        public bool IsLowerLinked(InkRecognitionUnit other)
        {
            if (Math.Abs(this.BoundingRect.TopY + this.BoundingRect.Height - other.BoundingRect.TopY) < 12.0)
            {
                return true;
            }
            return false;
        }

        public bool IsRightLinked(InkRecognitionUnit other)
        {
            if (Math.Abs(this.BoundingRect.TopX + this.BoundingRect.Width - other.BoundingRect.TopX) < 12.0)
            {
                return true;
            }
            return false;
        }
    }

    public enum RecognitionUnitType
    {
        [EnumMember(Value = "leaf")]
        Leaf,
        [EnumMember(Value = "container")]
        Container
    }
}
