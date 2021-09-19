using System.Collections.Generic;
using ServiceStack.DataAnnotations;

namespace SkillChat.Server.ServiceModel.Molds.Status
{
	[Description("Страница список статусов пользователя")]
	public class UserMessageStatusPage
	{
		[Description("Cписок статусов пользователя для всех чатов")]
		public List<UserMessageStatusMold> Statuses { get; set; }
	}
}