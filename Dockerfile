# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /

RUN mkdir -p /src/poc
RUN mkdir -p /src/ycs

# the .csproj files are copied first so the dotnet restore only runs if either .csproj has changed
COPY ./src/poc/poc.csproj /src/poc/
COPY ./src/ycs/ycs.csproj /src/ycs/

# Will restore both projects as cli references Ycs
RUN dotnet restore --use-current-runtime "/src/poc/poc.csproj"

# Copy the projects' source files, so only the dotnet publish image layer is invalidated if actual source changes
COPY ./src ./src

RUN mkdir -p /app

# copy and publish the app, which does the same for ycs because it is a project reference and libraries
RUN dotnet publish -c Release -o /app --self-contained true --use-current-runtime --no-restore "/src/poc/poc.csproj"

# final stage/image
FROM mcr.microsoft.com/dotnet/runtime:7.0

# copy dotnet build result /app 
COPY --from=build /app /app

RUN ls -alh /

RUN ls -alh /app

# the executable built in the build container from our source in this build context
ENTRYPOINT [ "/app/poc" ]
