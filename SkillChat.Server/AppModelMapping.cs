using AutoMapper;
using AutoMapper.Configuration;
using SkillChat.Server.Domain;
using SkillChat.Server.ServiceModel.Molds;

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
            //cfg.CreateMap<User, UserProfileMold>()
            //    .ForMember(mold => mold.IsPasswordSetted, e => e.MapFrom(model => model.Password != null))
            //    ;
        }
    }
}