#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1.5-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["SkillChat.Server/SkillChat.Server.csproj", "SkillChat.Server/"]
COPY ["SkillChat.Interface/SkillChat.Interface.csproj", "SkillChat.Interface/"]
COPY ["SkillChat.Server.ServiceInterface/SkillChat.Server.ServiceInterface.csproj", "SkillChat.Server.ServiceInterface/"]
COPY ["SkillChat.Server.ServiceModel/SkillChat.Server.ServiceModel.csproj", "SkillChat.Server.ServiceModel/"]
COPY ["SkillChat.Server.Domain/SkillChat.Server.Domain.csproj", "SkillChat.Server.Domain/"]
RUN dotnet restore "SkillChat.Server/SkillChat.Server.csproj"
COPY . .
WORKDIR "/src/SkillChat.Server"
RUN dotnet build "SkillChat.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SkillChat.Server.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SkillChat.Server.dll"]
