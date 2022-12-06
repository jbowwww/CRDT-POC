# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /source

# copy csproj and restore as distinct layers
COPY . .
RUN dotnet restore --use-current-runtime "./cli.csproj"

# copy and publish app and libraries
RUN dotnet publish -c Release -o /app --use-current-runtime --self-contained --runtime linux-64 --no-restore

# final stage/image
FROM mcr.microsoft.com/dotnet/runtime:7.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["cli.exe"]
