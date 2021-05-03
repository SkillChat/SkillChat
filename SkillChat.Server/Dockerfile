#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0.5 AS base
WORKDIR /app
EXPOSE 5000
EXPOSE 5001
EXPOSE 5002

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
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
