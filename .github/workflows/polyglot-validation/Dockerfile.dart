# Polyglot SDK Validation - Dart
# This Dockerfile sets up an environment for validating the Dart AppHost SDK
#
# Usage:
#   docker build -f Dockerfile.dart -t polyglot-dart .
#   docker run --rm \
#     -v "$(pwd):/workspace" \
#     -v /var/run/docker.sock:/var/run/docker.sock \
#     polyglot-dart
#
# Note: Expects self-extracting binary and NuGet artifacts to be pre-downloaded to /workspace/artifacts/
#
# The official Dart image ships a current Dart SDK on Debian, so this uses it as the base directly
# instead of adding a third-party apt repository to Ubuntu.
FROM dart:3.12

# Install system dependencies (wget, docker CLI, jq for JSON manipulation)
RUN apt-get update && apt-get install -y \
    wget \
    docker.io \
    jq \
    && rm -rf /var/lib/apt/lists/*

# Pre-configure Aspire CLI path
ENV PATH="/root/.aspire/bin:${PATH}"
ENV ASPIRE_CLI_TELEMETRY_OPTOUT=1

WORKDIR /workspace

COPY setup-local-cli.sh /scripts/setup-local-cli.sh
COPY test-dart.sh /scripts/test-dart.sh
RUN chmod +x /scripts/setup-local-cli.sh /scripts/test-dart.sh

# Entrypoint: Set up Aspire CLI and run validation
# Bundle extraction happens lazily on first command that needs the layout
ENTRYPOINT ["/bin/bash", "-c", "\
    set -e && \
    /scripts/setup-local-cli.sh && \
    aspire --nologo config set features:experimentalPolyglot:dart true --global && \
    echo '' && \
    echo '=== Running validation ===' && \
    /scripts/test-dart.sh \
"]
