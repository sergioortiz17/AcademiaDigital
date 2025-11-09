# Stage 1: Build del frontend (compilación)
FROM node:18-alpine AS builder

WORKDIR /app

# Copiar package.json primero para cachear las dependencias
COPY package.json ./

# Instalar dependencias (sin --frozen-lockfile para permitir nuevas dependencias)
RUN npm install

COPY . .

ARG REACT_APP_API_URL
ENV REACT_APP_API_URL=$REACT_APP_API_URL

RUN npm run build

# Stage 2: Servidor Nginx para producción
FROM nginx:alpine

COPY --from=builder /app/build /usr/share/nginx/html

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
