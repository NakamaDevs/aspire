#!/usr/bin/env bash
# Build a local Aspire release from this checkout and link it into mise.
#
# The release is a portable layout produced by ./localhive.sh:
#   <output>/bin/aspire                      native AOT CLI with the embedded bundle
#   <output>/bin/.aspire-install.json        identity sidecar (channel, version, packages)
#   <output>/hives/local/packages/*.nupkg    every Aspire package from this build
#
# The sidecar `packages` field points the CLI at the packages directory, so the
# CLI resolves Aspire.Hosting.* (including Aspire.Hosting.Elixir) from this build
# and never from nuget.org. `mise link` then exposes the layout as
# `aspire@<version>` so a project can pin it in mise.toml.
#
# Usage:
#   eng/scripts/mise-local-release.sh [--suffix <id>] [--output <dir>] [--link-only]
#
#   --suffix <id>    Prerelease suffix. Default: local.<yyyymmdd>.t<hhmmss>.
#                    The release version becomes <VersionPrefix>-<suffix>.
#   --output <dir>   Layout directory. Default: ~/.aspire/local-releases/aspire-<version>.
#   --link-only      Skip the build. Rewrite the sidecar and link an existing layout.
#   --no-link        Build the layout but do not run `mise link`.
#   --keep-symbols   Skip symbol stripping (no dsymutil). Use when dsymutil fails.
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
suffix=""
output=""
link_only=0
do_link=1
keep_symbols=0

while [[ $# -gt 0 ]]; do
  case "$1" in
    --suffix) suffix="$2"; shift 2 ;;
    --output) output="$2"; shift 2 ;;
    --link-only) link_only=1; shift ;;
    --no-link) do_link=0; shift ;;
    --keep-symbols) keep_symbols=1; shift ;;
    -h|--help) sed -n 2,22p "${BASH_SOURCE[0]}"; exit 0 ;;
    *) echo "Unknown argument: $1" >&2; exit 1 ;;
  esac
done

read_prop() { sed -nE "s|.*<$1>([^<]+)</$1>.*|\1|p" "$repo_root/eng/Versions.props" | head -1; }
version_prefix="$(read_prop MajorVersion).$(read_prop MinorVersion).$(read_prop PatchVersion)"
[[ "$version_prefix" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]] || { echo "Cannot read Major/Minor/PatchVersion from eng/Versions.props (got '$version_prefix')" >&2; exit 1; }
[[ -n "$suffix" ]] || suffix="local.$(date -u +%Y%m%d).t$(date -u +%H%M%S)"
version="$version_prefix-$suffix"
[[ -n "$output" ]] || output="$HOME/.aspire/local-releases/aspire-$version"

# localhive.sh calls `dotnet` by name; use the repo-local SDK from ./restore.sh.
[[ -x "$repo_root/.dotnet/dotnet" ]] || { echo "Run ./restore.sh first: $repo_root/.dotnet/dotnet not found" >&2; exit 1; }
export DOTNET_ROOT="$repo_root/.dotnet"
export PATH="$DOTNET_ROOT:$PATH"

if [[ "$(uname -s)" == "Darwin" ]]; then
  # Native AOT runs `dsymutil` and `strip` from PATH. A Homebrew LLVM dsymutil ahead of
  # Apple's rejects the DWARF that ILCompiler emits ("input verification failed").
  export PATH="/usr/bin:$PATH"
fi
if [[ $keep_symbols -eq 1 ]]; then
  # MSBuild reads environment variables as properties; the CLI project does not set this one.
  export StripSymbols=false
fi

if [[ $link_only -eq 0 ]]; then
  # Build into a staging directory. The previous layout stays in place until the
  # build succeeds, so a failed build never removes a working release.
  staging="$output.build"
  rm -rf "$staging" "$staging.tar.gz"
  (cd "$repo_root" && ./localhive.sh -c Release -o "$staging" -v "$suffix" --archive --native-aot)
  rm -rf "$output" "$output.tar.gz"
  mv "$staging" "$output"
  [[ -f "$staging.tar.gz" ]] && mv "$staging.tar.gz" "$output.tar.gz"
fi

bin_dir="$output/bin"
packages_dir="$output/hives/local/packages"
[[ -x "$bin_dir/aspire" ]] || { echo "CLI not found at $bin_dir/aspire" >&2; exit 1; }
[[ -d "$packages_dir" ]] || { echo "Packages not found at $packages_dir" >&2; exit 1; }

# The portable hive is outside ~/.aspire/hives, so the CLI only finds it through
# the sidecar. Keep the localhive `source` value and add the identity fields.
printf '{"source":"localhive","channel":"local","version":"%s","packages":"%s"}\n' \
  "$version" "$packages_dir" > "$bin_dir/.aspire-install.json"

echo "Release: $version"
echo "Layout:  $output"
echo "CLI:     $bin_dir/aspire"
echo "Feed:    $packages_dir ($(ls "$packages_dir"/*.nupkg | wc -l | tr -d ' ') packages)"

if [[ $do_link -eq 1 ]]; then
  mise link --force "aspire@$version" "$output"
  echo "Linked:  mise link aspire@$version"
  echo
  echo "Pin it in a project:"
  echo "  mise use aspire@$version"
  echo "Then in the AppHost directory:"
  echo "  aspire config set channel local"
  echo "  aspire config set sdk:version $version"
fi
