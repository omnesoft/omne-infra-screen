# syntax=docker/dockerfile:1

FROM node:20-alpine AS build
WORKDIR /app

COPY src/ui/package*.json ./
RUN npm ci

COPY src/ui/ ./
RUN npm run build

FROM nginxinc/nginx-unprivileged:1.31.2-alpine3.23 AS final
COPY --from=build /app/dist /usr/share/nginx/html
COPY src/ui/nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 8080
