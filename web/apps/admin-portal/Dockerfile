FROM node:22-alpine AS build
WORKDIR /workspace
COPY . .
RUN npm ci
RUN npm run build --workspace @modular-mlm/admin-portal

FROM node:22-alpine AS runtime
ENV NODE_ENV=production \
    PORT=8080 \
    HOSTNAME=0.0.0.0
WORKDIR /app
COPY --from=build --chown=node:node /workspace/apps/admin-portal/.next/standalone/ ./
USER node
WORKDIR /app/apps/admin-portal
EXPOSE 8080
CMD ["node", "server.js"]
