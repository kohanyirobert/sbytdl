# sbytdl

Angular + ASP.NET app for downloading videos on your server with `yt-dlp`.

## Features

- Paste one or multiple URLs
- Configure output directory (and create it if missing)
- Separate single-file name and multi-file template (`{index}` token)
- Overwrite conflict preview before running
- Real-time progress polling
- Update `yt-dlp` from frontend
- Built for Linux VPS/seedbox user-space deployment

## Local development

### Backend

```bash
cd backend
dotnet run
```

### Frontend

```bash
cd frontend
npm install
npm start
```

Set `apiBase` in `frontend/src/app/app.component.ts` if your backend runs elsewhere.

## Production deploy (user-space)

The deploy script publishes backend + frontend, syncs to your VPS, then installs/updates a user-level `systemd` service.

### Required prerequisites on VPS

- `yt-dlp` available in PATH for your user
- user-level `systemd` enabled (`systemctl --user` works)
- `python3` installed (used to auto-pick an available port)

### GitHub Actions secrets

Create these repo secrets:

- `DEPLOY_HOST` (example: `seedbox.example.com`)
- `DEPLOY_USER` (example: `alice`)
- `DEPLOY_SSH_KEY` (private key content)
- `DEPLOY_TARGET_DIR` (example: `/home/alice/apps/sbytdl`)

Using GitHub CLI:

```bash
gh secret set DEPLOY_HOST --body "seedbox.example.com"
gh secret set DEPLOY_USER --body "alice"
gh secret set DEPLOY_TARGET_DIR --body "/home/alice/apps/sbytdl"
gh secret set DEPLOY_SSH_KEY < ~/.ssh/id_ed25519
```

### Trigger deploy

- Push to `main`, or
- Run **Actions → build-and-deploy → Run workflow**.

Optional input:

- `deploy_port`: set a fixed backend port.
- If empty, deployment auto-selects an open local port in range `18080-18999`.

Deployment writes selected port to `${DEPLOY_TARGET_DIR}/.deploy-info` on the VPS.

### Service behavior

Deployment maintains this user-level service:

- `~/.config/systemd/user/sbytdl.service`
- starts backend at `http://127.0.0.1:<selected_port>`

Useful commands on VPS:

```bash
systemctl --user status sbytdl.service
systemctl --user restart sbytdl.service
cat /home/<user>/apps/sbytdl/.deploy-info
```
