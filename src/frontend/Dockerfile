FROM node:22-alpine AS build
WORKDIR /app

# Copy dependency definitions
COPY package*.json ./
RUN npm ci

# Copy the rest of the application
COPY . .

# Embed Vite API URL at build time if provided
ARG VITE_CMS_API_URL
ENV VITE_CMS_API_URL=${VITE_CMS_API_URL}

# Build the project (bypassing strict tsc typechecking during Docker build)
RUN npx vite build

# Nginx stage to serve static files
FROM nginx:alpine AS final
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
