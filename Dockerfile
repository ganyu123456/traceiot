# ============================================================
# 阶段一：编译发布（.NET 8 SDK）
# ============================================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 先只复制 csproj 文件，利用 Docker Layer 缓存加速 NuGet 还原
COPY backend/TraceIot.sln                                                                         backend/
COPY backend/src/TraceIot.Domain.Shared/TraceIot.Domain.Shared.csproj                            backend/src/TraceIot.Domain.Shared/
COPY backend/src/TraceIot.Domain/TraceIot.Domain.csproj                                          backend/src/TraceIot.Domain/
COPY backend/src/TraceIot.Application.Contracts/TraceIot.Application.Contracts.csproj            backend/src/TraceIot.Application.Contracts/
COPY backend/src/TraceIot.Application/TraceIot.Application.csproj                                backend/src/TraceIot.Application/
COPY backend/src/TraceIot.EntityFrameworkCore/TraceIot.EntityFrameworkCore.csproj                backend/src/TraceIot.EntityFrameworkCore/
COPY backend/src/TraceIot.HttpApi/TraceIot.HttpApi.csproj                                        backend/src/TraceIot.HttpApi/
COPY backend/src/TraceIot.HttpApi.Host/TraceIot.HttpApi.Host.csproj                              backend/src/TraceIot.HttpApi.Host/
COPY backend/src/TraceIot.MqttWorker/TraceIot.MqttWorker.csproj                                  backend/src/TraceIot.MqttWorker/

WORKDIR /src/backend
RUN dotnet restore TraceIot.sln

# 复制全部源代码并发布
COPY backend/ .
RUN dotnet publish src/TraceIot.HttpApi.Host/TraceIot.HttpApi.Host.csproj \
    -c Release -o /app/publish --no-restore --no-self-contained

# ============================================================
# 阶段二：运行时（ASP.NET 8 Runtime，体积更小）
# ============================================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# 创建非 root 用户运行，提升安全性
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

COPY --from=build --chown=appuser /app/publish .

EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true

ENTRYPOINT ["dotnet", "TraceIot.HttpApi.Host.dll"]
