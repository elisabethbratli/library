FROM node:22-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
RUN npm run build

FROM nginx:1.27-alpine
COPY templates/ /etc/nginx/templates/
COPY --from=build /app/dist /usr/share/nginx/html
# Only substitute our own variables so nginx's $uri etc. are left alone.
ENV NGINX_ENVSUBST_FILTER="^API_"
ENV API_UPSTREAM=http://localhost:5080 API_HOST=localhost
EXPOSE 8080
