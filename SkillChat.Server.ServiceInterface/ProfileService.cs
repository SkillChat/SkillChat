using System.Threading.Tasks;
using AutoMapper;
using Raven.Client.Documents.Session;
using ServiceStack;
using SkillChat.Server.Domain;
using SkillChat.Server.ServiceModel;
using SkillChat.Server.ServiceModel.Molds;

namespace SkillChat.Server.ServiceInterface
{
    public class ProfileService : Service
    {
        public IAsyncDocumentSession RavenSession { get; set; }
        public IMapper Mapper { get; set; }

        [Authenticate]
        public async Task<UserProfileMold> Get(GetProfile request)
        {
            var user = await RavenSession.LoadAsync<User>(request.UserId);
            var mapped = Mapper.Map<UserProfileMold>(user);
            return mapped;
        }

        [Authenticate]
        public async Task<UserProfileMold> Post(SetProfile request)
        {
            var session = Request.ThrowIfUnauthorized();
            var displayName = request.DisplayName;

            var user = await RavenSession.LoadAsync<User>(session.UserAuthId);
            user.DisplayName = displayName;

            await RavenSession.StoreAsync(user);
            await RavenSession.SaveChangesAsync();

            var mapped = Mapper.Map<UserProfileMold>(user);
            return mapped;
        }
    }
}