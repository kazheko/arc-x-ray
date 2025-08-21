using ArcXray.Contracts;
using ArcXray.Contracts.Application;
using System.IO;
using System.Text.Json;

namespace ArcXray.Analyzers.Applications
{
    // todo: re-work
    public class DetectionConfigProvider : IProvideDetectionConfig
    {
        private readonly IFileRepository _fileRepository;

        public DetectionConfigProvider(IFileRepository fileRepository)
        {
            _fileRepository = fileRepository;
        }

        public async Task<IEnumerable<DetectionConfiguration>> GetConfigAsync(string sdk, string directory)
        {
            var files = _fileRepository.FindFiles(directory, "*.json");

            var result = new List<DetectionConfiguration>();

            foreach (var path in files)
            {
                var content = await _fileRepository.ReadFileAsync(path);
                var config = JsonSerializer.Deserialize<DetectionConfiguration>(content);
                result.Add(config);
            }

            return result;
        }
    }
}
