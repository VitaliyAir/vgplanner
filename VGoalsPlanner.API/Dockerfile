# VGoalsPlanner API. Контекст сборки: папка решения (D:\Family\Developer\VGoalsPlanner), например
#   docker build -f VGoalsPlanner.API/Dockerfile -t vgplanner-api .
# Запускается из D:\Family\Developer\VDocker\docker-compose.yml.
#
# Версия API берётся из VGoalsPlanner.API/version.txt через Directory.Build.props (файл лежит
# в контексте сборки), коммит приходит аргументом APP_COMMIT: в контексте нет .git, поэтому
# SourceLink сам его не определит. Обычно аргументы передаёт scripts/release.ps1.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Общие MSBuild-свойства и файл версии API — до restore, чтобы Version была одинаковой на всех шагах.
COPY Directory.Build.props ./
COPY VGoalsPlanner.API/version.txt VGoalsPlanner.API/version.txt

# Сначала только файлы проектов: слой restore переиспользуется, пока меняется только код.
COPY VGoalsPlanner.Domain/VGoalsPlanner.Domain.csproj VGoalsPlanner.Domain/
COPY VGoalsPlanner.Application/VGoalsPlanner.Application.csproj VGoalsPlanner.Application/
COPY VGoalsPlanner.Infrastructure/VGoalsPlanner.Infrastructure.csproj VGoalsPlanner.Infrastructure/
COPY VGoalsPlanner.API/VGoalsPlanner.API.csproj VGoalsPlanner.API/
RUN dotnet restore VGoalsPlanner.API/VGoalsPlanner.API.csproj

COPY VGoalsPlanner.Domain/ VGoalsPlanner.Domain/
COPY VGoalsPlanner.Application/ VGoalsPlanner.Application/
COPY VGoalsPlanner.Infrastructure/ VGoalsPlanner.Infrastructure/
COPY VGoalsPlanner.API/ VGoalsPlanner.API/

ARG APP_VERSION=
ARG APP_COMMIT=
# ${APP_VERSION:+-p:Version=$APP_VERSION} подставляется только при переданном аргументе;
# без него версия остаётся прочитанной из version.txt.
RUN dotnet publish VGoalsPlanner.API/VGoalsPlanner.API.csproj -c Release --no-restore -o /app/publish -p:UseAppHost=false \
    -p:SourceRevisionId=$APP_COMMIT ${APP_VERSION:+-p:Version=$APP_VERSION}

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

# Только для labels образа; авторитетная версия — в сборке и в GET /api/version.
ARG APP_VERSION=0.0.0
ARG APP_COMMIT=
LABEL org.opencontainers.image.title="VGoalsPlanner API" \
      org.opencontainers.image.version="$APP_VERSION" \
      org.opencontainers.image.revision="$APP_COMMIT"

WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Docker
EXPOSE 8080
ENTRYPOINT ["dotnet", "VGoalsPlanner.API.dll"]
