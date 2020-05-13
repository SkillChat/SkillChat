using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ServiceStack;
using ServiceStack.Auth;
using SkillChat.Server.Domain;
using SkillChat.Server.ServiceModel;
using SkillChat.Server.ServiceModel.Molds;

namespace SkillChat.Server.ServiceInterface
{
    public class AuthService : Service
    {
        public const string RefreshPermission = "refresh";
        public static string anonimPrefix = "user:";

        //public IAnonimUserRepository AnonimUserRepository { get; set; }
        //public IUserRepository UserRepository { get; set; }
        public ILoggerFactory LoggerFactory { get; set; }

        public async Task<TokenResult> Post(AuthViaPassword request)
        {
            var email = request.Login.ToUpperInvariant();//ВАЖНО!!! Email должен быть в ВЕРХНЕМ регистре.

            var user = new User();//(await UserRepository.GetAsync(s => s.Email == email)).FirstOrDefault();
            if (user == null)
                throw new HttpError(HttpStatusCode.NotFound, "User is not found");

            if (string.IsNullOrWhiteSpace(user.Password))
                throw new HttpError(HttpStatusCode.NotFound, "Password is not set");

            if (user.Password != request.Password)
                throw new HttpError(HttpStatusCode.NotFound, "Password is wrong");

            var customAccessExpire = (request.AccessTokenExpirationPeriod.HasValue && request.AccessTokenExpirationPeriod >= 0)
                ? TimeSpan.FromSeconds(request.AccessTokenExpirationPeriod.Value) : (TimeSpan?)null;
            var customRefreshExpire = (request.RefreshTokenExpirationPeriod.HasValue && request.RefreshTokenExpirationPeriod >= 0)
                ? TimeSpan.FromSeconds(request.RefreshTokenExpirationPeriod.Value) : (TimeSpan?)null;

            var tokenResult = await GenerateToken(user, customAccessExpire, customRefreshExpire);
            if (tokenResult == null)
                throw new HttpError(HttpStatusCode.Conflict, "Roles is not setted");

            return tokenResult;
        }

        [Authenticate]
        public async Task<TokenResult> Post(PostRefreshToken request)
        {
            var session = Request.ThrowIfUnauthorized();
            var uid = session.UserAuthId;

            var isAnonimUser = uid.StartsWith(anonimPrefix);

            User user = null;//isAnonimUser
                //? (dynamic) (await AnonimUserRepository.GetAsync(anonim => anonim.Uid == uid)).FirstOrDefault()
                //: (dynamic) (await UserRepository.GetAsync(s => s.Id == uid)).FirstOrDefault();

            if (user == null)
                throw new HttpError(HttpStatusCode.NotFound, $"User {uid} is not found");

            var customAccessExpire =
                (request.AccessTokenExpirationPeriod.HasValue && request.AccessTokenExpirationPeriod >= 0)
                    ? TimeSpan.FromSeconds(request.AccessTokenExpirationPeriod.Value)
                    : (TimeSpan?)null;

            var customRefreshExpire =
                (request.RefreshTokenExpirationPeriod.HasValue && request.RefreshTokenExpirationPeriod >= 0)
                    ? TimeSpan.FromSeconds(request.RefreshTokenExpirationPeriod.Value)
                    : (TimeSpan?)null;

            var tokenResult = isAnonimUser
                ? await GenerateAnonimToken(uid)
                : await GenerateToken(user, customAccessExpire, customRefreshExpire);

            if (tokenResult == null)
                throw new HttpError(HttpStatusCode.Conflict, "Roles is not setted");

            return tokenResult;
        }

        internal async Task<TokenResult> GenerateAnonimToken(string uid)
        {
            var sessionId = Guid.NewGuid().ToString();
            var token = new TokenResult();
            var jwtProvider = (JwtAuthProvider)AuthenticateService.GetAuthProvider("jwt");

            if (jwtProvider?.PublicKey == null)
                throw new HttpError(HttpStatusCode.ServiceUnavailable, "AuthProvider does not work");
            
            var body = JwtAuthProvider.CreateJwtPayload(new AuthUserSession
            {
                UserAuthId = uid,
                CreatedAt = DateTime.UtcNow,
            },
                issuer: jwtProvider.Issuer, expireIn: TimeSpan.FromDays(30));

            body["useragent"] = Request.UserAgent;
            body["session"] = sessionId;

            token.AccessToken = JwtAuthProvider.CreateEncryptedJweToken(body, jwtProvider.PublicKey.Value);

            var refreshBody = JwtAuthProvider.CreateJwtPayload(new AuthUserSession
            {
                UserAuthId = uid,
                CreatedAt = DateTime.UtcNow,
                Permissions = new List<string> { RefreshPermission }
            },
                issuer: jwtProvider.Issuer, expireIn: TimeSpan.FromDays(365));

            refreshBody["useragent"] = Request.UserAgent;
            refreshBody["session"] = body["session"];

            token.RefreshToken = JwtAuthProvider.CreateEncryptedJweToken(refreshBody, jwtProvider.PublicKey.Value);
            
            return token;
        }

        /// <summary>
        /// Генерация пары токенов для пользователя
        /// </summary>
        /// <returns>Пара токенов: Access и Refresh</returns>
        internal async Task<TokenResult> GenerateToken(User user, TimeSpan? customAccessExpire = null, TimeSpan? customRefreshExpire = null)
        {
            //var user = await UserRepository.GetWithIncludesAsync(uid);
            var sessionId = Guid.NewGuid().ToString();

            var token = new TokenResult();

            var jwtProvider = (JwtAuthProvider)AuthenticateService.GetAuthProvider("jwt");
            if (jwtProvider?.PublicKey == null)
                throw new HttpError(HttpStatusCode.ServiceUnavailable, "AuthProvider does not work");

            var defaultAccessExpire = jwtProvider.ExpireTokensIn;
            //Можно запросить время жизни токена не больше чем 'defaultAccessExpire'
            var accExpSpan = (customAccessExpire ?? defaultAccessExpire) >= defaultAccessExpire
                ? defaultAccessExpire
                : customAccessExpire.Value;

            var body = JwtAuthProvider.CreateJwtPayload(new AuthUserSession
            {
                UserAuthId = user.Id,
                CreatedAt = DateTime.UtcNow,
                DisplayName = user.Login,
            },
                issuer: jwtProvider.Issuer, expireIn: accExpSpan);

            body["useragent"] = Request.UserAgent;
            body["session"] = sessionId;

            token.AccessToken = JwtAuthProvider.CreateEncryptedJweToken(body, jwtProvider.PublicKey.Value);

            var defaultRefreshExpire = jwtProvider.ExpireRefreshTokensIn;
            var refExpSpan = (customRefreshExpire ?? defaultRefreshExpire) >= defaultRefreshExpire
                ? defaultRefreshExpire
                : customRefreshExpire.Value;

            var refreshBody = JwtAuthProvider.CreateJwtPayload(new AuthUserSession
            {
                UserAuthId = user.Id,
                DisplayName = user.Login,
                CreatedAt = DateTime.UtcNow,
                Permissions = new List<string> { RefreshPermission },
            },
                issuer: jwtProvider.Issuer, expireIn: refExpSpan);

            refreshBody["useragent"] = Request.UserAgent;
            refreshBody["session"] = body["session"];

            token.RefreshToken = JwtAuthProvider.CreateEncryptedJweToken(refreshBody, jwtProvider.PublicKey.Value);

            return token;
        }

        public async Task<PasswordChangeResult> Post(CreatePassword request)
        {
            var session = Request.ThrowIfUnauthorized();

            var uid = session?.UserAuthId;
            if (uid == null)
                throw new HttpError(HttpStatusCode.Unauthorized);

            var user = new User(); //await UserRepository.GetFirstAsync(u => u.Id == uid);
            if (user == null)
                throw new HttpError(HttpStatusCode.NotFound);

            if (user.Password != null)
                throw new HttpError(HttpStatusCode.Conflict);

            user.Password = request.NewPassword;

            //await UserRepository.SaveAsync(user);

            return new PasswordChangeResult { Result = PasswordChangeResult.ChangeEnum.Created };
        }
        
        public async Task<UserProfileMold> Get(GetMyProfile request)
        {
            var session = Request.ThrowIfUnauthorized();

            var uid = session?.UserAuthId;
            if (uid == null)
                throw new HttpError(HttpStatusCode.Unauthorized);

            var me = new User(); //await UserRepository.GetWithIncludesAsync(uid);
            if (me == null)
                throw new HttpError(HttpStatusCode.NotFound);

            var profile = new UserProfileMold
            {
                UserId = session.UserAuthId,
                Login = session.DisplayName,
                IsPasswordSetted = me.Password != null,
            };

            return profile;
        }

        public async Task<TokenResult> Get(GetToken request)
        {
            var uid = $"{anonimPrefix}{Guid.NewGuid()}";

            var user = await CreateUser(uid, request.Login);

            var token = await GenerateToken(user);

            return token;
        }

        internal async Task<User> CreateUser(string uid, string login)
        {
            var user = new User
            {
                Id = uid,
                Login = login,
                RegisteredTime = DateTimeOffset.UtcNow,
            };
            //await AnonimUserRepository.SaveAsync(user);
            return user;
        }
    }
}