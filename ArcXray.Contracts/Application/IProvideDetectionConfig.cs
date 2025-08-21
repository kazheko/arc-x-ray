namespace ArcXray.Contracts.Application
{
    public interface IProvideDetectionConfig
    {
        Task<IEnumerable<DetectionConfiguration>> GetConfigAsync(string sdk, string directory);
    }
}
