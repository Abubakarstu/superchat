const {
  default: makeWASocket,
  useMultiFileAuthState,
  DisconnectReason,
  fetchLatestBaileysVersion,
  downloadContentFromMessage,
  makeCacheableSignalKeyStore,
  Browsers,
} = require("@whiskeysockets/baileys");
const express = require("express");
const { toBuffer } = require("qrcode");
const pino = require("pino");
const NodeCache = require("node-cache");
const https = require("https");
const http = require("http");
const fs = require("fs");
const path = require("path");
const crypto = require("crypto");

const app = express();
app.use(express.json({ limit: "50mb" }));
app.use(express.urlencoded({ extended: true, limit: "50mb" }));

const PORT = process.env.PORT || 3001;
const msgRetryCounterCache = new NodeCache();
const httpsAgent = new https.Agent({ rejectUnauthorized: false });
const UPLOAD_DIR = path.join(__dirname, "uploads");
if (!fs.existsSync(UPLOAD_DIR)) fs.mkdirSync(UPLOAD_DIR, { recursive: true });
app.use("/uploads", express.static(UPLOAD_DIR));

let sock = null;
let qrCode = null;
let connectionStatus = "disconnected";
let netApiUrl = process.env.NET_API_URL || null;
let discoverInterval = null;
let messageStore = {}; // messageId -> fullMessage for edit/delete/reaction

function downloadUrl(url) {
  return new Promise((resolve, reject) => {
    const mod = url.startsWith("https") ? https : http;
    const opts = {};
    if (url.startsWith("https")) opts.agent = httpsAgent;
    mod.get(url, opts, (res) => {
      const chunks = [];
      res.on("data", (c) => chunks.push(c));
      res.on("end", () => resolve(Buffer.concat(chunks)));
    }).on("error", reject);
  });
}

function mimeToType(mime) {
  if (mime.startsWith("image/")) return "image";
  if (mime.startsWith("video/")) return "video";
  if (mime.startsWith("audio/")) return "audio";
  return "document";
}

async function discoverApi() {
  if (netApiUrl && netApiUrl !== "pending") return;

  const candidates = [
    ...(netApiUrl && netApiUrl !== "pending" ? [netApiUrl] : []),
    "http://localhost:5000",
    "http://localhost:51015",
    "https://localhost:44339",
    "http://localhost:5010",
    "http://localhost:5001",
    "http://localhost:3000",
  ];

  for (const url of candidates) {
    try {
      const opts = { signal: AbortSignal.timeout(2000) };
      if (url.startsWith("https")) opts.agent = httpsAgent;
      const res = await fetch(`${url}/api/config/status`, opts);
      if (res.ok) {
        netApiUrl = url;
        console.log(`✓ Connected to .NET API at ${url}`);
        if (discoverInterval) { clearInterval(discoverInterval); discoverInterval = null; }
        return;
      }
    } catch {}
  }
  if (!netApiUrl) netApiUrl = "pending";
  if (!discoverInterval) {
    discoverInterval = setInterval(discoverApi, 5000);
    console.log("⏳ Searching for .NET API... (retrying every 5s)");
  }
}

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
    browser: Browsers.macOS("Chrome"),
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

  // Store messages as they come for edit/delete/reaction lookups
  sock.ev.on("messages.upsert", async (msg) => {
    for (const m of msg.messages) {
      if (m.key?.id) messageStore[m.key.id] = m;
    }
    await handleIncomingMessages(msg);
  });

  sock.ev.on("presence.update", async ({ id, presences }) => {
    if (!netApiUrl || netApiUrl === "pending") return;
    for (const [jid, presence] of Object.entries(presences)) {
      if (jid.includes("status")) continue;
      const isOnline = presence.lastKnownPresence === "available";
      const isTyping = presence.lastKnownPresence === "composing" || presence.lastKnownPresence === "recording";
      try {
        const fetchOpts = { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ remoteJid: id, isOnline }) };
        if (netApiUrl.startsWith("https")) fetchOpts.agent = httpsAgent;
        await fetch(`${netApiUrl}/api/presence/online`, fetchOpts);
        if (isTyping !== undefined) {
          const typingOpts = { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ remoteJid: id, isTyping }) };
          if (netApiUrl.startsWith("https")) typingOpts.agent = httpsAgent;
          await fetch(`${netApiUrl}/api/presence/typing`, typingOpts);
        }
      } catch {}
    }
  });
}

async function handleIncomingMessages(msg) {
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
    const pushName = message.pushName || remoteJid;
    const msg = message.message;
    let text = "";
    let msgType = "text";
    let mediaUrl = null;
    let fileName = null;
    let mimeType = null;

    // Parse all message types
    if (msg?.conversation) {
      text = msg.conversation;
    } else if (msg?.extendedTextMessage?.text) {
      text = msg.extendedTextMessage.text;
    } else if (msg?.imageMessage?.caption) {
      text = msg.imageMessage.caption; msgType = "image";
      fileName = msg.imageMessage.fileName || "image.jpg";
      mimeType = msg.imageMessage.mimetype || "image/jpeg";
    } else if (msg?.imageMessage) {
      text = "📷 Image"; msgType = "image";
      fileName = msg.imageMessage.fileName || "image.jpg";
      mimeType = msg.imageMessage.mimetype || "image/jpeg";
    } else if (msg?.videoMessage?.caption) {
      text = msg.videoMessage.caption; msgType = "video";
      fileName = msg.videoMessage.fileName || "video.mp4";
      mimeType = msg.videoMessage.mimetype || "video/mp4";
    } else if (msg?.videoMessage) {
      text = "🎥 Video"; msgType = "video";
      fileName = msg.videoMessage.fileName || "video.mp4";
      mimeType = msg.videoMessage.mimetype || "video/mp4";
    } else if (msg?.audioMessage) {
      text = "🎵 Audio"; msgType = "audio";
      fileName = "audio.mp3";
      mimeType = msg.audioMessage.mimetype || "audio/mp4";
    } else if (msg?.documentMessage) {
      text = `📄 ${msg.documentMessage.fileName || "Document"}`; msgType = "document";
      fileName = msg.documentMessage.fileName || "document.bin";
      mimeType = msg.documentMessage.mimetype || "application/octet-stream";
    } else if (msg?.locationMessage) {
      const loc = msg.locationMessage;
      text = `📍 ${loc.degreesLatitude || ""}, ${loc.degreesLongitude || ""}`;
      if (loc.name) text += ` — ${loc.name}`;
      if (loc.address) text += ` (${loc.address})`;
      msgType = "location";
    } else if (msg?.contactMessage) {
      text = `👤 ${msg.contactMessage.displayName || "Contact"}`;
      msgType = "contact";
    } else if (msg?.reactionMessage) {
      const react = msg.reactionMessage;
      text = `${react.text || ""}`;
      msgType = "reaction";
      // Forward reaction key info so C# can match it
      fileName = react.key?.id || "";
    } else if (msg?.pollCreationMessage) {
      const poll = msg.pollCreationMessage;
      text = `📊 ${poll.name || "Poll"}`;
      if (poll.options) text += ` [${poll.options.length} options]`;
      msgType = "poll";
      mimeType = JSON.stringify({ pollName: poll.name, options: (poll.options||[]).map(o => o.optionName) });
    } else if (msg?.pollUpdateMessage) {
      const pu = msg.pollUpdateMessage;
      text = "📊 Poll vote update";
      msgType = "poll_update";
    } else if (msg?.buttonsResponseMessage) {
      text = `🔘 ${msg.buttonsResponseMessage.selectedButtonId || "Button clicked"}: ${msg.buttonsResponseMessage.selectedDisplayText || ""}`;
      msgType = "interactive_response";
    } else if (msg?.listResponseMessage) {
      const lr = msg.listResponseMessage;
      text = `📋 ${lr.title || "Selected"}: ${lr.description || lr.singleSelectReply?.selectedRowId || ""}`;
      msgType = "list_response";
    } else if (msg?.templateButtonReplyMessage) {
      text = `🔗 ${msg.templateButtonReplyMessage.selectedId || "Template button clicked"}`;
      msgType = "template_response";
    } else if (msg?.interactiveResponseMessage) {
      const ir = msg.interactiveResponseMessage?.nativeFlowResponseMessage?.params?.json || "{}";
      try { const p = JSON.parse(ir); text = `📱 ${p.id || "Interactive"}: ${p.title || p.response || ""}`; } catch { text = "📱 Interactive response"; }
      msgType = "interactive_response";
    } else if (msg?.stickerMessage) {
      text = "🖼️ Sticker"; msgType = "sticker";
      mimeType = msg.stickerMessage.mimetype || "image/webp";
    } else if (msg?.protocolMessage) {
      // Disappearing message settings change, ignore
      continue;
    }

    if (text && netApiUrl && netApiUrl !== "pending") {
      console.log(`Incoming ${msgType} from ${pushName} (${remoteJid}): ${text.substring(0, 50)}`);

      // Download incoming media
      if (["image","video","audio","document"].includes(msgType)) {
        try {
          let mediaMsg;
          if (msgType === "image") mediaMsg = msg.imageMessage;
          else if (msgType === "video") mediaMsg = msg.videoMessage;
          else if (msgType === "audio") mediaMsg = msg.audioMessage;
          else if (msgType === "document") mediaMsg = msg.documentMessage;
          if (mediaMsg) {
            const stream = await downloadContentFromMessage(mediaMsg, msgType === "audio" ? "audio" : msgType);
            const chunks = [];
            for await (const chunk of stream) chunks.push(chunk);
            const buf = Buffer.concat(chunks);
            const ext = fileName ? path.extname(fileName) : ".bin";
            const savedName = `${crypto.randomUUID()}${ext}`;
            fs.writeFileSync(path.join(UPLOAD_DIR, savedName), buf);
            mediaUrl = `${netApiUrl}/uploads/${savedName}`;
          }
        } catch (dlErr) {
          console.error("Failed to download incoming media:", dlErr.message);
        }
      }

      try {
        const fetchOpts = {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            remoteJid,
            content: text,
            contactName: pushName,
            contactPhone: remoteJid.split("@")[0],
            messageId: message.key.id,
            messageType: msgType,
            mediaUrl,
            fileName,
            mimeType,
          }),
        };
        if (netApiUrl.startsWith("https")) fetchOpts.agent = httpsAgent;
        const res = await fetch(`${netApiUrl}/api/webhook/incoming`, fetchOpts);
        if (!res.ok) console.error(`API returned ${res.status}`);
      } catch (err) {
        console.error("Failed to forward to .NET API:", err.message);
      }
    }
  }
}

// ==================== ENDPOINTS ====================

// --- Existing endpoints ---

app.post("/send-message", async (req, res) => {
  const { remoteJid, message } = req.body;
  if (!remoteJid || !message) return res.status(400).json({ error: "remoteJid and message are required" });
  if (!sock) return res.status(503).json({ error: "WhatsApp not connected" });
  try {
    await sock.sendMessage(remoteJid, { text: message });
    res.json({ status: "sent", remoteJid, message });
  } catch (err) {
    console.error("Send error:", err);
    res.status(500).json({ error: err.message });
  }
});

app.post("/send-media", async (req, res) => {
  const { remoteJid, mediaUrl, mediaType, caption, fileName, mimeType } = req.body;
  if (!remoteJid || !mediaUrl || !mediaType) return res.status(400).json({ error: "remoteJid, mediaUrl, and mediaType are required" });
  if (!sock) return res.status(503).json({ error: "WhatsApp not connected" });
  try {
    const absUrl = mediaUrl.startsWith("/") && netApiUrl && netApiUrl !== "pending" ? `${netApiUrl}${mediaUrl}` : mediaUrl;
    const buffer = await downloadUrl(absUrl);
    let payload;
    switch (mediaType) {
      case "image": payload = { image: buffer, caption: caption || "" }; break;
      case "video": payload = { video: buffer, caption: caption || "" }; break;
      case "audio": payload = { audio: buffer, mimetype: mimeType || "audio/mp4" }; break;
      case "document": payload = { document: buffer, fileName: fileName || "File", caption: caption || "" }; break;
      default: payload = { text: caption || "Media" };
    }
    await sock.sendMessage(remoteJid, payload);
    res.json({ status: "sent", remoteJid, mediaType, fileName });
  } catch (err) {
    console.error("Send media error:", err);
    res.status(500).json({ error: err.message });
  }
});

// --- New: Send reaction ---
app.post("/send-reaction", async (req, res) => {
  const { remoteJid, messageId, emoji, remove } = req.body;
  if (!remoteJid || !messageId || !emoji) return res.status(400).json({ error: "remoteJid, messageId, emoji required" });
  if (!sock) return res.status(503).json({ error: "WhatsApp not connected" });
  try {
    // Find the original message key from store
    const stored = messageStore[messageId];
    if (!stored?.key) return res.status(404).json({ error: "Original message not found in store" });
    await sock.sendMessage(remoteJid, {
      react: { text: remove ? "" : emoji, key: stored.key }
    });
    res.json({ status: "sent", emoji, removed: !!remove });
  } catch (err) {
    console.error("Reaction error:", err);
    res.status(500).json({ error: err.message });
  }
});

// --- New: Read receipts ---
app.post("/read-receipts", async (req, res) => {
  const { remoteJid, messageIds } = req.body;
  if (!remoteJid || !messageIds?.length) return res.status(400).json({ error: "remoteJid and messageIds required" });
  if (!sock) return res.status(503).json({ error: "WhatsApp not connected" });
  try {
    const keys = messageIds.map(id => {
      const stored = messageStore[id];
      return stored?.key || { id, remoteJid };
    });
    await sock.readMessages(keys);
    res.json({ status: "read", count: keys.length });
  } catch (err) {
    console.error("Read receipt error:", err);
    res.status(500).json({ error: err.message });
  }
});

// --- New: Edit message ---
app.post("/edit-message", async (req, res) => {
  const { remoteJid, messageId, newText } = req.body;
  if (!remoteJid || !messageId || newText === undefined) return res.status(400).json({ error: "remoteJid, messageId, newText required" });
  if (!sock) return res.status(503).json({ error: "WhatsApp not connected" });
  try {
    const stored = messageStore[messageId];
    if (!stored?.key) return res.status(404).json({ error: "Message not found in store" });
    await sock.sendMessage(remoteJid, { text: newText, edit: stored.key });
    res.json({ status: "edited", messageId });
  } catch (err) {
    console.error("Edit error:", err);
    res.status(500).json({ error: err.message });
  }
});

// --- New: Delete for everyone ---
app.post("/delete-message", async (req, res) => {
  const { remoteJid, messageId, forEveryone } = req.body;
  if (!remoteJid || !messageId) return res.status(400).json({ error: "remoteJid and messageId required" });
  if (!sock) return res.status(503).json({ error: "WhatsApp not connected" });
  try {
    if (forEveryone) {
      const stored = messageStore[messageId];
      if (!stored?.key) return res.status(404).json({ error: "Message not found in store" });
      await sock.sendMessage(remoteJid, { delete: stored.key });
    }
    // For "delete for me" it's a local-only operation on the C# side
    res.json({ status: forEveryone ? "deleted_for_everyone" : "deleted_for_me" });
  } catch (err) {
    console.error("Delete error:", err);
    res.status(500).json({ error: err.message });
  }
});

// --- New: Send contact ---
app.post("/send-contact", async (req, res) => {
  const { remoteJid, contactName, contactPhone } = req.body;
  if (!remoteJid || !contactName || !contactPhone) return res.status(400).json({ error: "remoteJid, contactName, contactPhone required" });
  if (!sock) return res.status(503).json({ error: "WhatsApp not connected" });
  try {
    const vcard = `BEGIN:VCARD\nVERSION:3.0\nFN:${contactName}\nTEL;type=CELL:+${contactPhone.replace(/\D/g, '')}\nEND:VCARD`;
    await sock.sendMessage(remoteJid, {
      contacts: { displayName: contactName, contacts: [{ vcard }] }
    });
    res.json({ status: "sent", contactName, contactPhone });
  } catch (err) {
    console.error("Send contact error:", err);
    res.status(500).json({ error: err.message });
  }
});

// --- New: Send poll ---
app.post("/send-poll", async (req, res) => {
  const { remoteJid, pollName, options, selectableCount } = req.body;
  if (!remoteJid || !pollName || !options?.length) return res.status(400).json({ error: "remoteJid, pollName, options required" });
  if (!sock) return res.status(503).json({ error: "WhatsApp not connected" });
  try {
    await sock.sendMessage(remoteJid, {
      poll: { name: pollName, values: options, selectableCount: selectableCount || 1 }
    });
    res.json({ status: "sent", pollName, optionCount: options.length });
  } catch (err) {
    console.error("Send poll error:", err);
    res.status(500).json({ error: err.message });
  }
});

// --- New: Send status/story ---
app.post("/send-status", async (req, res) => {
  const { text, mediaUrl, mediaType } = req.body;
  if (!sock) return res.status(503).json({ error: "WhatsApp not connected" });
  try {
    let payload;
    if (mediaUrl) {
      const buf = await downloadUrl(mediaUrl);
      if (mediaType === "image") payload = { image: buf, caption: text || "" };
      else if (mediaType === "video") payload = { video: buf, caption: text || "" };
      else payload = { text: text || "Status" };
    } else {
      payload = { text: text || "Status" };
    }
    await sock.sendMessage("status@broadcast", payload);
    res.json({ status: "sent" });
  } catch (err) {
    console.error("Send status error:", err);
    res.status(500).json({ error: err.message });
  }
});

// --- Group operations ---
app.post("/group-create", async (req, res) => {
  const { subject, participants } = req.body;
  if (!subject || !participants?.length) return res.status(400).json({ error: "subject and participants required" });
  if (!sock) return res.status(503).json({ error: "WhatsApp not connected" });
  try {
    const group = await sock.groupCreate(subject, participants);
    res.json({ status: "created", jid: group.id, subject: group.subject });
  } catch (err) {
    console.error("Group create error:", err);
    res.status(500).json({ error: err.message });
  }
});

app.post("/group-participants", async (req, res) => {
  const { groupJid, participants, action } = req.body;
  if (!groupJid || !participants?.length || !action) return res.status(400).json({ error: "groupJid, participants, action required" });
  if (!sock) return res.status(503).json({ error: "WhatsApp not connected" });
  try {
    const validActions = ["add", "remove", "promote", "demote"];
    if (!validActions.includes(action)) return res.status(400).json({ error: `action must be one of: ${validActions.join(", ")}` });
    const result = await sock.groupParticipantsUpdate(groupJid, participants, action);
    res.json({ status: "ok", action, results: result });
  } catch (err) {
    console.error("Group participants error:", err);
    res.status(500).json({ error: err.message });
  }
});

app.post("/group-update", async (req, res) => {
  const { groupJid, subject, description, setting, ephemeralDuration } = req.body;
  if (!groupJid) return res.status(400).json({ error: "groupJid required" });
  if (!sock) return res.status(503).json({ error: "WhatsApp not connected" });
  try {
    const results = {};
    if (subject) { await sock.groupUpdateSubject(groupJid, subject); results.subject = subject; }
    if (description) { await sock.groupUpdateDescription(groupJid, description); results.description = description; }
    if (setting) {
      const validSettings = ["announcement", "not_announcement", "locked", "unlocked"];
      if (!validSettings.includes(setting)) return res.status(400).json({ error: `setting must be: ${validSettings.join(", ")}` });
      await sock.groupSettingUpdate(groupJid, setting);
      results.setting = setting;
    }
    if (ephemeralDuration !== undefined) {
      await sock.sendMessage(groupJid, { disappearingMessagesInChat: ephemeralDuration });
      results.ephemeralDuration = ephemeralDuration;
    }
    res.json({ status: "updated", ...results });
  } catch (err) {
    console.error("Group update error:", err);
    res.status(500).json({ error: err.message });
  }
});

app.get("/group-metadata", async (req, res) => {
  const { groupJid } = req.query;
  if (!groupJid) return res.status(400).json({ error: "groupJid query param required" });
  if (!sock) return res.status(503).json({ error: "WhatsApp not connected" });
  try {
    const metadata = await sock.groupMetadata(groupJid);
    res.json({
      id: metadata.id,
      subject: metadata.subject,
      subjectOwner: metadata.subjectOwner,
      subjectTime: metadata.subjectTime,
      size: metadata.size,
      desc: metadata.desc,
      descId: metadata.descId,
      descOwner: metadata.descOwner,
      participants: metadata.participants.map(p => ({
        jid: p.id,
        admin: p.admin || null,
        name: p.name || "",
      })),
      ephemeralDuration: metadata.ephemeralDuration,
      announce: metadata.announce,
      restrict: metadata.restrict,
    });
  } catch (err) {
    console.error("Group metadata error:", err);
    res.status(500).json({ error: err.message });
  }
});

app.post("/group-leave", async (req, res) => {
  const { groupJid } = req.body;
  if (!groupJid) return res.status(400).json({ error: "groupJid required" });
  if (!sock) return res.status(503).json({ error: "WhatsApp not connected" });
  try {
    await sock.groupLeave(groupJid);
    res.json({ status: "left" });
  } catch (err) {
    console.error("Group leave error:", err);
    res.status(500).json({ error: err.message });
  }
});

// --- New: Block/unblock contact ---
app.post("/block-contact", async (req, res) => {
  const { remoteJid, block } = req.body;
  if (!remoteJid) return res.status(400).json({ error: "remoteJid required" });
  if (!sock) return res.status(503).json({ error: "WhatsApp not connected" });
  try {
    await sock.updateBlockStatus(remoteJid, block ? "block" : "unblock");
    res.json({ status: block ? "blocked" : "unblocked" });
  } catch (err) {
    console.error("Block error:", err);
    res.status(500).json({ error: err.message });
  }
});

// --- New: Update profile ---
app.post("/update-profile", async (req, res) => {
  const { name, status, profilePictureUrl } = req.body;
  if (!sock) return res.status(503).json({ error: "WhatsApp not connected" });
  try {
    if (name) await sock.updateProfileName(name);
    if (status) await sock.updateProfileStatus(status);
    if (profilePictureUrl) {
      const buf = await downloadUrl(profilePictureUrl);
      await sock.updateProfilePicture(sock.user.id, buf);
    }
    res.json({ status: "updated", name, status: status });
  } catch (err) {
    console.error("Update profile error:", err);
    res.status(500).json({ error: err.message });
  }
});

// --- Existing endpoints ---

async function downloadMedia(url) {
  const resp = await fetch(url);
  return Buffer.from(await resp.arrayBuffer());
}

app.post("/send-template", async (req, res) => {
  const { remoteJid, templateName, language, body, header, footer, buttons, contentType, typesJson } = req.body;
  if (!remoteJid || !body) return res.status(400).json({ error: "remoteJid and body are required" });
  if (!sock) return res.status(503).json({ error: "WhatsApp not connected" });
  try {
    let msg;
    if (contentType && typesJson) {
      let types;
      try { types = JSON.parse(typesJson); } catch { types = {}; }
      const content = types[contentType] || types;
      const hdr = header || content.header || content.header_text || content.title || "";
      const ftr = footer || content.footer || "";
      switch (contentType) {
        case "twilio/text": msg = { text: body }; break;
        case "twilio/media": {
          const mediaUrl = content.media?.[0];
          if (mediaUrl) {
            const buf = await downloadMedia(mediaUrl);
            const ext = path.extname(new URL(mediaUrl).pathname).toLowerCase();
            if ([".jpg",".jpeg",".png",".gif",".webp"].includes(ext)) msg = { image: buf, caption: body };
            else if ([".mp4",".webm",".mov"].includes(ext)) msg = { video: buf, caption: body };
            else msg = { document: buf, fileName: `file${ext}`, caption: body };
          } else { msg = { text: body }; }
          break;
        }
        case "twilio/quick-reply": {
          const btnArr = (content.actions || []).map((a, i) => ({ buttonId: a.id || `btn_${i}`, buttonText: { displayText: a.title || `Option ${i+1}` }, type: 1 }));
          const qrMsg = { text: body, footer: ftr, buttons: btnArr };
          if (hdr) qrMsg.header = hdr;
          msg = qrMsg;
          break;
        }
        case "twilio/call-to-action": {
          const tmplBtns = (content.actions || []).map((a, i) => {
            if (a.type === "URL") return { index: i+1, urlButton: { displayText: a.title, url: a.url } };
            if (a.type === "PHONE_NUMBER") return { index: i+1, callButton: { displayText: a.title, phoneNumber: a.phone } };
            return { index: i+1, quickReplyButton: { displayText: a.title, id: `act_${i}` } };
          });
          const ctaMsg = { text: body, footer: ftr, templateButtons: tmplBtns };
          if (hdr) ctaMsg.header = hdr;
          msg = ctaMsg;
          break;
        }
        case "twilio/list-picker": {
          const sections = [{ title: body.slice(0,50), rows: (content.items||[]).map((item,i) => ({ title: item.item, rowId: item.id||`row_${i}`, description: (item.description||"").slice(0,70) })) }];
          msg = { text: body, footer: ftr, title: hdr||body.slice(0,24), buttonText: content.button||"Select", sections };
          break;
        }
        case "twilio/card": case "whatsapp/card": {
          const cardActions = (content.actions||[]).map((a,i) => {
            if (a.type==="URL") return { index: i+1, urlButton: { displayText: a.title, url: a.url } };
            if (a.type==="PHONE_NUMBER") return { index: i+1, callButton: { displayText: a.title, phoneNumber: a.phone } };
            return { index: i+1, quickReplyButton: { displayText: a.title, id: a.id||`card_${i}` } };
          });
          const cardMedia = content.media?.[0];
          if (cardMedia) {
            const buf = await downloadMedia(cardMedia);
            const cardMsg = { image: buf, caption: body, footer: ftr };
            if (cardActions.length) cardMsg.templateButtons = cardActions;
            msg = cardMsg;
          } else {
            const cardMsg = { text: body, footer: ftr };
            if (cardActions.length) cardMsg.templateButtons = cardActions;
            msg = cardMsg;
          }
          break;
        }
        case "twilio/carousel": {
          const firstCard = content.cards?.[0];
          if (firstCard) {
            const cardActions = (firstCard.actions||[]).map((a,i) => {
              if (a.type==="URL") return { index: i+1, urlButton: { displayText: a.title, url: a.url } };
              if (a.type==="PHONE_NUMBER") return { index: i+1, callButton: { displayText: a.title, phoneNumber: a.phone } };
              return { index: i+1, quickReplyButton: { displayText: a.title, id: a.id||`car_${i}` } };
            });
            const carText = `${firstCard.title}\n${firstCard.body}`;
            if (firstCard.media) {
              const buf = await downloadMedia(firstCard.media);
              const carMsg = { image: buf, caption: carText, footer: ftr };
              if (cardActions.length) carMsg.templateButtons = cardActions;
              msg = carMsg;
            } else {
              const carMsg = { text: carText, footer: ftr };
              if (cardActions.length) carMsg.templateButtons = cardActions;
              msg = carMsg;
            }
          } else { msg = { text: body }; }
          break;
        }
        case "twilio/location": msg = { location: { degreesLatitude: content.latitude, degreesLongitude: content.longitude } }; break;
        case "baileys/buttons": {
          const btnArr = (content.buttons||content.actions||[]).map((a,i) => ({ buttonId: a.id||`btn_${i}`, buttonText: { displayText: a.title||`Option ${i+1}` }, type: 1 }));
          const btnMsg = { text: body, footer: ftr, buttons: btnArr };
          if (hdr) btnMsg.header = hdr;
          msg = btnMsg;
          break;
        }
        case "baileys/list": {
          const listSections = (content.sections||[]).map((s,si) => ({ title: s.title||`Section ${si+1}`, rows: (s.rows||[]).map((r,ri) => ({ title: r.title, rowId: r.id||r.rowId||`row_${si}_${ri}`, description: r.description||"" })) }));
          msg = { text: body, footer: ftr, title: hdr||body.slice(0,24), buttonText: content.buttonText||content.button||"Select", sections: listSections };
          break;
        }
        case "baileys/template": {
          const tmplBtns = (content.buttons||content.actions||[]).map((a,i) => {
            if (a.type==="URL"||a.url) return { index: i+1, urlButton: { displayText: a.title, url: a.url } };
            if (a.type==="PHONE_NUMBER"||a.phone) return { index: i+1, callButton: { displayText: a.title, phoneNumber: a.phone } };
            return { index: i+1, quickReplyButton: { displayText: a.title, id: a.id||`tmpl_${i}` } };
          });
          const tplMsg = { text: body, footer: ftr, templateButtons: tmplBtns };
          if (hdr) tplMsg.header = hdr;
          msg = tplMsg;
          break;
        }
        case "baileys/sticker": {
          const stickerUrl = content.url||content.media?.[0];
          if (stickerUrl) { const buf = await downloadMedia(stickerUrl); msg = { sticker: buf }; }
          else { msg = { text: body }; }
          break;
        }
        default: msg = { text: body };
      }
    } else {
      let text = "";
      if (header) text += header + "\n\n";
      text += body;
      if (footer) text += "\n\n" + footer;
      msg = { text };
    }
    if (msg.templateButtons && msg.templateButtons.length === 0) delete msg.templateButtons;
    if (msg.buttons && msg.buttons.length === 0) delete msg.buttons;
    await sock.sendMessage(remoteJid, msg);
    res.json({ status: "sent", remoteJid, templateName, contentType });
  } catch (err) {
    console.error("Send template error:", err);
    res.status(500).json({ error: err.message });
  }
});

app.post("/forward-media", async (req, res) => {
  const { remoteJid, messageId, mediaType } = req.body;
  if (!remoteJid || !messageId || !mediaType) return res.status(400).json({ error: "remoteJid, messageId, mediaType required" });
  if (!sock) return res.status(503).json({ error: "WhatsApp not connected" });
  try {
    const msgs = await sock.loadMessages(remoteJid, 20);
    const found = msgs.find(m => m.key.id === messageId);
    if (!found || !found.message) return res.status(404).json({ error: "Message not found" });
    const msg = found.message;
    let mediaMsg, stream, buffer;
    if (mediaType === "image" && msg.imageMessage) mediaMsg = msg.imageMessage;
    else if (mediaType === "video" && msg.videoMessage) mediaMsg = msg.videoMessage;
    else if (mediaType === "audio" && msg.audioMessage) mediaMsg = msg.audioMessage;
    else if (mediaType === "document" && msg.documentMessage) mediaMsg = msg.documentMessage;
    if (!mediaMsg) return res.status(400).json({ error: "Media not found in message" });
    stream = await downloadContentFromMessage(mediaMsg, mediaType === "audio" ? "audio" : mediaType);
    const chunks = [];
    for await (const chunk of stream) chunks.push(chunk);
    buffer = Buffer.concat(chunks);
    const fileName = `${crypto.randomUUID()}.${mediaType==="image"?"jpg":mediaType==="video"?"mp4":mediaType==="audio"?"mp3":"bin"}`;
    fs.writeFileSync(path.join(UPLOAD_DIR, fileName), buffer);
    res.json({ status: "ok", fileName, mediaUrl: `${netApiUrl}/uploads/${fileName}` });
  } catch (err) {
    console.error("Forward media error:", err);
    res.status(500).json({ error: err.message });
  }
});

app.post("/upload", (req, res) => {
  if (!req.body || !req.body.file) return res.status(400).json({ error: "No file data" });
  const { fileName, fileData } = req.body;
  const buffer = Buffer.from(fileData, "base64");
  const name = fileName || `${crypto.randomUUID()}.bin`;
  fs.writeFileSync(path.join(UPLOAD_DIR, name), buffer);
  res.json({ status: "ok", fileName: name, filePath: name });
});

app.get("/profile-picture", async (req, res) => {
  const { jid } = req.query;
  if (!jid) return res.status(400).json({ error: "jid query param required" });
  if (!sock) return res.status(503).json({ error: "WhatsApp not connected" });
  try {
    const url = await sock.profilePictureUrl(jid, "image");
    const resp = await fetch(url);
    const buf = Buffer.from(await resp.arrayBuffer());
    const contentType = resp.headers.get("content-type") || "image/jpeg";
    res.set("Content-Type", contentType);
    res.set("Cache-Control", "public, max-age=86400");
    res.send(buf);
  } catch {
    res.status(404).json({ error: "No profile picture found" });
  }
});

app.get("/qr", (req, res) => {
  res.json({ qr: qrCode, status: connectionStatus });
});

app.get("/health", (req, res) => {
  res.json({ status: connectionStatus, connected: connectionStatus === "connected" });
});

app.post("/register", (req, res) => {
  const { apiUrl } = req.body;
  if (!apiUrl) return res.status(400).json({ error: "apiUrl required" });
  netApiUrl = apiUrl.replace(/\/+$/, "");
  if (discoverInterval) { clearInterval(discoverInterval); discoverInterval = null; }
  console.log(`✓ .NET API registered at ${netApiUrl}`);
  res.json({ status: "registered", apiUrl: netApiUrl });
});

app.listen(PORT, async () => {
  console.log(`Baileys service running on port ${PORT}`);
  await discoverApi();
  startBot();
});
