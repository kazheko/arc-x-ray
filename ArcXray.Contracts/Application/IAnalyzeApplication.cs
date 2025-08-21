using ArcXray.Contracts.RepositoryStructure;

namespace ArcXray.Contracts.Application
{
    public interface IAnalyzeApplication
    {
        Task<DetectionResult> AnalyzeProjectAsync(ProjectContext projectContext, DetectionConfiguration config);
    }
}
