namespace ArcXray.Contracts.Application
{
    public class DetectionResult
    {
        public string ProjectName { get; set; }
        public string ProjectPath { get; set; }
        public string ProjectType { get; set; }
        public double Confidence
        {
            get
            {
                return MaxPossibleScore > 0
                    ? TotalScore / MaxPossibleScore
                    : 0;
            }
        }
        public string Interpretation { get; set; }
        public string ConfidenceLevel { get; set; }
        public List<CheckResult> Checks { get; set; } = new List<CheckResult>();
        public double TotalScore
        {
            get
            {
                return Checks.Where(c => c.Passed).Sum(c => c.Weight);
            }
        }
        public double MaxPossibleScore
        {
            get
            {
                return Checks.Sum(c => c.Weight);
            }
        }
        public DateTime AnalysisTimestamp { get; set; }
        public TimeSpan AnalysisDuration { get; set; }

        // Grouped view for reporting
        public Dictionary<string, List<CheckResult>> ChecksByCategory =>
            Checks.GroupBy(c => c.Category).ToDictionary(g => g.Key, g => g.ToList());
    }
}
