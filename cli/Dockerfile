# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /source

# the .csproj files are copied first, above, so they are in separate layers to the FS resulting from these dotnet restore RUNs
COPY ./cli/cli.csproj ./cli
COPY ./ycs/src/Ycs/Ycs.csproj ./ycs

# Now the source files will be in a separate layer to the .csproj files, which I am trusting MS's guidance will be more efficient
COPY . .
#RUN dotnet restore --use-current-runtime "./ycs/src/Ycs/Ycs.csproj"
RUN dotnet restore --use-current-runtime "./cli/cli.csproj"

# Copy Ycs.dll into cli project
#---#
# or if i use a <ProjectReference> to the Ycs.csproj, in cli.csproj, will dotnet figure it out?

# copy and publish app and libraries
# TODO: If having issues linking, --use-current-runtime may mean .NET 7 SDK, which if the Ycs.dll built OK,
# means you can change Ycs.csproj to net7.0 which would suit cli.csproj better (it's just newer)
RUN dotnet publish -c Release -o /app --use-current-runtime --self-contained --runtime linux-64 --no-restore "./cli/cli.csproj"

# final stage/image
FROM mcr.microsoft.com/dotnet/runtime:7.0
WORKDIR /app

# copy dotnet build result /app 
COPY --from=build /app .

# the executable built in the build container from our source in this build context
ENTRYPOINT ["cli.exe"]
