# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy solution and project files first (layer cache for restore)
COPY AcademiaDigital.sln ./
COPY src/AcademiaDigital.Domain/AcademiaDigital.Domain.csproj        ./src/AcademiaDigital.Domain/
COPY src/AcademiaDigital.Application/AcademiaDigital.Application.csproj ./src/AcademiaDigital.Application/
COPY src/AcademiaDigital.Infrastructure/AcademiaDigital.Infrastructure.csproj ./src/AcademiaDigital.Infrastructure/
COPY src/AcademiaDigital.API/AcademiaDigital.API.csproj               ./src/AcademiaDigital.API/

RUN dotnet restore

# Copy everything and publish
COPY . .
RUN dotnet publish src/AcademiaDigital.API/AcademiaDigital.API.csproj \
    -c Release -o /publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /publish .

ENV ASPNETCORE_URLS=http://+:8000
EXPOSE 8000

ENTRYPOINT ["dotnet", "AcademiaDigital.API.dll"]
