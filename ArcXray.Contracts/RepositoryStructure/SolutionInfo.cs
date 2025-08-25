namespace ArcXray.Contracts.RepositoryStructure
{
    /// <summary>
    /// Represents a .NET solution with a list of its projects.
    /// </summary>
    public class SolutionInfo
    {
        public SolutionInfo(string name, string path, IEnumerable<ProjectInfo> projects)
        {
            Name = name;
            Path = path;
            Projects = projects.ToDictionary(x => x.ProjectPath);
            RootProjectPaths = FindRootProjects(projects);
        }

        public string Name { get; private set; }
        public string Path { get; private set; }

        public IDictionary<string, ProjectInfo> Projects { get; private set; }

        public IEnumerable<string> RootProjectPaths { get; private set; }

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
