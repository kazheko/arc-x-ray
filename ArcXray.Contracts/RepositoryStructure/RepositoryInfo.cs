using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArcXray.Contracts.RepositoryStructure
{
    public record RepositoryInfo(string Path, IEnumerable<SolutionInfo> Solutions);
}
