# Messaging Application

A real-time messaging web application built with **ASP.NET Core MVC + Web API**, featuring one-to-one and group chat (via SignalR), friend management, JWT-secured API endpoints (for the companion Flutter mobile client), and profile management with cloud-based (S3-compatible) picture storage.

## Tech Stack

- **Backend:** ASP.NET Core MVC (.NET 8)
- **Database:** PostgreSQL, via Entity Framework Core (`Npgsql.EntityFrameworkCore.PostgreSQL`)
- **Auth:** ASP.NET Core Identity (web/cookie-based) + JWT Bearer (API/mobile)
- **Realtime:** SignalR (`ChatHub`) for live chat and group messaging
- **Object storage:** S3-compatible storage (e.g. iDrive e2 / AWS S3) for profile pictures
- **Mobile client:** Flutter app, consuming the JWT-secured `Controllers/Api` endpoints

## Project Structure

```
Controllers/          MVC controllers (Account, Chat, Friend, Group, Profile)
Controllers/Api/       JWT-secured Web API controllers (used by the Flutter app)
Hubs/                  SignalR ChatHub
Services/              Business logic (Chat, Friend, Group, File/S3, JWT)
Models/Domain/         EF Core entities
Models/DTOs/           API request/response models
Models/ViewModels/     MVC view models
Views/                 Razor views (Account, Chat, Friend, Group, Profile, Shared)
Middleware/            Global exception handling
wwwroot/                Static assets (css, js, lib, uploads)
```

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [PostgreSQL](https://www.postgresql.org/download/) (local install or Docker)
- An S3-compatible bucket (AWS S3, iDrive e2, etc.) for profile picture storage

## Setup

1. **Clone and restore**
   ```bash
   git clone <repo-url>
   cd MessagingApp
   dotnet restore
   ```

2. **Configure the database connection**

   Edit `appsettings.json` (or better, use `dotnet user-secrets` for local dev) and set a PostgreSQL connection string:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Host=localhost;Port=5432;Database=MessagingAppDb;Username=postgres;Password=yourpassword"
   }
   ```

3. **Configure object storage (profile pictures)**

   ```json
   "ObjectStorage": {
     "ServiceUrl": "https://<your-endpoint>",
     "BucketName": "<your-bucket>",
     "AccessKey": "<your-access-key>",
     "SecretKey": "<your-secret-key>",
     "PublicBaseUrl": "https://<your-public-url>/<your-bucket>"
   }
   ```
   > Profile updates that don't include a new photo work fine even without this configured. Uploading/removing a photo requires valid credentials here.

4. **Configure JWT (used by the mobile API)**
   ```json
   "Jwt": {
     "Key": "<at least 32 characters>",
     "Issuer": "MessagingApp",
     "Audience": "MessagingAppUsers",
     "AccessTokenExpiryMinutes": 15,
     "RefreshTokenExpiryDays": 30
   }
   ```

5. **Apply database migrations**
   ```bash
   dotnet ef database update
   ```

6. **Run the app**
   ```bash
   dotnet run
   ```

## Features

- User registration/login (ASP.NET Core Identity) + JWT issuing for the mobile client
- Friend requests and friend list management
- One-to-one real-time chat (SignalR)
- Group chat with member management
- Profile view/edit — name, date of birth, gender, bio, profile picture
- Mobile-responsive Profile Edit page (form fields, avatar upload, and action buttons adapt down to small screens)

## Recent Fixes

- **Profile update 500 error:** the server was attempting to delete the existing profile picture from object storage on *every* profile save — even when no new photo was selected — because the file-upload check only tested for `null` instead of `null` **and** `Length > 0`. Combined with an `async void` delete method, this surfaced as an unhandled server error on save. Fixed by checking file length before treating the upload as present, and by converting the delete method to a properly awaited `async Task`.
- **Profile Edit mobile layout:** added/adjusted responsive rules (card padding, stacked action buttons, defensive `min-width`/`overflow` handling) so the Edit Profile page no longer overflows or feels cramped on small screens.

## Notes

- Deployment target is [Render](https://render.com) (the app reads a `PORT` environment variable at startup for this).
- The mobile app talks only to `Controllers/Api/*` (JWT-secured); the web app uses cookie-based Identity auth and traditional MVC form posts.