using AutoMapper;
using AutoMapper.Configuration;
using SkillChat.Client.ViewModel;
using SkillChat.Interface;
using SkillChat.Server.ServiceModel.Molds;
using SkillChat.Server.ServiceModel.Molds.Attachment;

namespace SkillChat.Client
{
    public class AppModelMapping
    {
        public static Mapper ConfigureMapping()
        {
            var cfg = new MapperConfigurationExpression();
            ConfigureModelMapping(cfg);
            var mapperConfiguration = new MapperConfiguration(cfg);
            mapperConfiguration.AssertConfigurationIsValid();
            mapperConfiguration.CompileMappings();
            var mapper = new Mapper(mapperConfiguration);
            return mapper;
        }

        private static void ConfigureModelMapping(MapperConfigurationExpression cfg)
        {
            cfg.CreateMap<AttachmentHubMold, AttachmentMold>();
            cfg.CreateMap<AttachmentMold, AttachmentHubMold>();
            cfg.CreateMap<MessageMold, MessageViewModel>()
                .ForMember(m => m.ShowNickname, e => e.Ignore())
                .ForMember(m => m.Selected, e => e.Ignore())
                .ForMember(m => m.IsMyMessage, e => e.Ignore())
                .ForMember(m => m.Attachments, e => e.Ignore())
                .ForMember(m => m.ProfileMold, e => e.Ignore())
                .ForMember(m => m.Time, e => e.Ignore())
                .ForMember(m => m.UserProfileInfoCommand, e => e.Ignore())
                .ForMember(m => m.IsChecked, e => e.Ignore())
                .ForMember(m => m.selectMsgMode, e => e.Ignore())
                .ForMember(m => m.MenuItems, e => e.Ignore());
                .ForMember(m => m.IsQuotedMessage, e => e.Ignore());
            cfg.CreateMap<ReceiveMessage, MessageViewModel>()
                .ForMember(m => m.ShowNickname, e => e.Ignore())
                .ForMember(m => m.Selected, e => e.Ignore())
                .ForMember(m => m.IsMyMessage, e => e.Ignore())
                .ForMember(m => m.Attachments, e => e.Ignore())
                .ForMember(m => m.ProfileMold, e => e.Ignore())
                .ForMember(m => m.Time, e => e.Ignore())
                .ForMember(m => m.UserProfileInfoCommand, e => e.Ignore())
                .ForMember(m => m.MenuItems, e => e.Ignore())
                .ForMember(m => m.IsQuotedMessage, e => e.Ignore())
                .ForMember(m => m.QuotedMessage, e => e.Ignore())
                .ForMember(m => m.LastEditTime, e => e.Ignore())
                .ForMember(m => m.UserNickname, e => e.MapFrom(c => c.UserNickname ?? c.UserLogin));
            cfg.CreateMap<ReceiveEditedMessage, MessageViewModel>()
                .ForMember(m => m.ShowNickname, e => e.Ignore())
                .ForMember(m => m.Selected, e => e.Ignore())
                .ForMember(m => m.IsMyMessage, e => e.Ignore())
                .ForMember(m => m.Attachments, e => e.Ignore())
                .ForMember(m => m.ProfileMold, e => e.Ignore())
                .ForMember(m => m.Time, e => e.Ignore())
                .ForMember(m => m.UserProfileInfoCommand, e => e.Ignore())
                .ForMember(m => m.MenuItems, e => e.Ignore())
                .ForMember(m => m.IsQuotedMessage, e => e.Ignore())
                .ForMember(m => m.QuotedMessage, e => e.Ignore())
                .ForMember(m => m.UserNickname, e => e.MapFrom(c => c.UserNickname ?? c.UserLogin));
        }
    }
}