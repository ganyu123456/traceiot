# ============================================================
# 阶段一：构建 React 应用（Node 20 Alpine）
# ============================================================
FROM node:20-alpine AS build
WORKDIR /app

# 先复制 package 文件，利用缓存层跳过不必要的 npm ci
COPY package.json package-lock.json ./
RUN npm ci --prefer-offline

# 复制源代码并构建
COPY . .
RUN npm run build

# ============================================================
# 阶段二：Nginx 静态服务（Alpine 极简镜像）
# ============================================================
FROM nginx:1.27-alpine AS runtime

# 替换默认配置
COPY nginx.conf /etc/nginx/conf.d/default.conf

# 复制构建产物
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80

HEALTHCHECK --interval=30s --timeout=5s --retries=3 \
    CMD wget -qO- http://localhost/health || exit 1
