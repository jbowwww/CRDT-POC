# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /source/cli

# the .csproj files are copied first, alone, so they and the result of "dotnet restore" exist on their own separate FS layer
COPY ./cli/cli.csproj ./cli/
COPY ./ycs/src/Ycs/Ycs.csproj ./ycs/src/Ycs/

# Will restore both projects as cli references Ycs
RUN dotnet restore --use-current-runtime "./cli/cli.csproj"

# Copy source files. Allows use of cached image layer built above to be used if only source has changed, avoiding unnecessary dotnet restore
COPY . .

# copy and publish app and libraries - TODO: change /app (here and below) to ./app and test - then doesn't matter if running docker build on system that probably can't write to /app
RUN dotnet publish -c Release -o ./app --self-contained true --use-current-runtime --no-restore "./cli/cli.csproj"

# final stage/image
FROM mcr.microsoft.com/dotnet/runtime:7.0
WORKDIR /app

# copy dotnet build result /app 
COPY --from=build ./app .

# the executable built in the build container from our source in this build context
ENTRYPOINT [ "/app/cli" ]

