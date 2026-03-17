using SemanticVersioning;
using SkillChat.Server.Test.TestInfrastructure;

namespace SkillChat.Server.Test;

public class DotNetVersionHelperTests
{
    [Test]
    [Arguments(new[] { "5.0.5" }, "5.0.6", "5.0.5")]
    [Arguments(new[] { "5.0.7" }, "5.0.6", "5.0.7")]
    [Arguments(new[] { "5.0.7-beta.3" }, "5.0.6", "5.0.7-beta.3")]
    [Arguments(new[] { "5.0.7" }, "5.0.6-beta.2", "5.0.7")]
    [Arguments(new[] { "5.0.66" }, "5.0.7", "5.0.66")]
    [Arguments(new[] { "5.0.99" }, "5.0.7", "5.0.99")]
    [Arguments(new[] { "5.0.0" }, "5.0.5", "5.0.0")]
    [Arguments(new[] { "5.4.0" }, "5.5.5", "")]
    [Arguments(new[] { "6.0.1" }, "5.5.5", "")]
    public async Task GetNearestVersion_ReturnsExpectedVersion(
        string[] installedVersions,
        string targetVersion,
        string expectedVersion)
    {
        var versions = new DotNetVersionHelper(new FakeDotNetVersion(installedVersions));

        var version = versions.GetNearestDotNetVersion(targetVersion);

        await Assert.That(version).IsEqualTo(expectedVersion);
    }

    private sealed class FakeDotNetVersion : IHaveVersions
    {
        public FakeDotNetVersion(IEnumerable<string> versions)
        {
            Versions = versions.Select(static version => new SemanticVersioning.Version(version)).ToList();
        }

        public IEnumerable<SemanticVersioning.Version> Versions { get; }
    }
}
