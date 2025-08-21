using ArcXray.Contracts;
using ArcXray.Contracts.Application;
using System.IO;
using System.Text.Json;

namespace ArcXray.Cli
{
    public class DetectionConfigProvider : IProvideDetectionConfig
    {
        private readonly IFileRepository _fileRepository;

        public DetectionConfigProvider(IFileRepository fileRepository)
        {
            _fileRepository = fileRepository;
        }

        public async Task<IEnumerable<DetectionConfiguration>> GetConfigAsync(string framework, string sdk, string directory)
        {
            var netVersion = framework.Replace("net", string.Empty);

            if(double.TryParse(netVersion, out double version) && version >=6)
            {
                framework = "net6.0+";
            }

            var dir = Path.Combine(directory, framework, sdk.Split('.').Last());

            if (!Directory.Exists(dir))
            {
                return Enumerable.Empty<DetectionConfiguration>();
            }

            var files = _fileRepository.FindFiles(dir, "*.json");

            var result = new List<DetectionConfiguration>();

            foreach (var path in files)
            {
                var content = await _fileRepository.ReadFileAsync(path);
                var config = JsonSerializer.Deserialize<DetectionConfiguration>(content);
                if (config != null)
                {
                    result.Add(config);
                }
            }

            return result;
        }
    }
}
