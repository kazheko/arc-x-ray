namespace ArcXray.Contracts.Application
{
    public class DetectionResult
    {
        private readonly List<CheckResult> _checkResults = new List<CheckResult>();

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
        public string Interpretation { get; private set; } = string.Empty;
        public IEnumerable<CheckResult> Checks => _checkResults;
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

        public void AddCheckResult(CheckResult checkResult)
        {
            _checkResults.Add(checkResult);
        }

        public void UpdateInterpretation(string interpretation)
        {
            Interpretation = interpretation;
        }
    }
}
