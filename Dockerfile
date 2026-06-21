# Build and run NseBhavcopy.App (.NET 8)
# Uses Alpine-based runtime/SDK for a smaller image (same app, no source changes).
# Build:  docker build -t stockscanner .
# Run:    docker run --rm -p 8080:8080 stockscanner
# Persist SQLite DB: docker run --rm -p 8080:8080 -v scanner-data:/app/Data stockscanner

FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

COPY src/NseBhavcopy.App/NseBhavcopy.App.csproj NseBhavcopy.App/
RUN dotnet restore NseBhavcopy.App/NseBhavcopy.App.csproj

COPY src/NseBhavcopy.App/ NseBhavcopy.App/
WORKDIR /src/NseBhavcopy.App
RUN dotnet publish -c Release -o /app/publish --no-restore \
    -p:DebugType=none \
    -p:DebugSymbols=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "NseBhavcopy.App.dll"]
