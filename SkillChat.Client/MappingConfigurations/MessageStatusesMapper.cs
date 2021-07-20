using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper.Configuration;
using SkillChat.Client.ViewModel.Models;
using SkillChat.Interface;
using SkillChat.Server.ServiceModel.Molds;
using SkillChat.Server.ServiceModel.Molds.Attachment;

namespace SkillChat.Client.MappingConfigurations
{
    public static class MessageStatusesMapper
    {
        public static void ConfigureModelMapping(MapperConfigurationExpression cfg)
        {
            cfg.CreateMap<ReceiveMessageStatus, MessageStatusModel>();
            cfg.CreateMap<MessageStatusModel, ReceiveMessageStatus>();
            cfg.CreateMap<HubMessageStatus, MessageStatusModel>();
            cfg.CreateMap<MessageStatusModel, HubMessageStatus>();
            cfg.CreateMap<MessageStatusMold, MessageStatusModel>()
                .ForMember(m => m.ChatId, o => o.Ignore());
        }
    }
}
