﻿using AutoMapper;
using AutoMapper.Configuration;
using SkillChat.Server.Domain;
using SkillChat.Server.ServiceModel;
using SkillChat.Server.ServiceModel.Molds;
using SkillChat.Server.ServiceModel.Molds.Chats;

namespace SkillChat.Server
{
    public class AppModelMapping
    {
        // TODO : IoC IMapper
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
            cfg.CreateMap<Chat, ChatMold>();
            cfg.CreateMap<ChatMember, ChatMemberMold>();

            cfg.CreateMap<Message, MessageMold>()
                .ForMember(m => m.UserLogin, e => e.MapFrom(m => m.UserId));

            cfg.CreateMap<User, UserProfileMold>();
            cfg.CreateMap<User, MyUserProfileMold>()
                .IncludeBase<User, UserProfileMold>()
                .ForMember(m => m.IsPasswordSetted, e => e.Ignore());

            cfg.CreateMap<Settings, UserChatSettings>();
            cfg.CreateMap<SetSettings, Settings>()
                .ForMember(m => m.Id, e => e.Ignore());
        }
    }
}
