# Build context = raíz del repo (ver docker-compose.yml), porque Infrastructure referencia
# el proyecto compartido services/AcademiaDigital.Finance.Contracts (solo DTOs del contrato
# HTTP con el microservicio Finance, ADR 0001).

# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy project files first (layer cache for restore)
COPY backend-dotnet/src/AcademiaDigital.Domain/AcademiaDigital.Domain.csproj                 ./backend-dotnet/src/AcademiaDigital.Domain/
COPY backend-dotnet/src/AcademiaDigital.Application/AcademiaDigital.Application.csproj        ./backend-dotnet/src/AcademiaDigital.Application/
COPY backend-dotnet/src/AcademiaDigital.Infrastructure/AcademiaDigital.Infrastructure.csproj ./backend-dotnet/src/AcademiaDigital.Infrastructure/
COPY backend-dotnet/src/AcademiaDigital.API/AcademiaDigital.API.csproj                       ./backend-dotnet/src/AcademiaDigital.API/
COPY services/AcademiaDigital.Finance.Contracts/AcademiaDigital.Finance.Contracts.csproj     ./services/AcademiaDigital.Finance.Contracts/

RUN dotnet restore backend-dotnet/src/AcademiaDigital.API/AcademiaDigital.API.csproj

# Copy sources and publish
COPY backend-dotnet/src ./backend-dotnet/src
COPY services/AcademiaDigital.Finance.Contracts ./services/AcademiaDigital.Finance.Contracts
RUN dotnet publish backend-dotnet/src/AcademiaDigital.API/AcademiaDigital.API.csproj \
    -c Release -o /publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /publish .

ENV ASPNETCORE_URLS=http://+:8000
EXPOSE 8000

ENTRYPOINT ["dotnet", "AcademiaDigital.API.dll"]
