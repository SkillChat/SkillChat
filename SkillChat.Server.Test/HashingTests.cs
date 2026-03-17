using SkillChat.Server.ServiceInterface;

namespace SkillChat.Server.Test;

public class HashingTests
{
    [Test]
    public async Task CreateSalt_ReturnsRandom128BitValue()
    {
        var first = Hashing.CreateSalt();
        var second = Hashing.CreateSalt();

        await Assert.That(first.Length).IsEqualTo(16);
        await Assert.That(second.Length).IsEqualTo(16);
        await Assert.That(first.SequenceEqual(second)).IsFalse();
    }

    [Test]
    public async Task CreateHashPassword_ReturnsStableHashForSamePasswordAndSalt()
    {
        var salt = Hashing.CreateSalt();

        var first = Hashing.CreateHashPassword("Password123!", salt);
        var second = Hashing.CreateHashPassword("Password123!", salt);

        await Assert.That(first).IsEqualTo(second);
        await Assert.That(first).IsNotEqualTo("Password123!");
    }
}
