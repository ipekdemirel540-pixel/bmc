using System.Collections.Generic;
using System.Windows.Media;

namespace ManufacturingToolsSuite.Pages
{
    public sealed class ProfileResultRow
    {
        public int ProfileIndex { get; init; }
        public string Dimensions { get; init; } = string.Empty;
        public string Components { get; init; } = string.Empty;
        public string CutLengths { get; init; } = string.Empty;
        public double UsedMm { get; init; }
        public double WasteMm { get; init; }
        public double UtilizationPercent { get; init; }
        public int Quantity { get; set; } = 1;
        public List<NestingSegment> Segments { get; set; } = new List<NestingSegment>();
    }

    public sealed class NestingSegment
    {
        public double Length { get; set; }
        public string Label { get; set; } = string.Empty;
        public Brush ColorBrush { get; set; } = Brushes.Blue;
        public Brush ForegroundBrush { get; set; } = Brushes.White;
        public double Width { get; set; }
        public bool IsWaste { get; set; }
        public bool IsClamp { get; set; }
        public bool IsKerf { get; set; }
        public string TooltipText { get; set; } = string.Empty;
    }
}
