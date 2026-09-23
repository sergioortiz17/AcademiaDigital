# Dockerfile de la herramienta de DESARROLLO (dev-tools).
# IMPORTANTE: el build context es la RAÍZ del repo (ver docker-compose.dev-tools.yml),
# porque el proyecto referencia backend-dotnet/src/AcademiaDigital.{Application,Infrastructure}.
# Esta imagen NO forma parte de la build de producción del backend/frontend reales.

# Stage 1: build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# csproj primero (cache de restore)
COPY dev-tools/AcademiaDigital.DevTools/AcademiaDigital.DevTools.csproj dev-tools/AcademiaDigital.DevTools/
COPY backend-dotnet/src/AcademiaDigital.Domain/AcademiaDigital.Domain.csproj                 backend-dotnet/src/AcademiaDigital.Domain/
COPY backend-dotnet/src/AcademiaDigital.Application/AcademiaDigital.Application.csproj       backend-dotnet/src/AcademiaDigital.Application/
COPY backend-dotnet/src/AcademiaDigital.Infrastructure/AcademiaDigital.Infrastructure.csproj backend-dotnet/src/AcademiaDigital.Infrastructure/
COPY services/AcademiaDigital.Finance.Contracts/AcademiaDigital.Finance.Contracts.csproj     services/AcademiaDigital.Finance.Contracts/

RUN dotnet restore dev-tools/AcademiaDigital.DevTools/AcademiaDigital.DevTools.csproj

# fuentes necesarias
COPY dev-tools/AcademiaDigital.DevTools/ dev-tools/AcademiaDigital.DevTools/
COPY backend-dotnet/src/AcademiaDigital.Domain/         backend-dotnet/src/AcademiaDigital.Domain/
COPY backend-dotnet/src/AcademiaDigital.Application/     backend-dotnet/src/AcademiaDigital.Application/
COPY backend-dotnet/src/AcademiaDigital.Infrastructure/  backend-dotnet/src/AcademiaDigital.Infrastructure/
COPY services/AcademiaDigital.Finance.Contracts/         services/AcademiaDigital.Finance.Contracts/

RUN dotnet publish dev-tools/AcademiaDigital.DevTools/AcademiaDigital.DevTools.csproj \
    -c Release -o /publish

# Stage 2: runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /publish .

ENV ASPNETCORE_URLS=http://+:8090
ENV DevTools__SeedsPath=/seeds
EXPOSE 8090

ENTRYPOINT ["dotnet", "AcademiaDigital.DevTools.dll"]
