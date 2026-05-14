# Этап 1: Сборка (Node.js)
FROM node:18-alpine as build
WORKDIR /app

# Копируем файлы зависимостей
COPY package*.json ./

# Устанавливаем зависимости
RUN npm install

# Копируем исходный код
COPY . .

# Собираем проект (игнорируя ошибки типов, если они есть в скрипте build)
RUN npm run build

# Этап 2: Запуск (Nginx)
FROM nginx:alpine

# Копируем собранные файлы из папки dist (стандарт для Vite)
COPY --from=build /app/dist /usr/share/nginx/html

# Создаем конфиг Nginx для поддержки React Router (Single Page Application)
RUN echo 'server { \
    listen 80; \
    root /usr/share/nginx/html; \
    index index.html; \
    location / { \
        try_files $uri $uri/ /index.html; \
    } \
}' > /etc/nginx/conf.d/default.conf

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]