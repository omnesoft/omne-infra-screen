# syntax=docker/dockerfile:1
# Build context: src/ui/

# ── build ──────────────────────────────────────────────────────────────────────
# Restore node_modules in a cache-stable layer before copying the rest of the
# sources so a code-only change doesn't re-run npm ci.
FROM node:22-alpine AS build
WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci

COPY . .
RUN npm run build

# ── runtime ────────────────────────────────────────────────────────────────────
FROM nginx:1.27-alpine AS runtime

COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf.template /etc/nginx/nginx.conf.template

# /tmp/nginx is used for the PID file and proxy buffers so the nginx user
# doesn't need access to root-owned /var/run or /var/cache/nginx.
RUN mkdir -p /tmp/nginx && \
    chown -R nginx:nginx \
        /tmp/nginx \
        /var/cache/nginx \
        /var/log/nginx \
        /usr/share/nginx/html

USER nginx
EXPOSE 8080

# BFF_ORIGIN is the base URL of the BFF container reachable from within the
# Docker network (e.g. http://bff:8080). envsubst replaces only that
# placeholder, leaving nginx's own $variable references intact.
ENV BFF_ORIGIN=http://bff:8080

CMD ["/bin/sh", "-c", \
     "envsubst '${BFF_ORIGIN}' \
        < /etc/nginx/nginx.conf.template \
        > /tmp/nginx/nginx.conf \
      && exec nginx -c /tmp/nginx/nginx.conf -g 'daemon off;'"]
