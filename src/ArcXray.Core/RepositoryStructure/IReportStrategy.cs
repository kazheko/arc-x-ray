using ArcXray.Core.RepositoryStructure.Models;

namespace ArcXray.Core.RepositoryStructure
{
    /// <summary>
    /// Defines a strategy for generating reports from analysis results.
    /// </summary>
    public interface IReportStrategy
    {
        void GenerateReport(RepositoryInfo result);
    }
}
