# To build this Dockerfile, run the following command from the solution directory:
# docker build --file "Source\WebScheduler.Server\Dockerfile" --tag orleans .
# Or you can use the experimental buildx command for a better experience:
# docker buildx build --progress plain --file "Source\WebScheduler.Server\Dockerfile" --tag orleans .

# Base image used by Visual Studio at development time
# (See https://docs.microsoft.com/en-us/visualstudio/containers/container-msbuild-properties)
FROM mcr.microsoft.com/dotnet/aspnet:6.0.5-alpine3.15-amd64 AS base
# Open Container Initiative (OCI) labels (See https://github.com/opencontainers/image-spec/blob/master/annotations.md).
LABEL org.opencontainers.image.title="Web Scheduler Data Migrator" \
    org.opencontainers.image.description="Data Migrator for Web Scheduler" \
    org.opencontainers.image.documentation="https://github.com/web-scheduler/web-scheduler" \
    org.opencontainers.image.source="https://github.com/web-scheduler/web-scheduler.git" \
    org.opencontainers.image.url="https://github.com/web-scheduler/web-scheduler" \
    org.opencontainers.image.vendor="Elan Hasson"
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
RUN apk add --no-cache \
    # Install cultures to enable use of System.CultureInfo
    icu-libs \
    # Install time zone database to enable use of System.TimeZoneInfo
    tzdata
# Set the default locale and language.
ENV LC_ALL=en_US.UTF-8
ENV LANG=en_US.UTF-8
WORKDIR /app

COPY ./migrator/efbundle /app/efbundle
RUN apk add --no-cache tree && tree /app

# Runtime image used to run the application
FROM base AS runtime
WORKDIR /app
COPY --from=base /app .

ENTRYPOINT ["efbundle"]
