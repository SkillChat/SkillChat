using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using Raven.Client.Documents;
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
            var uid = session?.UserAuthId;

            var existedUser = await RavenSession.Query<User>().FirstOrDefaultAsync(x => x.Id == uid);
            if (existedUser != null)
            {
                var user = Mapper.Map<SetProfile, User>(request, existedUser);

                await RavenSession.StoreAsync(user);
                await RavenSession.SaveChangesAsync();

                var mapped = Mapper.Map<UserProfileMold>(user);
                return mapped;
            }
            throw new HttpError(HttpStatusCode.NotFound, "Пользователь не найден в базе данных");
        }

        [Authenticate]
        public async Task<UserProfileMold> Get(GetProfileInfoUser request)
        {
            var existedUser = await RavenSession.Query<User>().FirstOrDefaultAsync(x => x.Id == request.UserId);
            if (existedUser != null)
            {
                var user = await RavenSession.LoadAsync<User>(existedUser.Id);
                var mapped = Mapper.Map<UserProfileMold>(user);
                return mapped;
            }
            throw new HttpError(HttpStatusCode.NotFound, "Пользователь не найден в базе данных");
        }
    }
}