using System.Collections.Generic;

namespace SkillChat.Server
{
    public interface IHaveVersions
    {
        IEnumerable<SemanticVersioning.Version> Versions { get; }
    }
}
