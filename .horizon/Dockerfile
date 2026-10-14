# syntax=docker/dockerfile:1.7
FROM ghcr.io/peters/horizon-worker-base@sha256:5d831ab73cc11575080ededaca851480fc9539c13916fd9c19e36903962526d8
USER root
SHELL ["/bin/bash", "-o", "pipefail", "-c"]

# CI uses ubuntu-latest, pwsh and DOTNET_100_SDK_VERSION=10.0.402.
ARG DOTNET_VERSION=10.0.402
ARG POWERSHELL_VERSION=7.6.6
ARG GITVERSION_VERSION=6.4.0
ARG MONGODB_VERSION=8.0.32
ARG LOCALCI_VERSION=0.1.0
ENV DOTNET_ROOT=/opt/dotnet \
    DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

RUN --mount=type=cache,target=/var/cache/apt,sharing=locked \
    --mount=type=cache,target=/var/lib/apt/lists,sharing=locked \
    apt-get update && apt-get install -y --no-install-recommends \
      ca-certificates curl libicu74 libssl3t64 libunwind8 libgssapi-krb5-2 \
      liblttng-ust1t64 libcurl4t64 zlib1g util-linux

RUN curl -fsSL "https://builds.dotnet.microsoft.com/dotnet/Sdk/${DOTNET_VERSION}/dotnet-sdk-${DOTNET_VERSION}-linux-x64.tar.gz" -o /tmp/dotnet.tar.gz \
    && echo '1904a98dda2db34ba78ba3c4fb1b224455329af91c341d83547843c1839e112b04ae699ee92fa2449a8f62249196a938723078c0c60b9f65c88539d5687729c2  /tmp/dotnet.tar.gz' | sha512sum -c - \
    && mkdir -p /opt/dotnet \
    && tar -xzf /tmp/dotnet.tar.gz -C /opt/dotnet \
    && rm /tmp/dotnet.tar.gz

RUN curl -fsSL "https://github.com/PowerShell/PowerShell/releases/download/v${POWERSHELL_VERSION}/powershell-${POWERSHELL_VERSION}-linux-x64.tar.gz" -o /tmp/powershell.tar.gz \
    && echo 'ddbc4a2d113bbd46d283cfedcbcd117a70caefd7673f41f2b4e0000badf103bc  /tmp/powershell.tar.gz' | sha256sum -c - \
    && mkdir -p /opt/powershell \
    && tar -xzf /tmp/powershell.tar.gz -C /opt/powershell \
    && chmod 755 /opt/powershell/pwsh \
    && rm /tmp/powershell.tar.gz

# The package targets net8/net9 and declares rollForward=LatestMajor.
# CI still updates the user-global tool and computes BUILD_VERSION itself.
RUN --mount=type=cache,target=/root/.nuget/packages \
    /opt/dotnet/dotnet tool install GitVersion.Tool --tool-path /opt/gitversion \
      --version "${GITVERSION_VERSION}" --allow-roll-forward

# Replace the skipped Docker-based MongoDB action with its native Ubuntu server.
RUN curl -fsSL "https://repo.mongodb.org/apt/ubuntu/dists/noble/mongodb-org/8.0/multiverse/binary-amd64/mongodb-org-server_${MONGODB_VERSION}_amd64.deb" -o /tmp/mongodb.deb \
    && echo 'a6f0d74f63fa232a8cada49580368f4c29022d6b13ec157a7a816aaa8ce4203b  /tmp/mongodb.deb' | sha256sum -c - \
    && (printf '#!/bin/sh\nexit 101\n' > /usr/sbin/policy-rc.d) \
    && chmod 755 /usr/sbin/policy-rc.d \
    && dpkg -i /tmp/mongodb.deb \
    && rm /tmp/mongodb.deb /usr/sbin/policy-rc.d

RUN mkdir -p /opt/localci \
    && curl -fsSL "https://github.com/peters/localci/releases/download/v${LOCALCI_VERSION}/localci-x86_64-unknown-linux-musl.tar.gz" -o /tmp/localci-x86_64-unknown-linux-musl.tar.gz \
    && curl -fsSL "https://github.com/peters/localci/releases/download/v${LOCALCI_VERSION}/SHA256SUMS" -o /tmp/localci-SHA256SUMS \
    && echo 'b1c6f682da8598e33c4a712623f113b1df9ef284772b5a0d40ce4b2d764daea9  /tmp/localci-SHA256SUMS' | sha256sum -c - \
    && (cd /tmp && grep ' localci-x86_64-unknown-linux-musl.tar.gz$' localci-SHA256SUMS | sha256sum -c -) \
    && tar -xzf /tmp/localci-x86_64-unknown-linux-musl.tar.gz -C /opt/localci \
    && chmod 755 /opt/localci/localci \
    && rm /tmp/localci-x86_64-unknown-linux-musl.tar.gz /tmp/localci-SHA256SUMS

COPY localci horizon-mongomigrations-mongodb /usr/local/bin/
RUN chmod 755 /usr/local/bin/localci /usr/local/bin/horizon-mongomigrations-mongodb \
    && mkdir -p /etc/horizon-worker/session-env.d \
    && printf '%s\n' \
      'PATH_PREPEND=/opt/dotnet' \
      'PATH_PREPEND=/opt/powershell' \
      'PATH_PREPEND=/opt/gitversion' \
      'PATH_PREPEND=/usr/local/bin' \
      'DOTNET_ROOT=/opt/dotnet' \
      'DOTNET_CLI_TELEMETRY_OPTOUT=1' \
      'DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1' \
      > /etc/horizon-worker/session-env.d/20-mongomigrations.core.env
