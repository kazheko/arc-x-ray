namespace ArcXray.Contracts.Application
{
    public class CheckResult
    {
        public string CheckId { get; set; }
        public bool Passed { get; set; }
        public double Weight { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public string Details { get; set; }
    }
}
