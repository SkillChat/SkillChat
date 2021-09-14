using AutoMapper;
using AutoMapper.Configuration;
using SkillChat.Client.ViewModel.Models;
using SkillChat.Interface;
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
            cfg.CreateMap<UserMessageStatusModel, HubUserMessageStatus>();
        }
    }
}
