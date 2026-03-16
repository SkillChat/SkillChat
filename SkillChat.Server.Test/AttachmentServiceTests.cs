#nullable enable
using System.Text;
using ServiceStack;
using SkillChat.Server.Domain;
using SkillChat.Server.ServiceModel;
using SkillChat.Server.Test.TestInfrastructure;

namespace SkillChat.Server.Test;

public class AttachmentServiceTests
{
    private static ServerTestHost Host => TestEnvironment.Host;

    [Test]
    public async Task SetAttachment_UploadsFileAndStoresMetadata()
    {
        var user = await Host.CreateUserAsync();
        var client = Host.CreateClient(Host.CreateAccessToken(user));
        var tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.txt");
        await File.WriteAllTextAsync(tempFile, "attachment content");

        try
        {
            using var fileStream = File.OpenRead(tempFile);
            var result = client.PostFileWithRequest<SkillChat.Server.ServiceModel.Molds.Attachment.AttachmentMold>(
                fileStream,
                Path.GetFileName(tempFile),
                new SetAttachment());

            var storedAttachment = await Host.LoadAsync<Attachment>(result.Id);
            var storedFile = Path.Combine(Host.FilesPath, result.Id["attachment/".Length..]);

            using var _ = Assert.Multiple();
            await Assert.That(result.SenderId).IsEqualTo(user.Id);
            await Assert.That(storedAttachment).IsNotNull();
            await Assert.That(File.Exists(storedFile)).IsTrue();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Test]
    public async Task GetAttachment_ReturnsStoredFileStream()
    {
        var user = await Host.CreateUserAsync();
        var content = Encoding.UTF8.GetBytes("download me");
        var attachment = await Host.CreateAttachmentAsync(user.Id, fileName: "payload.txt", content: content);
        var client = Host.CreateClient(Host.CreateAccessToken(user));

        using var stream = await client.GetAsync(new GetAttachment
        {
            Id = attachment.Id["attachment/".Length..],
        });
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);

        await Assert.That(memoryStream.ToArray().SequenceEqual(content)).IsTrue();
    }
}
