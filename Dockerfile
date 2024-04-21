# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /source/cli

# the .csproj files are copied first so the dotnet restore only runs if either .csproj has changed
COPY ./cli/cli.csproj ./cli/
COPY ./ycs/src/Ycs/Ycs.csproj ./ycs/src/Ycs/

# Will restore both projects as cli references Ycs
RUN dotnet restore --use-current-runtime "./cli/cli.csproj"

# Copy the projects' source files, so only the dotnet publish image layer is invalidated if actual source changes
COPY . .

# copy and publish app and libraries
RUN dotnet publish -c Release -o /app --self-contained true --use-current-runtime --no-restore "./cli/cli.csproj"

# final stage/image
FROM mcr.microsoft.com/dotnet/runtime:7.0

# copy dotnet build result /app 
COPY --from=build /app /app

# the executable built in the build container from our source in this build context
ENTRYPOINT [ "/app/cli" ]
