FROM mcr.microsoft.com/dotnet/sdk:8.0.101 AS build
WORKDIR /app

COPY server/OkeyApi/ ./
RUN dotnet tool restore
RUN dotnet ef migrations add Init

ENTRYPOINT ["dotnet", "ef", "database", "update"]
