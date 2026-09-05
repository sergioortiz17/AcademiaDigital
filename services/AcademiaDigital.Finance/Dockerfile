# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy project files first (layer cache for restore)
COPY src/AcademiaDigital.Finance.Domain/AcademiaDigital.Finance.Domain.csproj                 ./src/AcademiaDigital.Finance.Domain/
COPY src/AcademiaDigital.Finance.Application/AcademiaDigital.Finance.Application.csproj       ./src/AcademiaDigital.Finance.Application/
COPY src/AcademiaDigital.Finance.Infrastructure/AcademiaDigital.Finance.Infrastructure.csproj ./src/AcademiaDigital.Finance.Infrastructure/
COPY src/AcademiaDigital.Finance.API/AcademiaDigital.Finance.API.csproj                       ./src/AcademiaDigital.Finance.API/

RUN dotnet restore src/AcademiaDigital.Finance.API/AcademiaDigital.Finance.API.csproj

# Copy sources and publish
COPY src ./src
RUN dotnet publish src/AcademiaDigital.Finance.API/AcademiaDigital.Finance.API.csproj \
    -c Release -o /publish --no-restore

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /publish .

ENV ASPNETCORE_URLS=http://+:8091
EXPOSE 8091

ENTRYPOINT ["dotnet", "AcademiaDigital.Finance.API.dll"]
