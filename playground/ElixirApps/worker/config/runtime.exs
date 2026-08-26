import Config

# The OTLP exporter reads OTEL_EXPORTER_OTLP_ENDPOINT and the other OTEL_* variables that
# Aspire sets, so no endpoint belongs in this file.
config :opentelemetry, traces_exporter: :otlp
