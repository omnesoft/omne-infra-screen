# ── Stage 1: build (cached until package-lock.json changes) ──────────────────
FROM node:24-alpine AS build
WORKDIR /app

# Copy lockfile first so npm ci layer is cache-stable
COPY src/ui/package.json src/ui/package-lock.json ./
RUN npm ci

COPY src/ui/ ./
RUN npm run build

# ── Stage 2: serve (small, non-root) ─────────────────────────────────────────
FROM nginx:alpine AS runtime

# Remove default config
RUN rm /etc/nginx/conf.d/default.conf

COPY src/ui/nginx.conf /etc/nginx/conf.d/app.conf
COPY --from=build /app/dist /usr/share/nginx/html

# nginx:alpine already has a non-root 'nginx' user; configure it to run without
# needing privileged port binding by using port 80 via the default nginx master
# process which drops privileges after binding. For stricter setups swap to 8080.
EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
