# Dana Cotech

Full-stack foundation for Dana Gharaibeh's dynamic career learning platform.

## Stack

- Frontend: React, Vite, MUI, Axios, i18next with English/Arabic and RTL support
- Backend: .NET 9 Web API, Swagger, layered architecture, `ServiceResponse<T>`
- Data: SQL Server through ADO.NET (`Microsoft.Data.SqlClient`)

## SQL Server

Run the scripts in order:

```text
database/001_landing_pages.sql
database/002_assets.sql
database/003_seed_landing_pages.sql
database/004_seed_assets_example.sql
database/005_client_journey.sql
database/006_user_roles.sql
database/007_client_journey_operations.sql
database/008_password_resets.sql
database/009_payment_transactions.sql
database/010_email_verification.sql
database/011_assets_defaults.sql
database/012_meps_jod_currency.sql
database/013_notification_outbox_delivery.sql
```

Then set the connection string in `src/backend/DanaCotech.Api/appsettings.json`:

```json
"ConnectionStrings": {
  "DanaCotechDb": "Server=.\\MSSQLSERVER01;Database=DanaCotechDb;Trusted_Connection=True;TrustServerCertificate=True"
}
```

If `DanaCotechDb` is empty, the API uses the in-memory fallback content for local development.

Assets are stored as raw base64 in `dbo.Assets.Base64Content`, without the `data:*/*;base64,` prefix.
The API endpoint `GET /api/assets/{assetKey}?lang=en` returns both `base64Content` and a ready-to-use `dataUrl`.

## Run Locally

Backend:

```powershell
dotnet run --project src/backend/DanaCotech.Api/DanaCotech.Api.csproj --launch-profile http
```

Frontend:

```powershell
cd src/frontend/dana-cotech-web
npm run dev
```

URLs:

- Frontend: `http://localhost:5173`
- API: `http://localhost:5115`
- Swagger: `http://localhost:5115/swagger`

## Homepage CMS

Open `http://localhost:5174/cms` to edit and publish every English or Arabic homepage section.
Publishing creates a new version in `dbo.LandingPages` and marks the previous language version as unpublished.
The **Theme & visibility** panel controls brand colors, corner radius, and show/hide switches for every homepage section.
The **Custom HTML sections** panel adds ordered, show/hide HTML blocks. HTML is rendered as trusted administrator content.

The CMS write endpoint is `PUT /api/content/landing/{lang}` and requires an authenticated `Admin` session.
There is no public registration. Administrators create `Client` or `Admin` accounts from the CMS.
For the one-time initial administrator, create an account before deploying this change or insert it using an approved provisioning process, then promote it explicitly:

```sql
UPDATE dbo.ClientAccounts SET Role = N'Admin' WHERE Email = N'admin@example.com';
```

Log in again after promotion so the new role is included in the signed session token.

## Password reset

The `/forgot-password` and `/reset-password` screens use one-time links that expire after 30 minutes.
Run `database/008_password_resets.sql` before using this feature. Reset emails are queued in
`dbo.NotificationOutbox` with the `password-reset` template key. Development exposes the generated
link in the API response for local testing; production does not.

## Email notifications and Google Calendar

Booking, enrollment, verification, password-reset, career-profile, and refund notifications are
queued in `dbo.NotificationOutbox`. The API worker delivers email over SMTP and creates Google
Calendar events for booked sessions. Calendar invitations are sent to the client; Meet links are
created when `GoogleCalendar:CreateMeetLink` is enabled and the configured calendar supports them.

Configure credentials through protected environment variables, never in source control:

- Email: `Email__Enabled`, `Email__SmtpHost`, `Email__SmtpPort`, `Email__EnableSsl`,
  `Email__Username`, `Email__Password`, `Email__FromAddress`, and `Email__FromName`.
- Google Calendar: `GoogleCalendar__Enabled`, `GoogleCalendar__CalendarId`,
  `GoogleCalendar__ClientId`, `GoogleCalendar__ClientSecret`, `GoogleCalendar__RefreshToken`,
  `GoogleCalendar__TimeZone`, and `GoogleCalendar__CreateMeetLink`.

For Gmail SMTP, use an app password for the sending account. For Google Calendar, enable the Calendar
API in a Google Cloud project and authorize the calendar owner with offline access to the
`https://www.googleapis.com/auth/calendar.events` scope. The worker refreshes the short-lived access
token using the saved refresh token. See the [Google OAuth refresh-token guide](https://developers.google.com/identity/protocols/oauth2/web-server),
[Calendar event insert reference](https://developers.google.com/workspace/calendar/api/v3/reference/events/insert),
and [Gmail app-password help](https://support.google.com/accounts/answer/185833).

Copy the corresponding values from `deploy/bawsala.env.example` into `/etc/bawsala/bawsala.env`
or the protected `PRODUCTION_APPSETTINGS_JSON` GitHub secret. The outbox uses bounded retries and
idempotent Calendar event IDs; run the migration scripts through `--migrate-database` before
starting the updated API.

## Client Journey

The integrated client MVP includes:

- `/programs` — program catalog
- `/login` — login and account creation
- `/checkout/{product-slug}` — test checkout and enrollment
- `/dashboard` — enrollments, progress, bookings, and career identity
- `/learn/{product-slug}` — cohort and library learning experience
- `/assessment/{key}` — strengths, interests, values, and purpose assessments
- `/booking` — coaching and cohort session booking

Run `database/005_client_journey.sql` before using account and journey APIs with SQL Server.
Checkout currently confirms a test enrollment. Production deployment still needs payment provider
credentials, SMTP and Google OAuth credentials, real video URLs, and authentication protection for `/cms`.

## Course CMS

Administrators manage courses from **CMS → Courses**. Each language version supports course pricing,
duration, cover media, publish state, display order, sections, and uploaded video lessons. Keep the same
course slug in English and Arabic. Published course content is stored with the versioned CMS data in SQL
Server and is used directly by the program catalog, checkout, client dashboard, and learning pages.
