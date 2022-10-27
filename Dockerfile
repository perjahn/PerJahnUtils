FROM mcr.microsoft.com/dotnet/nightly/sdk

WORKDIR /app

ENV DOTNET_CLI_TELEMETRY_OPTOUT=1
ENV POWERSHELL_TELEMETRY_OPTOUT=1

RUN apt-get update && \
    apt-get -y upgrade && \
    apt-get -y install jq p7zip-full clang

RUN releasesurl='https://api.github.com/repos/PowerShell/PowerShell/releases/latest' && \
    architecture=$(dpkg --print-architecture) && \
    if [ $architecture = "amd64" ]; then architecture='x64'; fi && \
    jqpattern='.assets[] | select(.name|test("powershell-[0-9\\.]+-linux-'$architecture'\\.tar\\.gz")) | .browser_download_url' && \
    asseturl=$(curl -s "$releasesurl" | jq "$jqpattern" -r) && \
    filename='/tmp/powershell.tar.gz' && \
    echo "Downloading: '$asseturl' -> '$filename'" && \
    curl -Ls "$asseturl" -o $filename && \
    mkdir -p /opt/microsoft/powershell/7 && \
    tar zxf /tmp/powershell.tar.gz -C /opt/microsoft/powershell/7 && \
    rm $filename && \
    chmod +x /opt/microsoft/powershell/7/pwsh && \
    ln -s /opt/microsoft/powershell/7/pwsh /usr/bin/pwsh

COPY . .

RUN ./build.ps1

RUN ls -la
