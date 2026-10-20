# VGoalsPlanner API. Контекст сборки: папка решения (D:\Family\Developer\VGoalsPlanner), например
#   docker build -f VGoalsPlanner.API/Dockerfile -t vgplanner-api .
# Запускается из D:\Family\Developer\VDocker\docker-compose.yml.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

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
RUN dotnet publish VGoalsPlanner.API/VGoalsPlanner.API.csproj -c Release --no-restore -o /app/publish -p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Docker
EXPOSE 8080
ENTRYPOINT ["dotnet", "VGoalsPlanner.API.dll"]
