# Etapa 1: compilar y publicar la API
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

COPY src/CombustibleAPI.Domain/CombustibleAPI.Domain.csproj src/CombustibleAPI.Domain/
COPY src/CombustibleAPI.Application/CombustibleAPI.Application.csproj src/CombustibleAPI.Application/
COPY src/CombustibleAPI.Infrastructure/CombustibleAPI.Infrastructure.csproj src/CombustibleAPI.Infrastructure/
COPY src/CombustibleAPI.Api/CombustibleAPI.Api.csproj src/CombustibleAPI.Api/

RUN dotnet restore src/CombustibleAPI.Api/CombustibleAPI.Api.csproj

COPY src/ src/

RUN dotnet publish src/CombustibleAPI.Api/CombustibleAPI.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# Etapa 2: ejecutar únicamente la aplicación publicada
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://0.0.0.0:10000

EXPOSE 10000

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "CombustibleAPI.Api.dll"]