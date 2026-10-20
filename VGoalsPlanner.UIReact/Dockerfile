# VGoalsPlanner UI: сборка Vite, раздаёт nginx, /api проксируется в контейнер API.
# Контекст сборки: VGoalsPlanner.UIReact. Настройки OIDC вшиваются при сборке (build arguments).

FROM node:22-alpine AS build
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
ARG VITE_OIDC_AUTHORITY=http://localhost:8083/realms/VEco
ARG VITE_OIDC_CLIENT_ID=vgplanner
ENV VITE_OIDC_AUTHORITY=$VITE_OIDC_AUTHORITY \
    VITE_OIDC_CLIENT_ID=$VITE_OIDC_CLIENT_ID
RUN npm run build

FROM nginx:1.27-alpine
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
