FROM mcr.microsoft.com/dotnet/sdk

WORKDIR /app

ENV DOTNET_CLI_TELEMETRY_OPTOUT=1
ENV POWERSHELL_TELEMETRY_OPTOUT=1

RUN apt-get update && \
    apt-get -y upgrade && \
    apt-get -y install jq p7zip-full clang

COPY . .

RUN ./build.ps1

RUN ls -la
