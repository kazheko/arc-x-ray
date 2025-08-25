using ArcXray.Contracts;
using ArcXray.Contracts.Application;
using System.IO;
using System.Text.Json;

namespace ArcXray.Cli
{
    public class CheckListProvider : IProvideCheckList
    {
        private readonly IFileRepository _fileRepository;

        public CheckListProvider(IFileRepository fileRepository)
        {
            _fileRepository = fileRepository;
        }

        public async Task<IEnumerable<CheckList>> GetConfigAsync(string framework, string sdk, string directory)
        {
            if (!sdk.EndsWith("Web"))
            {
                return Enumerable.Empty<CheckList>();
            }

            var dir = Path.Combine(directory, sdk.Split('.').Last());

            if (!Directory.Exists(dir))
            {
                return Enumerable.Empty<CheckList>();
            }

            var files = _fileRepository.FindFiles(dir, "*.json");

            var result = new List<CheckList>();

            foreach (var path in files)
            {
                var content = await _fileRepository.ReadFileAsync(path);
                var config = JsonSerializer.Deserialize<CheckList>(content);
                if (config != null)
                {
                    result.Add(config);
                }
            }

            return result;
        }
    }
}
