namespace ArcXray.Contracts.Application
{
    public interface IProvideCheckList
    {
        Task<IEnumerable<CheckList>> GetConfigAsync(string framework, string sdk, string directory);
    }
}
