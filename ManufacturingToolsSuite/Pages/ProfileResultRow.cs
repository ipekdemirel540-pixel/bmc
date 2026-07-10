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
    }
}
