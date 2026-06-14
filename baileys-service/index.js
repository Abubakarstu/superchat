const {
  default: makeWASocket,
  useMultiFileAuthState,
  DisconnectReason,
  fetchLatestBaileysVersion,
} = require("@whiskeysockets/baileys");
const express = require("express");
const { toBuffer } = require("qrcode");
const pino = require("pino");
const NodeCache = require("node-cache");

const app = express();
app.use(express.json());

const PORT = process.env.PORT || 3001;
const NET_API_URL = process.env.NET_API_URL || "http://localhost:5000";
const msgRetryCounterCache = new NodeCache();

let sock = null;
let qrCode = null;
let connectionStatus = "disconnected";

async function startBot() {
  const { state, saveCreds } = await useMultiFileAuthState("auth_info");

  const { version } = await fetchLatestBaileysVersion();

  sock = makeWASocket({
    version,
    logger: pino({ level: "silent" }),
    printQRInTerminal: false,
    auth: state,
    msgRetryCounterCache,
    generateHighQualityLinkPreview: true,
    syncFullHistory: false,
  });

  sock.ev.on("connection.update", async (update) => {
    const { connection, lastDisconnect, qr } = update;

    if (qr) {
      qrCode = qr;
      connectionStatus = "awaiting_scan";
      console.log("QR code updated. Scan with WhatsApp.");
      try {
        const qrBuffer = await toBuffer(qr);
        qrCode = `data:image/png;base64,${qrBuffer.toString("base64")}`;
      } catch {
        qrCode = qr;
      }
    }

    if (connection === "open") {
      connectionStatus = "connected";
      console.log("WhatsApp connected successfully!");
    }

    if (connection === "close") {
      connectionStatus = "disconnected";
      const shouldReconnect =
        lastDisconnect?.error?.output?.statusCode !== DisconnectReason.loggedOut;
      console.log("Connection closed.", shouldReconnect ? "Reconnecting..." : "Logged out.");
      if (shouldReconnect) {
        startBot();
      }
    }
  });

  sock.ev.on("creds.update", saveCreds);

  sock.ev.on("messages.upsert", async (msg) => {
    const messages = msg.messages;
    for (const message of messages) {
      if (
        message.key.fromMe ||
        !message.message ||
        message.key.remoteJid === "status@broadcast"
      ) {
        continue;
      }

      const remoteJid = message.key.remoteJid;
      const text =
        message.message.conversation ||
        message.message.extendedTextMessage?.text ||
        "";
      const pushName = message.pushName || remoteJid;

      if (text) {
        console.log(`Incoming from ${pushName} (${remoteJid}): ${text}`);
        try {
          await fetch(`${NET_API_URL}/api/webhook/incoming`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
              remoteJid,
              content: text,
              contactName: pushName,
              contactPhone: remoteJid.split("@")[0],
              messageId: message.key.id,
              messageType: "text",
            }),
          });
        } catch (err) {
          console.error("Failed to forward to .NET API:", err.message);
        }
      }
    }
  });
}

// Routes
app.post("/send-message", async (req, res) => {
  const { remoteJid, message } = req.body;

  if (!remoteJid || !message) {
    return res.status(400).json({ error: "remoteJid and message are required" });
  }

  if (!sock) {
    return res.status(503).json({ error: "WhatsApp not connected" });
  }

  try {
    await sock.sendMessage(remoteJid, { text: message });
    res.json({ status: "sent", remoteJid, message });
  } catch (err) {
    console.error("Send error:", err);
    res.status(500).json({ error: err.message });
  }
});

app.get("/qr", (req, res) => {
  res.json({ qr: qrCode, status: connectionStatus });
});

app.get("/health", (req, res) => {
  res.json({ status: connectionStatus, connected: connectionStatus === "connected" });
});

app.listen(PORT, () => {
  console.log(`Baileys service running on port ${PORT}`);
  startBot();
});
