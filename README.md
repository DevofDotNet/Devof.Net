# Devof.NET - Multi-Author Blogging Platform

Devof.NET is a production-grade, open-source blogging platform built with **ASP.NET Core Razor Pages** and **Clean Architecture**. It features a modern, responsive UI, rich content editing, and comprehensive administration tools.

## 🚀 Features

- **User Accounts**: Authentication via Identity (Local) or OAuth (Google, GitHub).
- **Posting**: Markdown editor with live preview, image uploads, and cover images.
- **Engagement**: Comments, Likes, Bookmarks, and Follow capability.
- **Discovery**: Tagging system, Search, Trending posts, and algorithmic feed.
- **Profiles**: Public author profiles with bio, socials, and post history.
- **Administration**: 
  - Manage Users (Ban/Unban)
  - Manage Posts (Publish/Unpublish)
  - Dashboard Metrics
- **Performance**: Pagination, Optimized SQL queries, and Rate Limiting.
- **SEO**: Dynamic Sitemap, Robots.txt, and Meta tags.

## 🛠️ Technology Stack

- **Framework**: .NET 8 / .NET 10 (Preview)
- **Architecture**: Clean Architecture (Domain, Application, Infrastructure, Web)
- **Database**: MySQL 8.0 (via `Pomelo.EntityFrameworkCore.MySql`)
- **Frontend**: Razor Pages, Vanilla CSS (Variables & Dark Mode), Vanilla JS
- **Libraries**:
  - `Markdig` (Markdown)
  - `FluentValidation`
  - `ASP.NET Core Identity`

## ⚙️ prerequisites

- .NET SDK (8.0 or later)
- MySQL Server (8.0 or MariaDB)
- Git

## 🏁 Getting Started

1. **Clone the repository**
   ```bash
   git clone https://github.com/yourusername/DFevofNet.git
   cd DFevofNet
   ```

2. **Configure Database**
   Update `src/Blog.Web/appsettings.json` with your MySQL connection string:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=localhost;Port=3306;Database=DevofNET;User=root;Password=password;CharSet=utf8mb4"
   }
   ```

3. **Run the Application**
   The application will automatically apply migrations and seed default data on startup.
   ```bash
   dotnet run --project src/Blog.Web
   ```

4. **Access the Site**
   Open [http://localhost:5000](http://localhost:5000) in your browser.

## 🔑 Default Credentials

A default Administrator account is created automatically:
- **Email**: `admin@devof.net`
- **Password**: `Admin@123`

## 🧪 Project Structure

```
src/
├── Blog.Domain/          # Entities, Interfaces, Enums (Core)
├── Blog.Application/     # Services, DTOs, Validators (Logic)
├── Blog.Infrastructure/  # EF Core, Repositories, External Services (Data)
└── Blog.Web/             # Razor Pages, API, WWWRoot (UI)
```

## 🔒 Security Notes

- **OAuth**: Google/GitHub Client IDs must be configured in `appsettings.json` or User Secrets.
- **Secrets**: Never commit real database passwords or API keys to version control.

## 📄 License

MIT License
