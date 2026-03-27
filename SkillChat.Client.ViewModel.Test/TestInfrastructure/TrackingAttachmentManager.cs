#nullable enable
using System.Diagnostics;
using SkillChat.Client.ViewModel;
using SkillChat.Client.ViewModel.Services;
using SkillChat.Server.ServiceModel.Molds.Attachment;

namespace SkillChat.Client.ViewModel.Test.TestInfrastructure;

internal sealed class TrackingAttachmentManager : AttachmentManager
{
    public TrackingAttachmentManager(string attachmentPath, ISkillChatApiClient serviceClient)
        : base(attachmentPath, serviceClient)
    {
    }

    public Func<string, bool>? FileExistsOverride { get; set; }
    public Func<Task<bool>>? DownloadOverride { get; set; }
    public ProcessStartInfo? LastStartInfo { get; private set; }

    public override bool IsExistAttachment(AttachmentMold data)
    {
        return FileExistsOverride?.Invoke(data.FileName) ?? base.IsExistAttachment(data);
    }

    public override Task<bool> DownloadAttachment(AttachmentMold data)
    {
        return DownloadOverride?.Invoke() ?? base.DownloadAttachment(data);
    }

    protected override void StartProcess(ProcessStartInfo startInfo)
    {
        LastStartInfo = startInfo;
    }
}
