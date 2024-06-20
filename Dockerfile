# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build

WORKDIR /src

# the .csproj files are copied first so the dotnet restore only runs if either .csproj has changed
COPY ./src/cli/cli.csproj ./cli/
COPY ./src/ycs/ycs.csproj ./ycs/

# Will restore both projects as cli references Ycs
RUN dotnet restore ./cli/cli.csproj

# Copy the projects' source files, so only the dotnet publish image layer is invalidated if actual source changes
COPY ./src/cli ./cli/
COPY ./src/ycs ./ycs/

# copy and publish app and libraries
RUN dotnet publish ./cli/cli.csproj -o /app

# final stage/image
FROM mcr.microsoft.com/dotnet/runtime:7.0

# copy dotnet build result /app 
COPY --from=build /app /app

# environment variables passed into the container instance
ENV HOST=
ENV PORT=
ENV REMOTE_LIST=

# the executable built in the build container from our source in this build context
ENTRYPOINT /app/cli $HOST:$PORT $REMOTE_LIST
