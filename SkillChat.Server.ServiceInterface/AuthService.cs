﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Session;
using Serilog;
using ServiceStack;
using ServiceStack.Auth;
using SkillChat.Server.Domain;
using SkillChat.Server.ServiceModel;
using SkillChat.Server.ServiceModel.Molds;

namespace SkillChat.Server.ServiceInterface
{
    public class AuthService : Service
    {
        private const string RefreshPermission = "refresh";
        private const string UserPrefix = "User/";
        private const string SecretPostfix = "/secret";

        public IAsyncDocumentSession RavenSession { get; set; }

        #region Методы эндпоинтов
        public async Task<TokenResult> Post(RegisterNewUser request)
        {
            var login = request.Login.ToLowerInvariant();

            var user = await GetUserByLogin(login);
            if (user != null)
                throw new HttpError(HttpStatusCode.BadRequest, "Пользователь с таким логином уже существует");
            if (string.IsNullOrWhiteSpace(request.Password))
                throw new HttpError(HttpStatusCode.BadRequest, "Пароль не может быть пустым");

            user = await CreateUser(login, request.Password);
            var defaultChat = await RavenSession.Query<Chat>().FirstAsync(e => e.ChatName == "SkillBoxChat");
            if(defaultChat.Members == null)
            {
                defaultChat.Members = new List<ChatMember>();
            }
            defaultChat.Members.Add(new ChatMember() { UserId = user.Id, UserRole = ChatMemberRole.Participient });
            await RavenSession.SaveChangesAsync();

            var tokenResult = await GenerateToken(user);

            return tokenResult;
        }
        public async Task<TokenResult> Post(AuthViaPassword request)
        {
            var login = request.Login.ToLowerInvariant();

            var user = await GetUserByLogin(login);
            if (user == null)
                throw new HttpError(HttpStatusCode.NotFound, "User is not found");

            var secret = await RavenSession.LoadAsync<UserSecret>(user.Id + SecretPostfix);

            if (string.IsNullOrWhiteSpace(secret?.Password))
                throw new HttpError(HttpStatusCode.NotFound, "Password is not set");

            if (secret.Password != request.Password)
                throw new HttpError(HttpStatusCode.NotFound, "Password is wrong");

            var customAccessExpire = (request.AccessTokenExpirationPeriod.HasValue && request.AccessTokenExpirationPeriod >= 0)
                ? TimeSpan.FromSeconds(request.AccessTokenExpirationPeriod.Value) : (TimeSpan?)null;
            var customRefreshExpire = (request.RefreshTokenExpirationPeriod.HasValue && request.RefreshTokenExpirationPeriod >= 0)
                ? TimeSpan.FromSeconds(request.RefreshTokenExpirationPeriod.Value) : (TimeSpan?)null;

            var tokenResult = await GenerateToken(user, customAccessExpire, customRefreshExpire);

            return tokenResult;
        }

        [Authenticate]
        public async Task<TokenResult> Post(PostRefreshToken request)
        {
            var session = Request.ThrowIfUnauthorized();
            var uid = session.UserAuthId;

            User user = await GetUserById(uid);

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

            var tokenResult = await GenerateToken(user, customAccessExpire, customRefreshExpire);

            if (tokenResult == null)
                throw new HttpError(HttpStatusCode.Conflict, "Roles is not setted");

            return tokenResult;
        }

        [Authenticate]
        public async Task<PasswordChangeResult> Post(CreatePassword request)
        {
            var session = Request.ThrowIfUnauthorized();

            var uid = session?.UserAuthId;
            if (uid == null)
                throw new HttpError(HttpStatusCode.Unauthorized);

            var user = await GetUserById(uid);
            if (user == null)
                throw new HttpError(HttpStatusCode.NotFound);

            var secret = await GetUserSecret(uid);
            if (secret.Password != null)
                throw new HttpError(HttpStatusCode.Conflict);

            secret.Password = request.NewPassword;

            await RavenSession.StoreAsync(secret);
            await RavenSession.SaveChangesAsync();

            return new PasswordChangeResult { Result = PasswordChangeResult.ChangeEnum.Created };
        }

        [Authenticate]
        public async Task<UserProfileMold> Get(GetMyProfile request)
        {
            var session = Request.ThrowIfUnauthorized();

            var uid = session?.UserAuthId;
            if (uid == null)
                throw new HttpError(HttpStatusCode.Unauthorized);

            var me = await GetUserById(uid);
            if (me == null)
                throw new HttpError(HttpStatusCode.NotFound);

            var secret = await GetUserSecret(uid);

            var profile = new UserProfileMold
            {
                UserId = session.UserAuthId,
                Login = session.DisplayName,
                IsPasswordSetted = secret.Password != null,
            };

            return profile;
        }

        public async Task<TokenResult> Get(GetToken request)
        {
            request.Login = request.Login.ToLowerInvariant();
            var user = await GetUserByLogin(request.Login);
            if (user == null)
            {
                user = await CreateUser(request.Login);
            }
            else
            {
                var secret = await RavenSession.LoadAsync<UserSecret>(user.Id + SecretPostfix);
                if (secret != null)
                {
                    throw HttpError.Unauthorized("Need a password");
                }
            }

            var token = await GenerateToken(user);

            Log.Information($"Created tokens pair for {user.Login}({user.Id})");
            return token;
        }

        #endregion

        #region Внутренние методы
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

        private async Task<UserSecret> GetUserSecret(string uid)
        {
            var secret = await RavenSession.LoadAsync<UserSecret>(uid + SecretPostfix);
            return secret;
        }

        private async Task<User> GetUserById(string uid)
        {
            return await RavenSession.LoadAsync<User>(uid, b => b.IncludeDocuments(uid + SecretPostfix));
        }

        private async Task<User> GetUserByLogin(string login)
        {
            var user = await RavenSession.Query<User>().Where(x => x.Login == login).Include(x => x.Id + SecretPostfix)
                .FirstOrDefaultAsync();
            return user;
        }

        private async Task<User> CreateUser(string login, string password = null)
        {
            var uid = $"{UserPrefix}{Guid.NewGuid()}";
            var user = new User
            {
                Id = uid,
                Login = login,
                RegisteredTime = DateTimeOffset.UtcNow,
                Password = password
            };
            await RavenSession.StoreAsync(user);
            await RavenSession.SaveChangesAsync();
            return user;
        }

        #endregion
    }
}