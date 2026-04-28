FROM node:24-alpine AS build
WORKDIR /app

COPY src/FilesXchange.Client/package.json src/FilesXchange.Client/package-lock.json ./
RUN npm ci --no-audit --no-fund

COPY src/FilesXchange.Client/ ./
RUN npm run build

FROM nginx:alpine AS runtime
COPY src/FilesXchange.Client/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80

