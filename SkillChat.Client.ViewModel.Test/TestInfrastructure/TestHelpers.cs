#nullable enable
using System.Reflection;
using System.Runtime.Serialization;
using AutoMapper;
using AutoMapper.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SkillChat.Client.ViewModel;
using SkillChat.Client.ViewModel.Services;
using SkillChat.Interface;
using SkillChat.Server.ServiceModel.Molds;
using SkillChat.Server.ServiceModel.Molds.Attachment;
using Splat;

namespace SkillChat.Client.ViewModel.Test.TestInfrastructure;

internal static class TestHelpers
{
    public static void ResetLocator()
    {
        Locator.SetLocator(new ModernDependencyResolver());
    }

    public static void ResetNotificationManager()
    {
        typeof(Notification)
            .GetField("_manager", BindingFlags.Static | BindingFlags.NonPublic)!
            .SetValue(null, null);
    }

    public static IMapper CreateClientMapper()
    {
        var configuration = new MapperConfigurationExpression();
        configuration.CreateMap<AttachmentHubMold, AttachmentMold>();
        configuration.CreateMap<AttachmentMold, AttachmentHubMold>();
        configuration.CreateMap<MessageMold, MessageViewModel>()
            .ForMember(m => m.ShowNickname, e => e.Ignore())
            .ForMember(m => m.Selected, e => e.Ignore())
            .ForMember(m => m.IsMyMessage, e => e.Ignore())
            .ForMember(m => m.Attachments, e => e.Ignore())
            .ForMember(m => m.ProfileMold, e => e.Ignore())
            .ForMember(m => m.Time, e => e.Ignore())
            .ForMember(m => m.UserProfileInfoCommand, e => e.Ignore())
            .ForMember(m => m.IsChecked, e => e.Ignore())
            .ForMember(m => m.IsUnreadBoundary, e => e.Ignore())
            .ForMember(m => m.SelectMsgMode, e => e.Ignore())
            .ForMember(m => m.MenuItems, e => e.Ignore())
            .ForMember(m => m.IsQuotedMessage, e => e.Ignore());

        var mapperConfiguration = new MapperConfiguration(configuration, NullLoggerFactory.Instance);
        mapperConfiguration.AssertConfigurationIsValid();
        mapperConfiguration.CompileMappings();
        return new Mapper(mapperConfiguration);
    }

    public static MainWindowViewModel CreateUninitializedMainWindow()
    {
#pragma warning disable SYSLIB0050
        var mainWindow = (MainWindowViewModel)FormatterServices.GetUninitializedObject(typeof(MainWindowViewModel));
#pragma warning restore SYSLIB0050
        mainWindow.SelectMessagesMode = new SelectMessages();
        mainWindow.SettingsViewModel = new SettingsViewModel(Substitute.For<ISkillChatApiClient>());
        mainWindow.ProfileViewModel = new ProfileViewModel(Substitute.For<ISkillChatApiClient>());
        mainWindow.User = new CurrentUserViewModel();
        mainWindow.RegisterUser = new RegisterUserViewModel();
        mainWindow.ConfirmationViewModel = new ConfirmationViewModel();
        mainWindow.AttachmentViewModel = new SendAttachmentsViewModel(Substitute.For<ISkillChatApiClient>());
        return mainWindow;
    }

    public static async Task EventuallyAsync(Func<bool> predicate, int timeoutMs = 5000)
    {
        var stopAt = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < stopAt)
        {
            if (predicate())
            {
                return;
            }

            await Task.Delay(50);
        }

        throw new TimeoutException("The expected condition was not met in time.");
    }

    public static AttachmentMold CreateAttachmentMold(string? id = null, string? fileName = null, long size = 10)
    {
        return new AttachmentMold
        {
            Id = id ?? $"attachment/{Guid.NewGuid():N}",
            FileName = fileName ?? $"file-{Guid.NewGuid():N}.txt",
            Hash = "hash",
            SenderId = "User/test",
            Size = size,
            UploadDateTime = DateTimeOffset.UtcNow,
        };
    }
}
