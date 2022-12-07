The Dockerfile.* files in this directory came as part of the dotnet-docker project template that was used.
As they were cluttering up the directory, and were suspected that they weren't actually in use or necessary, they've been moved here.
As far as I can tell (I haven't looked to hard), it really just appends the same variant that suffixes each Dockerfile's filename (e.g. ubunutu, windowsserver, ...) to the image names/tags specified inside the Dockerfile.
I think some may have slightly varying options to the dotnet executable as well (e.g. --self-contained false/true, etc)
