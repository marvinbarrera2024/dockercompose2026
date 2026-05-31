FROM node:18-alpine
WORKDIR /app
RUN npm install express pg redis
COPY server.js .
EXPOSE 3000
CMD ["node", "server.js"]