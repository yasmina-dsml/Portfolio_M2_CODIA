FROM mcr.microsoft.com/dotnet/sdk:8.0.101 AS build
WORKDIR /app

COPY server/OkeyApi/ ./
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:8.0.1 AS runtime
WORKDIR /app
COPY --from=build /app/out ./

EXPOSE 3031
ENTRYPOINT ["dotnet", "OkeyApi.dll"]
