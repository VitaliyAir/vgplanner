# VGoalsPlanner UI: сборка Vite, раздаёт nginx, /api проксируется в контейнер API.
# Контекст сборки: VGoalsPlanner.UIReact. Настройки OIDC вшиваются при сборке (build arguments).
#
# Версия UI берётся из version.txt рядом с этим Dockerfile (её читает vite.config.ts),
# поэтому build-arg для неё не нужен и забыть его невозможно. APP_VERSION ниже — только label.

FROM node:22-alpine AS build
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci
COPY . .

ARG VITE_OIDC_AUTHORITY=http://localhost:8083/realms/VEco
ARG VITE_OIDC_CLIENT_ID=vgplanner
# Коммит знает только сборка: в контексте нет .git.
ARG APP_COMMIT=
ENV VITE_OIDC_AUTHORITY=$VITE_OIDC_AUTHORITY \
    VITE_OIDC_CLIENT_ID=$VITE_OIDC_CLIENT_ID \
    VITE_APP_COMMIT=$APP_COMMIT
RUN npm run build

FROM nginx:1.27-alpine

# Только для labels образа; авторитетная версия — в собранном бандле (подпись в шапке).
ARG APP_VERSION=0.0.0
ARG APP_COMMIT=
LABEL org.opencontainers.image.title="VGoalsPlanner UI" \
      org.opencontainers.image.version="$APP_VERSION" \
      org.opencontainers.image.revision="$APP_COMMIT"

COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
