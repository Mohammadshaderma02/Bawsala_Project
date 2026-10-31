# Bawsala Ubuntu VPS Deployment

This deployment uses GitHub Actions, Nginx, systemd, SSH, and rsync. GitHub builds both applications, synchronizes the frontend to `/var/www/project/frontend/dist`, synchronizes the API to `/var/www/project/backend`, restarts `danacotech-backend`, and checks `/health`.

## 1 Server prerequisites

Install Nginx, rsync, curl, and the .NET 9 ASP.NET Core runtime on the Hostinger Ubuntu VPS. Create a non-root deployment user with SSH key access.

```bash
sudo mkdir -p /var/www/project/frontend/dist /var/www/project/backend /etc/bawsala
sudo chown -R YOUR_DEPLOY_USER:www-data /var/www/project
sudo chmod -R 775 /var/www/project
```

Copy `deploy/bawsala.env.example` to `/etc/bawsala/bawsala.env`, replace every placeholder, and protect it:

```bash
sudo chown root:www-data /etc/bawsala/bawsala.env
sudo chmod 640 /etc/bawsala/bawsala.env
```

To enable email, configure the Gmail sender's app password and verified From address. To enable
Calendar invitations, create a Google Cloud OAuth client, enable the Google Calendar API, and
authorize the calendar owner for offline access to `https://www.googleapis.com/auth/calendar.events`.
Store the resulting refresh token, client ID, and client secret in the protected environment file.
The worker refreshes access tokens automatically and sends attendees the Calendar event invitation.
See [Google's OAuth refresh-token guide](https://developers.google.com/identity/protocols/oauth2/web-server),
[Calendar event insert reference](https://developers.google.com/workspace/calendar/api/v3/reference/events/insert),
and [Gmail app-password help](https://support.google.com/accounts/answer/185833).

Copy and enable the systemd service:

```bash
sudo cp deploy/bawsala-api.service /etc/systemd/system/danacotech-backend.service
sudo systemctl daemon-reload
sudo systemctl enable danacotech-backend
```

Replace `YOUR_DOMAIN` in the Nginx template, install it, and validate it:

```bash
sudo cp deploy/bawsala.nginx.conf /etc/nginx/sites-available/bawsala
sudo ln -s /etc/nginx/sites-available/bawsala /etc/nginx/sites-enabled/bawsala
sudo nginx -t
sudo systemctl reload nginx
```

Use Certbot after DNS points to the VPS to enable HTTPS.

## 2 GitHub repository configuration

Create a private GitHub repository and add these Actions secrets under Settings, Secrets and variables, Actions:

- `VPS_HOST`: VPS IP address or hostname.
- `VPS_USER`: non-root deployment user.
- `VPS_SSH_PRIVATE_KEY`: private key matching the public key installed for the deployment user.
- `VPS_SSH_PORT`: optional, defaults to `22`.
- `PRODUCTION_APPSETTINGS_JSON`: the complete production `appsettings.json` document. It must include a SQL Server connection string at `ConnectionStrings:DanaCotechDb` and all production authentication, payment, email, and calendar secrets. For email and Calendar, use the keys documented in `deploy/bawsala.env.example` with double underscores converted to nested JSON properties.

Add these repository variables:

Protect the GitHub `production` environment if deployment approval is required.

The deployment user must be permitted to restart only the Bawsala service without a password. Configure this narrowly through `sudoers` rather than granting unrestricted sudo access.

## 3 Release flow

- Pull requests run a clean frontend and backend build.
- Pushes to `main` build the same verified artifact.
- The verified frontend artifact is synchronized to `/var/www/project/frontend/dist`.
- The verified backend artifact is synchronized to `/var/www/project/backend`.
- The production configuration is created from the protected GitHub secret after the build artifact is downloaded; it is never committed or uploaded as a build artifact.
- Numbered scripts from `database/` are applied transactionally in filename order. Successful scripts are recorded in `dbo.SchemaMigrations` and are not run twice.
- systemd restarts the API and the workflow checks the local health endpoint.

Real application secrets stay in GitHub Actions secrets and the protected server configuration; they never enter GitHub source control or uploaded build artifacts.

Large videos are deliberately excluded from the Git repository and deployment artifact. Manage them through the Admin media library or a dedicated object-storage/CDN service so normal code deployments remain fast.
