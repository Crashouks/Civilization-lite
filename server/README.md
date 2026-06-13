# Civilization-lite MySQL API

> **Full setup guide:** see the [MySQL & cloud save tutorial](../README.md#mysql-cloud-saves--overview) in the root README.

Unity should **not** connect to MySQL directly (database password would be inside the game).  
This small Node.js server talks to MySQL and exposes a REST API for cloud saves.

## 1. Install MySQL

Install MySQL 8+ locally and create the schema:

```bash
mysql -u root -p < schema.sql
```

Or run `schema.sql` in MySQL Workbench.

## 2. Configure the API

```bash
cd server
copy .env.example .env
npm install
```

Edit `.env`:

```
PORT=3000
MYSQL_HOST=127.0.0.1
MYSQL_PORT=3306
MYSQL_USER=civlite
MYSQL_PASSWORD=your_password
MYSQL_DATABASE=civilization_lite
```

Create the MySQL user (optional):

```sql
CREATE USER 'civlite'@'localhost' IDENTIFIED BY 'your_password';
GRANT ALL PRIVILEGES ON civilization_lite.* TO 'civlite'@'localhost';
FLUSH PRIVILEGES;
```

## 3. Start the server

```bash
npm start
```

Health check: [http://localhost:3000/health](http://localhost:3000/health)

## 4. Enable cloud save in Unity

1. Play the game
2. Open **ESC → Налаштування**
3. Turn on **Хмарне збереження (MySQL)**
4. Use **Зберегти** / **Завантажити** in the pause menu

Default API URL: `http://localhost:3000`

## API endpoints

| Method | Path | Description |
|--------|------|-------------|
| GET | `/health` | Server + DB status |
| POST | `/api/saves` | Upload/update save JSON (`saveName`: slot1–slot3) |
| GET | `/api/saves/:playerId/slots` | Summary of all 3 slots |
| GET | `/api/saves/:playerId/slot/:slotName` | Download slot (slot1, slot2, slot3) |
| GET | `/api/saves/:playerId/latest` | Download latest save (legacy) |
| GET | `/api/saves/:playerId/list` | List recent saves |

### POST `/api/saves` body

```json
{
  "playerId": "device-or-account-id",
  "displayName": "Player",
  "saveName": "autosave",
  "saveJson": "{...}",
  "turnNumber": 12,
  "playerCiv": "Rome"
}
```

Local JSON save still works when cloud save is disabled or the server is offline.
