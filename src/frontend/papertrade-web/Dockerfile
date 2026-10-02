FROM node:24-alpine AS build

WORKDIR /app

COPY src/frontend/papertrade-web/package.json .
COPY src/frontend/papertrade-web/package-lock.json .

RUN npm ci

COPY src/frontend/papertrade-web/ .

ARG VITE_API_BASE_URL=http://localhost:5044
ENV VITE_API_BASE_URL=${VITE_API_BASE_URL}

RUN npm run build

FROM nginx:stable-alpine AS final

COPY src/frontend/papertrade-web/nginx.conf \
    /etc/nginx/conf.d/default.conf

COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80

HEALTHCHECK \
    --interval=10s \
    --timeout=3s \
    --start-period=5s \
    --retries=3 \
    CMD wget --quiet --tries=1 --spider http://127.0.0.1/health || exit 1
