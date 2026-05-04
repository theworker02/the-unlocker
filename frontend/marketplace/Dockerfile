FROM node:25-alpine AS build
WORKDIR /app
COPY frontend/marketplace/package*.json ./
RUN npm install
COPY frontend/marketplace ./
RUN npm run build

FROM nginx:1.27-alpine
COPY --from=build /app/dist /usr/share/nginx/html
COPY frontend/marketplace/nginx.conf /etc/nginx/conf.d/default.conf
