using ArcXray.Core.RepositoryStructure;
using ArcXray.Core.RepositoryStructure.Models;

namespace ArcXray.Cli
{
    /// <summary>
    /// Prints the analysis result to the console.
    /// </summary>
    public class ConsoleReportStrategy : IReportStrategy
    {
        public void GenerateReport(RepositoryInfo result)
        {
            Console.WriteLine("📊 Repository Analysis Result");
            Console.WriteLine("=============================");
            Console.WriteLine();

            foreach (var solution in result.Solutions)
            {
                Console.WriteLine($"🟦 Solution: {solution.Name}");
                Console.WriteLine($"    Path: {solution.Path}");
                Console.WriteLine($"    Projects: {solution.Projects.Count}");
                Console.WriteLine();

                foreach (var project in solution.Projects)
                {
                    Console.WriteLine($"    📁 Project: {project.Name}");
                    Console.WriteLine($"        Path: {project.Path}");

                    if (project.ProjectReferences.Any())
                    {
                        Console.WriteLine("        🔗 Project References:");
                        foreach (var reference in project.ProjectReferences)
                            Console.WriteLine($"            - {reference}");
                    }
                    else
                    {
                        Console.WriteLine("        🔗 Project References: (none)");
                    }

                    if (project.NugetPackages.Any())
                    {
                        Console.WriteLine("        📦 NuGet Packages:");
                        foreach (var pkg in project.NugetPackages)
                            Console.WriteLine($"            - {pkg.Name} ({pkg.Version})");
                    }
                    else
                    {
                        Console.WriteLine("        📦 NuGet Packages: (none)");
                    }

                    Console.WriteLine();
                }

                Console.WriteLine("--------------------------------------------------");
                Console.WriteLine();
            }
        }
    }
}
