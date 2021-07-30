using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SkillChat.Server
{
    public interface IDotNetPackageList
    {
        IEnumerable<SemanticVersioning.Version> PackageList { get; }
    }
}
