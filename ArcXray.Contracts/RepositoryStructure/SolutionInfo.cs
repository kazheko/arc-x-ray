namespace ArcXray.Contracts.RepositoryStructure
{
    /// <summary>
    /// Represents a .NET solution with a list of its projects.
    /// </summary>
    public class SolutionInfo
    {
        private readonly IDictionary<string, ProjectInfo> _allProjects;
        private readonly IEnumerable<string> _rootProjectPaths;

        public SolutionInfo(string name, string path, IEnumerable<ProjectInfo> projects)
        {
            Name = name;
            Path = path;
            _allProjects = projects.ToDictionary(x => x.ProjectPath);
            _rootProjectPaths = FindRootProjects(projects);
        }

        public string Name { get; private set; }
        public string Path { get; private set; }

        public IEnumerable<ProjectInfo> AllProjects => _allProjects.Values;

        public IEnumerable<ProjectInfo> RootProjects => _rootProjectPaths.Select(x => _allProjects[x]);

        private static IEnumerable<string> FindRootProjects(IEnumerable<ProjectInfo> allProjects)
        {
            var rootProjects = allProjects.Select(x => x.ProjectPath);

            foreach (var project in allProjects)
            {
                var deps = project.ProjectReferences
                    .Select(x => x.ProjectPath)
                    .Select(System.IO.Path.GetDirectoryName);

                rootProjects = rootProjects.Except(deps);
            }

            return rootProjects;
        }
    }
}
