# Civilization-lite

Turn-based hex strategy prototype built in **Unity 6** (URP 2D). Play locally with JSON saves, or optionally sync saves to **MySQL** through a small Node.js API.

## Requirements

| Tool | Version | Used for |
|------|---------|----------|
| [Unity](https://unity.com/download) | **6000.3.x** (Unity 6) | Game client |
| [Node.js](https://nodejs.org/) | **18+** (LTS recommended) | Cloud save API |
| [MySQL](https://dev.mysql.com/downloads/) | **8.0+** | Cloud save storage |

Cloud saves are **optional**. The game runs fine with local saves only.

---

## Quick start (play without MySQL)

1. Clone this repository.
2. Open the project in **Unity Hub** → **Add** → select the repo folder.
3. Open scene **`Assets/MainMenu/Scenes/NewMenu.unity`** (or press Play — it is the first scene in Build Settings).
4. From the main menu:
   - **New game** — pick a civilization (Rome, America, Egypt, Scythia).
   - **Load game** — choose one of 3 local save slots (if a save exists).

**Build order (already configured):**

1. `Assets/MainMenu/Scenes/NewMenu.unity` — main menu  
2. `Assets/Gamescena/Gamescena.unity` — gameplay  

---

## Controls

Full hints are in **Settings → Controls** (main menu) or **Esc → Налаштування → Керування** (in-game).

| Input | Action |
|-------|--------|
| **WASD** / **Arrow keys** | Move camera |
| **Mouse wheel** | Zoom |
| **LMB** on unit | Select unit |
| **LMB** on hex | Move selected unit |
| **LMB** on enemy | Attack |
| **LMB** on city | Open city panel |
| **B** | Found city (settler, when movement is spent) |
| **Esc** | Pause menu (save / load / settings) |
| **Next turn** button | End your turn |

---

## Local saves

- **3 slots** per machine: `slot1`, `slot2`, `slot3`.
- Files are stored under Unity’s persistent data path, for example:
  - **Windows:** `%USERPROFILE%\AppData\LocalLow\<CompanyName>\<ProductName>\`
  - Filenames: `civilization_save_slot1.json`, `civilization_save_slot2.json`, …
- Legacy single file `civilization_save.json` is still read for slot 1 if present.
- In-game: **Esc → Зберегти** / **Завантажити**.
- Main menu: **Load game** lists slots with turn and civilization.

Cloud and local saves are merged on load: the copy with the **higher turn number** wins.

---

## MySQL cloud saves — overview

The Unity game **never connects to MySQL directly** (that would expose database credentials in the client). Instead:

```
Unity  →  HTTP REST API (Node.js)  →  MySQL
```

Default API URL: `http://localhost:3000`

See also [`server/README.md`](server/README.md) for API endpoint details.

---

## MySQL setup tutorial

### 1. Install MySQL

**Windows**

1. Download [MySQL Installer](https://dev.mysql.com/downloads/installer/) (MySQL Server 8.x).
2. Run installer → choose **Server only** or **Developer Default**.
3. Set a **root password** and remember it.
4. Finish installation; note that the service runs on port **3306** by default.

**macOS (Homebrew)**

```bash
brew install mysql
brew services start mysql
mysql_secure_installation
```

**Linux (Debian/Ubuntu)**

```bash
sudo apt update
sudo apt install mysql-server
sudo mysql_secure_installation
```

### 2. Create database and tables

**Option A — command line**

From the repo root:

```bash
mysql -u root -p < server/schema.sql
```

Enter your MySQL root password when prompted.

**Option B — MySQL Workbench**

1. Connect to local MySQL.
2. **File → Open SQL Script** → select `server/schema.sql`.
3. Execute the script.

This creates:

- Database: `civilization_lite`
- Table `players` — one row per device/player ID
- Table `game_saves` — JSON save data per player and slot

### 3. Create a dedicated MySQL user (recommended)

Log into MySQL as root:

```sql
CREATE USER 'civlite'@'localhost' IDENTIFIED BY 'your_strong_password';
GRANT ALL PRIVILEGES ON civilization_lite.* TO 'civlite'@'localhost';
FLUSH PRIVILEGES;
```

Use this user in the API `.env` file instead of `root`.

### 4. Verify MySQL

```bash
mysql -u civlite -p civilization_lite -e "SHOW TABLES;"
```

Expected output includes `players` and `game_saves`.

---

## Cloud save API setup

### 1. Install dependencies

**Windows (PowerShell):**

```powershell
cd server
Copy-Item .env.example .env
npm install
```

**macOS / Linux:**

```bash
cd server
cp .env.example .env
npm install
```

### 2. Configure environment

Edit `server/.env`:

```env
PORT=3000

MYSQL_HOST=127.0.0.1
MYSQL_PORT=3306
MYSQL_USER=civlite
MYSQL_PASSWORD=your_strong_password
MYSQL_DATABASE=civilization_lite
```

### 3. Start the server

```bash
npm start
```

For development with auto-restart on file changes:

```bash
npm run dev
```

You should see:

```text
Civilization-lite API listening on http://localhost:3000
```

### 4. Health check

Open in a browser or run:

```bash
curl http://localhost:3000/health
```

Success:

```json
{ "ok": true, "service": "civilization-lite-api" }
```

If `ok` is `false`, check MySQL credentials, that the service is running, and that `schema.sql` was applied.

---

## Enable cloud save in Unity

1. Start the API server (`npm start` in `server/`).
2. Play the game from Unity or a build.
3. Open **Esc → Налаштування** (in-game settings).
4. Turn **Хмарне збереження (MySQL)** **on**.
5. Click **Перевірити підключення** — status should report a successful connection.
6. Use **Esc → Зберегти** / **Завантажити** to sync slots with the server.

**Settings stored in PlayerPrefs:**

| Key | Purpose |
|-----|---------|
| `CloudSave_Enabled` | 0/1 — cloud save on/off |
| `CloudSave_ApiUrl` | API base URL (default `http://localhost:3000`) |
| `CloudSave_PlayerId` | Player/device ID (auto-generated if empty) |

Each installation gets a unique **Player ID** (device identifier). Saves on the server are keyed by that ID and slot name (`slot1`–`slot3`).

### Built game / another PC

- Run the API somewhere reachable (same LAN or hosted server).
- Set **API URL** in game settings to e.g. `http://192.168.1.10:3000` or your public URL.
- Use the same **Player ID** only if you intentionally copy PlayerPrefs; otherwise each device has its own cloud profile.

---

## Troubleshooting

### Cloud save

| Problem | What to check |
|---------|----------------|
| **Перевірити підключення** fails | API running? `curl http://localhost:3000/health`. Firewall blocking port 3000? |
| Save upload fails | MySQL user/password in `.env`. Database and tables exist. Unity console / API terminal for errors. |
| Load shows empty cloud slot | Wrong Player ID, or slot never uploaded with cloud enabled. |
| `ECONNREFUSED` | Start API with `npm start` in `server/`. |

### MySQL

| Problem | What to check |
|---------|----------------|
| Access denied for user | User created? Password matches `.env`? `GRANT` on `civilization_lite.*`? |
| Unknown database | Run `server/schema.sql` again. |
| Port in use | Change `PORT` in `.env` and update Unity API URL. |

### Game

| Problem | What to check |
|---------|----------------|
| Black screen / no menu | Open **NewMenu** scene; confirm both scenes are in **File → Build Profiles / Build Settings**. |
| No load button | No local or cloud save yet — start a new game and save once. |
| Lag when selecting units | Ensure latest code (path preview and hex distance fixes). |

---

## Project structure

```text
Civilization-lite/
├── Assets/
│   ├── MainMenu/          Main menu UI and NewMenu scene
│   ├── Gamescena/         Gameplay scene
│   └── 8Set/script/       Core game logic (map, turns, combat, saves, AI)
├── server/
│   ├── src/index.js       Express REST API
│   ├── schema.sql         MySQL schema
│   ├── .env.example       API configuration template
│   └── README.md          API reference
└── README.md              This file
```

**Main scripts:**

| Area | Files |
|------|--------|
| Map & turns | `Program1.cs`, `TurnManager.cs` |
| Saves | `SaveManager.cs`, `CloudSaveClient.cs`, `GameSaveData.cs` |
| Diplomacy / AI | `DiplomacyManager.cs`, `CivilizationAI.cs` |
| UI | `GameUI.cs`, `CivMainMenuUI.cs`, `InGameSettingsPanel.cs` |
| Fog of war | `FogOfWarManager.cs` |

---

## Development notes

- **Unity version:** `6000.3.5f2` (see `ProjectSettings/ProjectVersion.txt`).
- **Render pipeline:** URP 2D.
- **Input:** New Input System (`UnityEngine.InputSystem`).
- Saves are **JSON** (version 3) with map seed, units, cities, fog, wars, and eliminated civs.
- Do not commit `server/.env` — it contains database passwords. Only commit `.env.example`.

---

## License

See repository license if present. Third-party assets under `Assets/` may have their own licenses (e.g. Miniature Army 2D, terrain tile samples).
