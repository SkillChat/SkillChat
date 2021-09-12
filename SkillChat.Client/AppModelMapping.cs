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
                .ForMember(m => m.QuotedMessageViewModel, e => e.Ignore())
                .ForMember(m => m.IdQuotedMessage, e => e.Ignore())
                .ForMember(m => m.Time, e => e.Ignore())
                .ForMember(m => m.UserProfileInfoCommand, e => e.Ignore())
                .ForMember(m => m.MenuItems, e => e.Ignore());
        }
    }
}
