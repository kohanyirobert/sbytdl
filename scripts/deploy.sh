#!/usr/bin/env bash
set -euo pipefail

: "${DEPLOY_HOST:?missing DEPLOY_HOST}"
: "${DEPLOY_USER:?missing DEPLOY_USER}"
: "${DEPLOY_TARGET_DIR:?missing DEPLOY_TARGET_DIR}"

DEPLOY_PORT="${DEPLOY_PORT:-}"

rsync -az --delete backend/publish/ "${DEPLOY_USER}@${DEPLOY_HOST}:${DEPLOY_TARGET_DIR}/backend/"

ssh "${DEPLOY_USER}@${DEPLOY_HOST}" \
  DEPLOY_TARGET_DIR="${DEPLOY_TARGET_DIR}" DEPLOY_PORT="${DEPLOY_PORT}" 'bash -s' <<'REMOTE'
set -euo pipefail

TARGET_DIR="${DEPLOY_TARGET_DIR}"
PORT="${DEPLOY_PORT}"

if [ -z "${PORT}" ]; then
  PORT=$(python3 - <<'PY'
import socket
for port in range(18080, 18999):
    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
        try:
            s.bind(('127.0.0.1', port))
        except OSError:
            continue
        print(port)
        break
PY
)
fi

mkdir -p "${HOME}/.config/systemd/user"
cat > "${HOME}/.config/systemd/user/sbytdl.service" <<SERVICE
[Unit]
Description=sbytdl backend
After=network.target

[Service]
WorkingDirectory=${TARGET_DIR}/backend
ExecStart=${TARGET_DIR}/backend/Sbytdl.Api
Restart=always
Environment=ASPNETCORE_URLS=http://127.0.0.1:${PORT}

[Install]
WantedBy=default.target
SERVICE

systemctl --user daemon-reload
systemctl --user enable --now sbytdl.service
systemctl --user restart sbytdl.service

echo "SBYTDL_PORT=${PORT}" > "${TARGET_DIR}/.deploy-info"
printf 'Deployed and running at http://127.0.0.1:%s\n' "${PORT}"
REMOTE
