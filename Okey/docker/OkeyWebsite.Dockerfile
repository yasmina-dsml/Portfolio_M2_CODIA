FROM node:22.1.0-bookworm
WORKDIR /app

COPY server/okeywebsite/new_app/ ./
RUN npm install

EXPOSE 3000
ENTRYPOINT ["node", "index.js"]
