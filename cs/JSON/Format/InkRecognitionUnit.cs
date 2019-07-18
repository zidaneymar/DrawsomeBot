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

        private float Tolerance = 10;

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

            var res = 0;
            var pList = new List<Point>()
            {
                p1,p2,p3,p4
            };
            pList.ForEach(item =>
            {
                if (rect.Contains(item))
                {
                    res += 1;
                }
            });
            return res >= 3;
        }

        // todo: fix the acurracy
        public bool IsLowerLinked(InkRecognitionUnit other)
        {
            if (Math.Abs(this.BoundingRect.TopY + this.BoundingRect.Height - other.BoundingRect.TopY) < Tolerance)
            {
                if (Math.Abs(this.BoundingRect.TopX + this.BoundingRect.Width / 2 - (other.BoundingRect.TopX + other.BoundingRect.Width / 2)) < Tolerance)
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsRightMidAligned(InkRecognitionUnit other)
        {
            if (Math.Abs(this.BoundingRect.TopX + this.BoundingRect.Width - other.BoundingRect.TopX) < Tolerance)
            {
                if (Math.Abs(this.BoundingRect.TopY + this.BoundingRect.Height / 2 - other.BoundingRect.TopY) < Tolerance)
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsRightLowerLinked(InkRecognitionUnit other)
        {
            if (Math.Abs(this.BoundingRect.TopY + this.BoundingRect.Height - other.BoundingRect.TopY) < Tolerance)
            {
                if (Math.Abs(this.BoundingRect.TopX + this.BoundingRect.Width - (other.BoundingRect.TopX + other.BoundingRect.Width / 2)) < Tolerance)
                {
                    return true;
                }
            }

            return false;
        }

        public float DistanceToLower(InkRecognitionUnit other)
        {
            var x1 = this.BoundingRect.TopX + this.BoundingRect.Width / 2;
            var y1 = this.BoundingRect.TopY + this.BoundingRect.Height;
            var x2 = other.BoundingRect.TopX + other.BoundingRect.Width / 2;
            var y2 = other.BoundingRect.TopY;

            return Math.Abs((x1 - x2) * (y1 - y2));
        }

        public float DistanceToRight(InkRecognitionUnit other)
        {
            var x1 = this.BoundingRect.TopX + this.BoundingRect.Width;
            var y1 = this.BoundingRect.TopY + this.BoundingRect.Height / 2;
            var x2 = other.BoundingRect.TopX;
            var y2 = other.BoundingRect.TopY;

            return Math.Abs((x1 - x2) * (y1 - y2));
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
