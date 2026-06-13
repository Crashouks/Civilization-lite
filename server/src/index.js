require("dotenv").config();

const express = require("express");
const cors = require("cors");
const mysql = require("mysql2/promise");

const app = express();
const port = Number(process.env.PORT || 3000);

app.use(cors());
app.use(express.json({ limit: "4mb" }));

const pool = mysql.createPool({
  host: process.env.MYSQL_HOST || "127.0.0.1",
  port: Number(process.env.MYSQL_PORT || 3306),
  user: process.env.MYSQL_USER || "root",
  password: process.env.MYSQL_PASSWORD || "",
  database: process.env.MYSQL_DATABASE || "civilization_lite",
  waitForConnections: true,
  connectionLimit: 10,
});

async function ensurePlayer(externalId, displayName) {
  const id = String(externalId || "").trim();
  if (!id) {
    throw new Error("playerId is required");
  }

  await pool.query(
    `INSERT INTO players (external_id, display_name)
     VALUES (?, ?)
     ON DUPLICATE KEY UPDATE
       display_name = COALESCE(VALUES(display_name), display_name),
       updated_at = CURRENT_TIMESTAMP`,
    [id, displayName || null]
  );

  const [rows] = await pool.query(
    "SELECT id FROM players WHERE external_id = ? LIMIT 1",
    [id]
  );

  return rows[0].id;
}

app.get("/health", async (_req, res) => {
  try {
    await pool.query("SELECT 1");
    res.json({ ok: true, service: "civilization-lite-api" });
  } catch (error) {
    res.status(500).json({ ok: false, error: error.message });
  }
});

app.post("/api/saves", async (req, res) => {
  try {
    const { playerId, displayName, saveName, saveJson, turnNumber, playerCiv } = req.body || {};

    if (!playerId || !saveJson) {
      return res.status(400).json({ success: false, message: "playerId and saveJson are required" });
    }

    const playerDbId = await ensurePlayer(playerId, displayName);
    const name = String(saveName || "autosave").slice(0, 128);

    const [existing] = await pool.query(
      "SELECT id FROM game_saves WHERE player_id = ? AND save_name = ? LIMIT 1",
      [playerDbId, name]
    );

    if (existing.length > 0) {
      await pool.query(
        `UPDATE game_saves
         SET save_json = ?, turn_number = ?, player_civ = ?, updated_at = CURRENT_TIMESTAMP
         WHERE id = ?`,
        [saveJson, Number(turnNumber || 1), playerCiv || null, existing[0].id]
      );

      return res.json({
        success: true,
        saveId: existing[0].id,
        message: "Save updated",
      });
    }

    const [result] = await pool.query(
      `INSERT INTO game_saves (player_id, save_name, save_json, turn_number, player_civ)
       VALUES (?, ?, ?, ?, ?)`,
      [playerDbId, name, saveJson, Number(turnNumber || 1), playerCiv || null]
    );

    res.json({
      success: true,
      saveId: result.insertId,
      message: "Save created",
    });
  } catch (error) {
    console.error("POST /api/saves", error);
    res.status(500).json({ success: false, message: error.message });
  }
});

app.get("/api/saves/:playerId/slots", async (req, res) => {
  try {
    const playerId = String(req.params.playerId || "").trim();
    if (!playerId) {
      return res.status(400).json({ success: false, message: "playerId is required" });
    }

    const slotNames = ["slot1", "slot2", "slot3"];
    const [rows] = await pool.query(
      `SELECT gs.save_name, gs.turn_number, gs.player_civ, gs.updated_at
       FROM game_saves gs
       INNER JOIN players p ON p.id = gs.player_id
       WHERE p.external_id = ?
         AND gs.save_name IN (?, ?, ?)`,
      [playerId, ...slotNames]
    );

    const byName = {};
    for (const row of rows) {
      byName[row.save_name] = row;
    }

    const slots = slotNames.map((name) => {
      const row = byName[name];
      return {
        saveName: name,
        exists: !!row,
        turnNumber: row ? row.turn_number : 0,
        playerCiv: row ? row.player_civ : "",
        updatedAt: row ? row.updated_at : null,
      };
    });

    res.json({ success: true, slots });
  } catch (error) {
    console.error("GET /api/saves/:playerId/slots", error);
    res.status(500).json({ success: false, message: error.message });
  }
});

app.get("/api/saves/:playerId/slot/:slotName", async (req, res) => {
  try {
    const playerId = String(req.params.playerId || "").trim();
    const slotName = String(req.params.slotName || "").trim();

    if (!playerId || !slotName) {
      return res.status(400).json({ success: false, message: "playerId and slotName are required" });
    }

    if (!/^slot[1-3]$/.test(slotName)) {
      return res.status(400).json({ success: false, message: "Invalid slot name" });
    }

    const [rows] = await pool.query(
      `SELECT gs.id, gs.save_name, gs.save_json, gs.turn_number, gs.player_civ, gs.updated_at
       FROM game_saves gs
       INNER JOIN players p ON p.id = gs.player_id
       WHERE p.external_id = ? AND gs.save_name = ?
       LIMIT 1`,
      [playerId, slotName]
    );

    if (rows.length === 0) {
      return res.status(404).json({ success: false, message: "No save found" });
    }

    const row = rows[0];
    res.json({
      success: true,
      saveId: row.id,
      saveName: row.save_name,
      saveJson: row.save_json,
      turnNumber: row.turn_number,
      playerCiv: row.player_civ,
      updatedAt: row.updated_at,
    });
  } catch (error) {
    console.error("GET /api/saves/:playerId/slot/:slotName", error);
    res.status(500).json({ success: false, message: error.message });
  }
});

app.get("/api/saves/:playerId/latest", async (req, res) => {
  try {
    const playerId = String(req.params.playerId || "").trim();
    if (!playerId) {
      return res.status(400).json({ success: false, message: "playerId is required" });
    }

    const [rows] = await pool.query(
      `SELECT gs.id, gs.save_name, gs.save_json, gs.turn_number, gs.player_civ, gs.updated_at
       FROM game_saves gs
       INNER JOIN players p ON p.id = gs.player_id
       WHERE p.external_id = ?
       ORDER BY gs.updated_at DESC
       LIMIT 1`,
      [playerId]
    );

    if (rows.length === 0) {
      return res.status(404).json({ success: false, message: "No save found" });
    }

    const row = rows[0];
    res.json({
      success: true,
      saveId: row.id,
      saveName: row.save_name,
      saveJson: row.save_json,
      turnNumber: row.turn_number,
      playerCiv: row.player_civ,
      updatedAt: row.updated_at,
    });
  } catch (error) {
    console.error("GET /api/saves/:playerId/latest", error);
    res.status(500).json({ success: false, message: error.message });
  }
});

app.get("/api/saves/:playerId/list", async (req, res) => {
  try {
    const playerId = String(req.params.playerId || "").trim();
    if (!playerId) {
      return res.status(400).json({ success: false, message: "playerId is required" });
    }

    const [rows] = await pool.query(
      `SELECT gs.id, gs.save_name, gs.turn_number, gs.player_civ, gs.updated_at
       FROM game_saves gs
       INNER JOIN players p ON p.id = gs.player_id
       WHERE p.external_id = ?
       ORDER BY gs.updated_at DESC
       LIMIT 20`,
      [playerId]
    );

    res.json({ success: true, saves: rows });
  } catch (error) {
    console.error("GET /api/saves/:playerId/list", error);
    res.status(500).json({ success: false, message: error.message });
  }
});

app.listen(port, () => {
  console.log(`Civilization-lite API listening on http://localhost:${port}`);
});
