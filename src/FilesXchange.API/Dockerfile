FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

COPY src/FilesXchange.API/FilesXchange.API.csproj src/FilesXchange.API/
RUN dotnet restore src/FilesXchange.API/FilesXchange.API.csproj

COPY src/FilesXchange.API/ src/FilesXchange.API/
WORKDIR /source/src/FilesXchange.API
RUN dotnet publish FilesXchange.API.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends wget \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS=http://+:8080 \
    FilesXchange__UploadDirectory=/app/uploads \
    FilesXchange__LogDirectory=/app/logs \
    ConnectionStrings__Default="Data Source=/app/data/filesxchange.db"

RUN mkdir -p /app/uploads /app/logs /app/data

EXPOSE 8080
COPY --from=build /app/publish ./

ENTRYPOINT ["dotnet", "FilesXchange.API.dll"]
