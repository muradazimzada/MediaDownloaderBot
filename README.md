
# Telegram Media Downloader Bot

## Overview
**Telegram Media Downloader Bot** is a .NET Core-based Telegram bot designed to download videos and convert them to audio on request. The bot utilizes PostgreSQL for database storage, EF Core for data management, and provides quick access to already downloaded files using a local file system. It leverages the Telegram Bot Library for interaction and ensures efficient responses by matching links to previously downloaded files.

## Features
- **Download Media**: Supports downloading videos and converting them to audio format.
- **Quick Access**: Quickly provides access to previously downloaded media using YouTube links, reducing redundant downloads.
- **File System Storage**: Stores downloaded files on the local file system for easy and fast access.
- **Database Integration**: Uses PostgreSQL to map links to files, enabling efficient retrieval of previously downloaded media.

## Technology Stack
- **Backend**: .NET Core
- **Database**: PostgreSQL
- **ORM**: Entity Framework Core (EF Core)
- **Telegram Bot Library**: For interaction with Telegram API
- **File System**: Local storage for media files

## Requirements
- [.NET Core SDK](https://dotnet.microsoft.com/download)
- [Docker & Docker Compose](https://www.docker.com/products/docker-desktop) (for containerized deployment)
- [PostgreSQL](https://www.postgresql.org/) (or run it in Docker)

## Setup

### Step 1: Clone the Repository
```bash
git clone https://github.com/muradazimzada/MediaDownloaderBot.git
cd TelegramMediaDownloaderBot
```

### Step 2: Configure Environment Variables
Set up necessary environment variables for database and Telegram bot configuration in a `.env` file or in Docker Compose:
- `TELEGRAM_BOT_TOKEN`: Your bot token from Telegram.
- `DB_HOST`: PostgreSQL database host.
- `DB_USER`: PostgreSQL user.
- `DB_PASSWORD`: PostgreSQL password.
- `DB_NAME`: Name of the PostgreSQL database.

### Step 3: Initialize Database
Run the following commands to set up the PostgreSQL database schema:
```bash
docker-compose up -d
```

This will start PostgreSQL in a Docker container and execute any necessary initialization scripts (e.g., `init.sql`).

### Step 4: Run the Bot
Navigate to the project directory and start the bot:

#### Using .NET CLI
```bash
dotnet run --project MediaDownloader
```

#### Using Docker Compose
```bash
docker-compose up --build
```

The bot will now be listening for Telegram messages and ready to download media.

## Usage
- **Download Video**: Send a YouTube link to the bot, and it will download the video.
- **Convert to Audio**: The bot can convert videos to audio files as requested.
- **Quick Access**: If a file is already downloaded, the bot retrieves it quickly from the local file system based on the URL.

## Directory Structure
```
.
├── docker-compose.yml          # Docker configuration for PostgreSQL and bot
├── init.sql                    # SQL file to initialize database
├── MediaDownloader.csproj      # Project file for the bot
├── Program.cs                  # Main application entry point
└── TelegramBotWebhook/         # Folder containing bot-specific webhook logic
```

## Troubleshooting
- Ensure your bot token is correct and that the database connection details are accurate.
- If encountering issues with PostgreSQL, check Docker logs:
  ```bash
  docker-compose logs db
  ```
- Ensure you’re using the correct version of .NET Core (as specified in the project).

## License
This project is licensed under the MIT License. See the LICENSE file for details.
