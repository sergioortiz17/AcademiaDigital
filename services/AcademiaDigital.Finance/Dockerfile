# Build context = ./services (ver docker-compose.finance.yml), para poder incluir el
# proyecto compartido AcademiaDigital.Finance.Contracts que vive fuera de la carpeta del
# servicio pero dentro de services/.

# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy project files first (layer cache para restore)
COPY AcademiaDigital.Finance.Contracts/AcademiaDigital.Finance.Contracts.csproj                              ./AcademiaDigital.Finance.Contracts/
COPY AcademiaDigital.Finance/src/AcademiaDigital.Finance.Domain/AcademiaDigital.Finance.Domain.csproj         ./AcademiaDigital.Finance/src/AcademiaDigital.Finance.Domain/
COPY AcademiaDigital.Finance/src/AcademiaDigital.Finance.Application/AcademiaDigital.Finance.Application.csproj ./AcademiaDigital.Finance/src/AcademiaDigital.Finance.Application/
COPY AcademiaDigital.Finance/src/AcademiaDigital.Finance.Infrastructure/AcademiaDigital.Finance.Infrastructure.csproj ./AcademiaDigital.Finance/src/AcademiaDigital.Finance.Infrastructure/
COPY AcademiaDigital.Finance/src/AcademiaDigital.Finance.API/AcademiaDigital.Finance.API.csproj               ./AcademiaDigital.Finance/src/AcademiaDigital.Finance.API/

RUN dotnet restore AcademiaDigital.Finance/src/AcademiaDigital.Finance.API/AcademiaDigital.Finance.API.csproj

# Copy sources and publish
COPY AcademiaDigital.Finance.Contracts/ ./AcademiaDigital.Finance.Contracts/
COPY AcademiaDigital.Finance/src/ ./AcademiaDigital.Finance/src/
RUN dotnet publish AcademiaDigital.Finance/src/AcademiaDigital.Finance.API/AcademiaDigital.Finance.API.csproj \
    -c Release -o /publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /publish .

ENV ASPNETCORE_URLS=http://+:8091
EXPOSE 8091

ENTRYPOINT ["dotnet", "AcademiaDigital.Finance.API.dll"]
