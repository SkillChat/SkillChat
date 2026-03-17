#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5000
EXPOSE 5001
EXPOSE 5002

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy only .csproj files first for better layer caching
COPY ["SkillChat.Server/SkillChat.Server.csproj", "SkillChat.Server/"]
COPY ["SkillChat.Interface/SkillChat.Interface.csproj", "SkillChat.Interface/"]
COPY ["SkillChat.Server.ServiceInterface/SkillChat.Server.ServiceInterface.csproj", "SkillChat.Server.ServiceInterface/"]
COPY ["SkillChat.Server.ServiceModel/SkillChat.Server.ServiceModel.csproj", "SkillChat.Server.ServiceModel/"]
COPY ["SkillChat.Server.Domain/SkillChat.Server.Domain.csproj", "SkillChat.Server.Domain/"]

# Restore dependencies (this layer is cached when only source code changes)
RUN dotnet restore "SkillChat.Server/SkillChat.Server.csproj"

# Copy the rest of the source code
COPY . .
WORKDIR "/src/SkillChat.Server"
RUN dotnet build "SkillChat.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SkillChat.Server.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app

# Create non-root user for security
RUN adduser --disabled-password --gecos "" --no-create-home appuser

COPY --from=publish /app/publish .

# Switch to non-root user
USER appuser

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
  CMD wget --no-verbose --tries=1 --spider http://localhost:5000/ || exit 1

ENTRYPOINT ["dotnet", "SkillChat.Server.dll"]
