namespace Avalonia.FlexPanel
{
    internal struct FlexLineInfo
    {
        public double ItemsU { get; set; }

        public double OffsetV { get; set; }

        public double LineU { get; set; }

        public double LineV { get; set; }

        public double LineFreeV { get; set; }

        public int ItemStartIndex { get; set; }

        public int ItemEndIndex { get; set; }

        public double ScaleU { get; set; }
    }
}