FROM mcr.microsoft.com/dotnet/nightly/sdk

WORKDIR /app

COPY . .

RUN ./build.ps1

RUN ls -la
