# Application & Environment Lifecycle Manager

## Smoke Test Harness

The repository includes an endpoint smoke test script at `scripts/api-smoke-test.sh`.

### Prerequisites

- `.NET SDK 9`
- `curl`
- `jq` (for JSON assertions in the script)

Install `jq` on Ubuntu/Debian:

```bash
sudo apt-get update
sudo apt-get install -y jq
```

### Run Smoke Test

```bash
cd /home/jorge/projects/release-pilot
./scripts/api-smoke-test.sh
```

Notes:

- The script auto-starts the API if it is not running and stops it after completion if it started it.
- If `jq` is missing, the script attempts an automatic install when root or passwordless sudo is available.

