# The Serverpod template writes this file. The Aspire playground keeps the server as a
# standalone package, not as a pub workspace member, so the build context is this directory.
#
# Build with:
#   docker build -t serverpod-api .
#
# Aspire keeps an authored Dockerfile. `aspire publish` therefore uses this file and writes
# no Dockerfile of its own for the `api` resource.

# Build stage
FROM dart:3.8.0 AS build

WORKDIR /app

# Resolve the dependencies first, so a source change does not invalidate the pub cache layer.
COPY pubspec.yaml pubspec.lock ./
RUN dart pub get

COPY . .
RUN dart pub get --offline

# Compile the server executable.
RUN dart compile exe bin/main.dart -o bin/server

# Add a fallback for the copy of possibly missing directories.
RUN mkdir -p config web migrations

# Final stage
FROM alpine:latest
WORKDIR /app

# Environment variables
ENV runmode=production
ENV serverid=default
ENV logging=normal
ENV role=monolith

# Copy runtime dependencies
COPY --from=build /runtime/ /

# Copy compiled server executable
COPY --from=build /app/bin/server server

# Copy configuration files and resources
COPY --from=build /app/config/ config/
COPY --from=build /app/web/ web/
COPY --from=build /app/migrations/ migrations/

# This file is required to enable the endpoint log filter in Insights.
COPY --from=build /app/lib/src/generated/protocol.yaml lib/src/generated/protocol.yaml

# Expose ports
EXPOSE 8080
EXPOSE 8081
EXPOSE 8082

# Define the entrypoint command
ENTRYPOINT ./server --mode=$runmode --server-id=$serverid --logging=$logging --role=$role
