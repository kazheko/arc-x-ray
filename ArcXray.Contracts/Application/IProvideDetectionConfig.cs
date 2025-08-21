namespace ArcXray.Contracts.Application
{
    public interface IProvideDetectionConfig
    {
        Task<IEnumerable<DetectionConfiguration>> GetConfigAsync(string framework, string sdk, string directory);
    }
}
