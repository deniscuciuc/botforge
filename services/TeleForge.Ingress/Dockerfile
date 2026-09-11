FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Root monorepo build configuration
COPY Directory.Build.props global.json nuget.config ./

# telegram-bot module files
COPY telegram-bot/Directory.Build.props telegram-bot/
COPY telegram-bot/Directory.Packages.props telegram-bot/
COPY telegram-bot/src/ telegram-bot/src/
COPY telegram-bot/services/ telegram-bot/services/

WORKDIR /src/telegram-bot/services/TeleForge.Ingress
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN addgroup --system --gid 1001 appgroup && \
    adduser --system --uid 1001 --ingroup appgroup appuser

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_RUNNING_IN_CONTAINER=true

EXPOSE 8080
EXPOSE 9464

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
  CMD curl -f http://localhost:8080/healthz || exit 1

USER appuser
ENTRYPOINT ["dotnet", "TeleForge.Ingress.dll"]
