// State
let currentConvId = null, conversations = [], convs = [], currentPanel = "inbox", aiConfigId = null;
let conversationChart = null, channelChart = null, typingTimers = {};
let pendingAttachment = null;

function avatar(name, url, size) {
  if (url) {
    const safeName = (name||"").replace(/"/g,"&quot;").replace(/'/g,"&#39;");
    return `<img src="${url}" class="avatar-img" style="width:${size}px;height:${size}px" data-name="${safeName}" data-size="${size}" onerror="var d=this.dataset;this.outerHTML=avatarInitials(d.name,parseInt(d.size))" />`;
  }
  return avatarInitials(name, size);
}

function avatarInitials(name, size) {
  const initials = (name||"?").split(" ").map(s=>s[0]).join("").substring(0,2).toUpperCase();
  const colors = ["#25D366","#0088cc","#E4405F","#006AFF","#EA4335","#7b68ee","#20c997","#fd7e14"];
  const i = (name||"").length % colors.length;
  return `<div class="avatar-initials" style="width:${size}px;height:${size}px;background:${colors[i]}">${initials}</div>`;
}

function statusIcon(status, direction) {
  if (direction !== "Outbound") return "";
  if (status === "Sent") return '<i class="bi bi-check" style="font-size:14px;color:#8696a0"></i>';
  if (status === "Delivered") return '<i class="bi bi-check-all" style="font-size:14px;color:#8696a0"></i>';
  if (status === "Read") return '<i class="bi bi-check-all" style="font-size:14px;color:#53bdeb"></i>';
  if (status === "Failed") return '<i class="bi bi-exclamation-circle" style="font-size:14px;color:#ef5350"></i>';
  return '<i class="bi bi-clock" style="font-size:14px;color:#8696a0"></i>';
}

function formatFileSize(bytes) {
  if (!bytes) return "";
  if (bytes < 1024) return bytes + " B";
  if (bytes < 1048576) return (bytes / 1024).toFixed(1) + " KB";
  return (bytes / 1048576).toFixed(1) + " MB";
}

function getDocIcon(name) {
  const ext = (name||"").split('.').pop().toLowerCase();
  if (["pdf"].includes(ext)) return "bi-filetype-pdf";
  if (["doc","docx"].includes(ext)) return "bi-filetype-docx";
  if (["xls","xlsx"].includes(ext)) return "bi-filetype-xlsx";
  if (["ppt","pptx"].includes(ext)) return "bi-filetype-pptx";
  if (["zip","rar","7z"].includes(ext)) return "bi-file-zip";
  if (["txt"].includes(ext)) return "bi-file-text";
  return "bi-file-earmark";
}

function formatDate(d) {
  if (!d) return "";
  const dt = new Date(d);
  const today = new Date();
  const yesterday = new Date(today); yesterday.setDate(yesterday.getDate()-1);
  if (dt.toDateString() === today.toDateString()) return "";
  if (dt.toDateString() === yesterday.toDateString()) return "Yesterday";
  return dt.toLocaleDateString("en-US", { weekday:'long', month:'short', day:'numeric' });
}

function needsDateSeparator(prev, curr) {
  if (!prev) return true;
  const a = new Date(prev.createdAt), b = new Date(curr.createdAt);
  return a.toDateString() !== b.toDateString();
}

function formatMsgTime(d) {
  if (!d) return "";
  return new Date(d).toLocaleTimeString("en-US", { hour:'2-digit', minute:'2-digit' });
}

// ===== MEDIA RENDERING =====
function renderMediaContent(m) {
  const mt = m.messageType || "text";
  const url = m.mediaUrl;
  if (!url || mt === "text" || mt === "template") return { mediaHtml: "", hasMedia: false };

  if (mt === "image") {
    return {
      mediaHtml: `<div class="msg-image-wrap" onclick="event.stopPropagation();openLightbox('${esc(url)}')"><img src="${esc(url)}" alt="Image" loading="lazy" /></div>`,
      hasMedia: true
    };
  }
  if (mt === "video") {
    return {
      mediaHtml: `<div class="msg-video-wrap"><video src="${esc(url)}" preload="metadata" onclick="event.stopPropagation();this.paused?this.play():this.pause()"></video><div class="msg-video-play" onclick="event.stopPropagation();this.previousElementSibling.play();this.style.display='none'"><i class="bi bi-play-fill"></i></div></div>`,
      hasMedia: true
    };
  }
  if (mt === "audio") {
    return {
      mediaHtml: `<div class="msg-audio-wrap"><audio src="${esc(url)}" controls preload="none"></audio></div>`,
      hasMedia: true
    };
  }
  if (mt === "document") {
    const fname = m.fileName || m.mediaUrl?.split('/').pop() || "Document";
    const icon = getDocIcon(fname);
    return {
      mediaHtml: `<div class="msg-document-wrap" onclick="event.stopPropagation();window.open('${esc(url)}','_blank')"><div class="msg-document-icon"><i class="bi ${icon}"></i></div><div class="msg-document-info"><div class="msg-document-name">${esc(fname)}</div><div class="msg-document-size">${m.fileSize ? formatFileSize(m.fileSize) : 'Document'}</div></div><i class="bi bi-download" style="font-size:18px;opacity:0.6"></i></div>`,
      hasMedia: true
    };
  }
  return { mediaHtml: "", hasMedia: false };
}

function openLightbox(url) {
  const lb = document.createElement("div");
  lb.className = "msg-lightbox";
  lb.onclick = () => lb.remove();
  lb.innerHTML = `<img src="${esc(url)}" />`;
  document.body.appendChild(lb);
}

// ===== INBOX =====
async function loadInbox() {
  try {
    const res = await fetch("/api/conversations");
    convs = await res.json();
    renderInbox();
  } catch(e) { console.error(e); }
}

function profilePicUrl(c) {
  return c.avatarUrl || (c.remoteJid ? `/api/contacts/profile-picture?jid=${encodeURIComponent(c.remoteJid)}` : null);
}

function contactPicUrl(c) {
  if (c.avatarUrl) return c.avatarUrl;
  const phone = (c.phone || "").replace(/[^0-9]/g, "");
  if (phone) return `/api/contacts/profile-picture?jid=${encodeURIComponent(phone + "@s.whatsapp.net")}`;
  return null;
}

function renderInbox() {
  const list = document.getElementById("inboxList");
  if (!convs.length) { list.innerHTML = '<div class="text-center text-secondary p-4"><i class="bi bi-chat-dots fs-1 d-block mb-2"></i>No conversations</div>'; return; }
  list.innerHTML = convs.map(c => {
    const active = currentConvId === c.id ? 'active' : '';
    const dot = c.isOnline ? '<span class="online-dot"></span>' : '';
    const typing = c.isTyping ? '<div class="typing-indicator"><span></span><span></span><span></span></div>' : '';
    const preview = c.isTyping ? '<em class="text-success" style="font-size:12px">typing...</em>' : esc((c.lastMessage||'').substring(0,50));
    const picUrl = profilePicUrl(c);
    return `<div class="conv-item ${active}" onclick="selectConv('${c.id}')">
      <div class="conv-avatar">${avatar(c.contactName, picUrl, 44)}${dot}</div>
      <div class="conv-body"><div class="d-flex justify-content-between"><strong class="small">${esc(c.contactName)}</strong><small class="text-secondary">${timeAgo(c.lastMessageAt)}</small></div>
      <div class="small text-secondary text-truncate">${preview}</div>
      <div><span class="badge badge-${c.channelType||'whatsapp'}">${c.channelType||'whatsapp'}</span> <small class="text-primary">${c.messageCount||0} msgs</small></div></div>
    </div>`;
  }).join("");
}

async function selectConv(id) {
  currentConvId = id;
  const c = convs.find(x => x.id === id);
  document.getElementById("inboxHeader").classList.remove("d-none");
  document.getElementById("inboxInput").classList.remove("d-none");
  document.getElementById("inboxContactAvatar").innerHTML = avatar(c?.contactName, profilePicUrl(c), 36);
  const dot = c?.isOnline ? '<span class="online-dot ms-1"></span>' : '';
  document.getElementById("inboxContactName").innerHTML = `${esc(c?.contactName||"")}${dot}`;
  const statusText = c?.isOnline ? 'Online' : (c?.lastSeenAt ? 'Last seen '+timeAgo(c.lastSeenAt) : '');
  document.getElementById("inboxContactInfo").textContent = (c?.contactPhone||"") + " \u00b7 " + (c?.channelType||"whatsapp");
  document.getElementById("inboxContactStatus").textContent = statusText;
  renderInbox();
  document.getElementById("inboxMain").classList.add("show");
  document.getElementById("inboxSidebar").classList.add("hide");
  document.getElementById("inboxMessages").innerHTML = '<div class="text-center text-secondary p-4"><div class="spinner-border spinner-border-sm"></div></div>';
  try {
    const res = await fetch(`/api/conversations/${id}/messages`);
    const msgs = await res.json();
    renderMessages(msgs);
  } catch(e) { console.error(e); }
}

function renderMessages(msgs) {
  const v = document.getElementById("inboxMessages");
  let html = "";
  for (let i = 0; i < msgs.length; i++) {
    const m = msgs[i];
    const prev = i > 0 ? msgs[i-1] : null;
    if (needsDateSeparator(prev, m)) {
      const label = formatDate(m.createdAt);
      if (label) html += `<div class="date-separator"><span>${label}</span></div>`;
    }
    html += renderMsgBubble(m);
  }
  v.innerHTML = html;
  v.scrollTop = v.scrollHeight;
}

function renderMsgBubble(m) {
  if (m.isDeletedForMe && m.direction !== "Inbound") return '';
  const dir = m.direction === "Inbound" ? "msg-inbound" : "msg-outbound";
  const rc = (m.reactions||[]).map(r => `<span class="reaction-badge" onclick="event.stopPropagation();removeReaction('${m.id}','${r.emoji}')">${r.emoji}</span>`).join('');
  const hasRc = rc ? `<div class="msg-reactions">${rc}</div>` : '';
  const st = statusIcon(m.status, m.direction);
  const edited = m.isEdited ? '<span class="msg-edited" title="Edited"> edited</span>' : '';
  const tm = formatMsgTime(m.createdAt) + edited + ' ' + st;
  const media = renderMediaContent(m);

  // Reply indicator
  let replyHtml = '';
  if (m.replyTo) {
    const replyDir = m.replyTo.direction === "Inbound" ? "Inbound" : "Outbound";
    replyHtml = `<div class="msg-reply-preview" onclick="event.stopPropagation();scrollToMsg('${m.replyToId}')">
      <div class="msg-reply-bar ${replyDir === 'Inbound' ? 'reply-inbound' : 'reply-outbound'}"></div>
      <div class="msg-reply-content">
        <div class="msg-reply-sender">${esc(replyDir === 'Inbound' ? 'Contact' : 'You')}</div>
        <div class="msg-reply-text">${esc((m.replyTo.content||'').substring(0,80))}</div>
      </div>
    </div>`;
  }

  const textHtml = m.content && m.content !== "🎵 Audio" && m.content !== "📷 Image" && m.content !== "🎥 Video" && !m.content.startsWith("📄 ") ? `<div class="msg-text">${esc(m.content)}</div>` : '';
  const dataId = `msg-${m.id}`;
  const isOutbound = m.direction === "Outbound";
  const pmId = m.providerMessageId || "";
  const rJid = m.remoteJid || "";

  return `<div class="msg-bubble ${dir}" id="${dataId}" data-provider-message-id="${esc(pmId)}" data-remote-jid="${esc(rJid)}" onclick="showReactionPicker(event,'${m.id}')" oncontextmenu="showMsgContextMenu(event,'${m.id}')">
    ${replyHtml}${media.mediaHtml}${textHtml}${hasRc}<div class="msg-meta">${tm}</div></div>`;
}

function appendMsg(msg) {
  const v = document.getElementById("inboxMessages");
  const lastMsg = v.querySelector(".msg-bubble:last-of-type");
  if (!lastMsg && v.querySelector(".empty-state")) v.innerHTML = "";
  const html = renderMsgBubble(msg);
  v.insertAdjacentHTML("beforeend", html);
  v.scrollTop = v.scrollHeight;
}

// ===== SEND MESSAGE WITH MEDIA SUPPORT =====
async function sendInboxMsg() {
  const input = document.getElementById("msgInput");
  const content = input.value.trim();
  if (!content && !pendingAttachment) return;
  if (!currentConvId) return;
  const c = convs.find(x => x.id === currentConvId);
  if (!c) return;

  input.value = "";
  const replyBar = document.getElementById("replyBar");
  const replyToId = replyBar?.dataset.replyToId || null;
  const body = { content: content || "", contactName: c.contactName, contactPhone: c.contactPhone, replyToId };
  cancelReply();

  if (pendingAttachment) {
    body.messageType = pendingAttachment.type;
    body.mediaUrl = pendingAttachment.url;
    body.fileName = pendingAttachment.name;
    body.mimeType = pendingAttachment.mimeType;
    // If no text content, set a default caption
    if (!body.content) body.content = pendingAttachment.type === "image" ? "📷 Image" : pendingAttachment.type === "video" ? "🎥 Video" : pendingAttachment.type === "audio" ? "🎵 Audio" : `📄 ${pendingAttachment.name}`;
  }

  clearAttachmentPreview();

  try {
    await fetch(`/api/conversations/${c.remoteJid}/messages`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body)
    });
    selectConv(currentConvId);
  } catch(e) { console.error(e); }
}

// ===== ATTACHMENT =====
function toggleAttachMenu() {
  document.getElementById("attachMenu").classList.toggle("show");
}

function triggerFilePicker(type) {
  document.getElementById("attachMenu").classList.remove("show");
  const input = document.getElementById("fileInput");
  input.accept = type === "image" ? "image/*" : type === "video" ? "video/*" : type === "audio" ? "audio/*" : "*/*";
  input.dataset.mediaType = type;
  input.value = "";
  input.click();
}

async function handleFileSelected(e) {
  const file = e.target.files[0];
  if (!file) return;
  const mediaType = e.target.dataset.mediaType || "document";

  // Show preview
  showAttachmentPreview(file, mediaType);

  // Upload
  try {
    const formData = new FormData();
    formData.append("file", file);
    const res = await fetch("/api/upload", { method: "POST", body: formData });
    const data = await res.json();
    pendingAttachment = { url: data.url, name: data.originalName, type: mediaType, mimeType: data.contentType, size: data.size };
  } catch(err) {
    console.error("Upload failed:", err);
    alert("Failed to upload file");
    clearAttachmentPreview();
  }
}

function showAttachmentPreview(file, type) {
  const el = document.getElementById("attachmentPreview");
  el.style.display = "flex";
  const thumb = document.getElementById("attachPreviewThumb");
  const info = document.getElementById("attachPreviewInfo");
  const icons = { image:"bi-image", video:"bi-film", audio:"bi-music-note", document:"bi-file-earmark" };
  if (type === "image") {
    const url = URL.createObjectURL(file);
    thumb.innerHTML = `<img src="${url}" class="preview-thumb" />`;
  } else {
    thumb.innerHTML = `<div class="preview-thumb" style="display:flex;align-items:center;justify-content:center;font-size:20px;background:var(--bs-tertiary-bg);border-radius:6px"><i class="bi ${icons[type]||icons.document}"></i></div>`;
  }
  info.innerHTML = `<div class="preview-name">${esc(file.name)}</div><div class="text-secondary" style="font-size:12px">${formatFileSize(file.size)}</div>`;
  document.getElementById("msgInput").placeholder = type === "image" ? "Add a caption..." : "Add a message...";
}

function clearAttachmentPreview() {
  pendingAttachment = null;
  document.getElementById("attachmentPreview").style.display = "none";
  document.getElementById("msgInput").placeholder = "Type a message...";
}

// ===== REACTIONS =====
function showReactionPicker(e, msgId) {
  const existing = document.getElementById("reactionPicker");
  if (existing) existing.remove();
  const rect = e.currentTarget.getBoundingClientRect();
  const picker = document.createElement("div");
  picker.id = "reactionPicker";
  picker.className = "reaction-picker";
  picker.style.top = Math.max(rect.top - 56, 10) + "px";
  picker.style.left = Math.min(rect.left, window.innerWidth - 320) + "px";
  const emojis = ["👍","❤️","😂","😮","😢","🙏","🔥","🎉"];
  picker.innerHTML = emojis.map(e => `<span onclick="addReaction('${msgId}','${e}');this.parentElement.remove()">${e}</span>`).join('');
  document.body.appendChild(picker);
  setTimeout(() => picker.remove(), 3000);
}

async function addReaction(msgId, emoji) {
  const msgEl = document.getElementById("msg-" + msgId);
  const remoteJid = msgEl?.dataset.remoteJid || "";
  const messageKeyId = msgEl?.dataset.providerMessageId || "";
  try {
    await fetch(`/api/messages/${msgId}/react`, { method:"POST", headers:{"Content-Type":"application/json"}, body:JSON.stringify({emoji, senderJid:"web-user", senderName:"You", remoteJid, messageKeyId}) });
  } catch(e) { console.error(e); }
}

async function removeReaction(msgId, emoji) {
  const msgEl = document.getElementById("msg-" + msgId);
  const remoteJid = msgEl?.dataset.remoteJid || "";
  const messageKeyId = msgEl?.dataset.providerMessageId || "";
  try {
    await fetch(`/api/messages/${msgId}/react`, { method:"POST", headers:{"Content-Type":"application/json"}, body:JSON.stringify({emoji: "", senderJid:"web-user", senderName:"You", remoteJid, messageKeyId}) });
  } catch(e) { console.error(e); }
}

// ===== TYPING =====
let typingTimeout = null;
function onTyping() {
  if (!currentConvId) return;
  const c = convs.find(x => x.id === currentConvId);
  if (!c) return;
  clearTimeout(typingTimeout);
  typingTimeout = setTimeout(() => {
    connection.invoke("Typing", c.remoteJid, false).catch(()=>{});
  }, 2000);
  connection.invoke("Typing", c.remoteJid, true).catch(()=>{});
}

function showTyping(remoteJid) {
  const v = document.getElementById("inboxMessages");
  let el = document.getElementById("typingBubble");
  if (!el) {
    el = document.createElement("div");
    el.id = "typingBubble";
    el.className = "typing-bubble";
    el.innerHTML = '<div class="typing-indicator"><span></span><span></span><span></span></div>';
    v.appendChild(el);
  }
  v.scrollTop = v.scrollHeight;
}

function hideTyping(remoteJid) {
  const el = document.getElementById("typingBubble");
  if (el) el.remove();
}

function toggleInboxSidebar() { document.getElementById("inboxSidebar").classList.toggle("hide"); document.getElementById("inboxMain").classList.toggle("show"); }

function filterInbox(q) {
  q = q.toLowerCase();
  document.querySelectorAll(".conv-item").forEach((el,i) => {
    el.style.display = convs[i]?.contactName?.toLowerCase().includes(q) ? "" : "none";
  });
}

async function showAssignModal() {
  try {
    const r = await fetch("/api/team/agents");
    const agents = await r.json();
    const s = document.getElementById("assignAgentSelect");
    s.innerHTML = agents.map(a => `<option value="${a.id}">${esc(a.name)} (${a.role})</option>`).join("");
    new bootstrap.Modal(document.getElementById("assignModal")).show();
  } catch(e) { console.error(e); }
}
async function assignConversation() {
  const agentId = document.getElementById("assignAgentSelect").value;
  await fetch("/api/team/assign", { method:"POST", headers:{"Content-Type":"application/json"}, body:JSON.stringify({conversationId:currentConvId,agentId}) });
  bootstrap.Modal.getInstance(document.getElementById("assignModal")).hide();
}

// ===== CONTACTS =====
async function loadContacts(search) {
  try {
    const url = "/api/contacts" + (search ? "?search="+encodeURIComponent(search) : "");
    const r = await fetch(url);
    const contacts = await r.json();
    document.getElementById("contactsTable").innerHTML = contacts.map(c => `<tr>
      <td>${avatar(c.name, contactPicUrl(c), 32)} <strong>${esc(c.name)}</strong></td><td>${esc(c.phone||'-')}</td><td>${esc(c.email||'-')}</td>
      <td>${esc(c.company||'-')}</td><td><span class="badge bg-info">${c.lifecycleStage||'lead'}</span></td>
      <td>${(c.tags||[]).map(t => `<span class="badge bg-secondary me-1">${t}</span>`).join('')}</td>
      <td><small class="text-secondary">${c.lastActivityAt ? timeAgo(c.lastActivityAt) : '-'}</small></td>
    </tr>`).join("");
  } catch(e) { console.error(e); }
}

function showAddContactModal() { new bootstrap.Modal(document.getElementById("addContactModal")).show(); }
async function createContact() {
  const body = { name:document.getElementById("cName").value, phone:document.getElementById("cPhone").value, email:document.getElementById("cEmail").value, company:document.getElementById("cCompany").value, notes:document.getElementById("cNotes").value };
  await fetch("/api/contacts", { method:"POST", headers:{"Content-Type":"application/json"}, body:JSON.stringify(body) });
  bootstrap.Modal.getInstance(document.getElementById("addContactModal")).hide();
  document.getElementById("cName").value = ""; document.getElementById("cPhone").value = ""; document.getElementById("cEmail").value = ""; document.getElementById("cCompany").value = ""; document.getElementById("cNotes").value = "";
  loadContacts();
}

// ===== CAMPAIGNS =====
async function loadCampaigns() {
  try {
    const r = await fetch("/api/campaigns");
    const list = await r.json();
    document.getElementById("campaignsGrid").innerHTML = list.map(c => `<div class="col-md-4 mb-3"><div class="card">
      <div class="card-body"><h6>${esc(c.name)}</h6>
      <div class="small text-secondary mb-2">Status: <span class="badge bg-${c.status==='SENT'?'success':c.status==='SENDING'?'warning':'secondary'}">${c.status}</span></div>
      <div class="small">Sent: ${c.deliveredCount||0}/${c.totalRecipients||0} | Opened: ${c.openedCount||0}</div>
      ${c.status==='DRAFT'?`<button class="btn btn-primary btn-sm mt-2" onclick="sendCampaign('${c.id}')"><i class="bi bi-send"></i> Send Now</button>`:''}
      <small class="text-secondary d-block mt-1">${c.createdAt ? new Date(c.createdAt).toLocaleDateString() : ''}</small>
    </div></div></div>`).join("");
  } catch(e) { console.error(e); }
}

function showCreateCampaignModal() { new bootstrap.Modal(document.getElementById("campaignModal")).show(); }
async function createCampaign() {
  const body = { name:document.getElementById("campName").value, scheduledAt:document.getElementById("campSchedule").value || null };
  await fetch("/api/campaigns", { method:"POST", headers:{"Content-Type":"application/json"}, body:JSON.stringify(body) });
  bootstrap.Modal.getInstance(document.getElementById("campaignModal")).hide();
  document.getElementById("campName").value = ""; document.getElementById("campSchedule").value = "";
  loadCampaigns();
}
async function sendCampaign(id) {
  await fetch(`/api/campaigns/${id}/send`, { method:"POST" });
  loadCampaigns();
}

// ===== TEMPLATES (WhatsApp-style with preview & send) =====
// ===== TWILIO-STYLE TEMPLATE BUILDER =====
let currentEditingTemplateId = null;

function selectContentType(type) {
  document.querySelectorAll(".template-type-item").forEach(el => el.classList.toggle("active", el.dataset.type === type));
  document.getElementById("tbContentType").dataset.selected = type;
  renderConfigFields(type);
  updateBuilderPreview();
}

function renderConfigFields(type) {
  const area = document.getElementById("tbConfigArea");
  switch(type) {
    case "twilio/text":
      area.innerHTML = `
        <div class="mb-2"><label class="form-label small">Body</label><textarea class="form-control form-control-sm" id="cfgBody" rows="5" placeholder="Message text, use {{1}} for variables" oninput="updateBuilderPreview()"></textarea></div>`;
      document.getElementById("tbActionsArea").style.display = "none";
      document.getElementById("tbItemsArea").style.display = "none";
      document.getElementById("tbCardsArea").style.display = "none";
      break;
    case "twilio/media":
      area.innerHTML = `
        <div class="mb-2"><label class="form-label small">Media URL</label><input class="form-control form-control-sm" id="cfgMedia" placeholder="https://example.com/image.jpg" oninput="updateBuilderPreview()" /></div>
        <div class="mb-2"><label class="form-label small">Body (optional)</label><textarea class="form-control form-control-sm" id="cfgBody" rows="3" placeholder="Caption text" oninput="updateBuilderPreview()"></textarea></div>`;
      document.getElementById("tbActionsArea").style.display = "none";
      document.getElementById("tbItemsArea").style.display = "none";
      document.getElementById("tbCardsArea").style.display = "none";
      break;
    case "twilio/quick-reply":
      area.innerHTML = `
        <div class="mb-2"><label class="form-label small">Body</label><textarea class="form-control form-control-sm" id="cfgBody" rows="3" placeholder="Message above buttons" oninput="updateBuilderPreview()"></textarea></div>`;
      document.getElementById("tbActionsArea").style.display = "block";
      document.getElementById("tbItemsArea").style.display = "none";
      document.getElementById("tbCardsArea").style.display = "none";
      renderActions("QUICK_REPLY");
      break;
    case "twilio/call-to-action":
      area.innerHTML = `
        <div class="mb-2"><label class="form-label small">Body</label><textarea class="form-control form-control-sm" id="cfgBody" rows="3" placeholder="Message above buttons" oninput="updateBuilderPreview()"></textarea></div>`;
      document.getElementById("tbActionsArea").style.display = "block";
      document.getElementById("tbItemsArea").style.display = "none";
      document.getElementById("tbCardsArea").style.display = "none";
      renderActions("CTA");
      break;
    case "twilio/list-picker":
      area.innerHTML = `
        <div class="mb-2"><label class="form-label small">Body</label><textarea class="form-control form-control-sm" id="cfgBody" rows="3" placeholder="Message above list" oninput="updateBuilderPreview()"></textarea></div>
        <div class="mb-2"><label class="form-label small">Button Text</label><input class="form-control form-control-sm" id="cfgButton" placeholder="Select an option" oninput="updateBuilderPreview()" /></div>`;
      document.getElementById("tbActionsArea").style.display = "none";
      document.getElementById("tbItemsArea").style.display = "block";
      document.getElementById("tbCardsArea").style.display = "none";
      renderListItems();
      break;
    case "twilio/card":
      area.innerHTML = `
        <div class="mb-2"><label class="form-label small">Title</label><input class="form-control form-control-sm" id="cfgTitle" placeholder="Card title" oninput="updateBuilderPreview()" /></div>
        <div class="mb-2"><label class="form-label small">Body</label><textarea class="form-control form-control-sm" id="cfgBody" rows="2" placeholder="Card body text" oninput="updateBuilderPreview()"></textarea></div>
        <div class="mb-2"><label class="form-label small">Subtitle</label><input class="form-control form-control-sm" id="cfgSubtitle" placeholder="Subtitle" oninput="updateBuilderPreview()" /></div>
        <div class="mb-2"><label class="form-label small">Media URL</label><input class="form-control form-control-sm" id="cfgMedia" placeholder="https://example.com/image.jpg" oninput="updateBuilderPreview()" /></div>`;
      document.getElementById("tbActionsArea").style.display = "block";
      document.getElementById("tbItemsArea").style.display = "none";
      document.getElementById("tbCardsArea").style.display = "none";
      renderActions("CARD");
      break;
    case "whatsapp/card":
      area.innerHTML = `
        <div class="mb-2"><label class="form-label small">Body</label><textarea class="form-control form-control-sm" id="cfgBody" rows="3" placeholder="Card body" oninput="updateBuilderPreview()"></textarea></div>
        <div class="mb-2"><label class="form-label small">Header Text</label><input class="form-control form-control-sm" id="cfgHeader" placeholder="Bolded header" oninput="updateBuilderPreview()" /></div>
        <div class="mb-2"><label class="form-label small">Footer</label><input class="form-control form-control-sm" id="cfgFooter" placeholder="Footer text" oninput="updateBuilderPreview()" /></div>
        <div class="mb-2"><label class="form-label small">Media URL</label><input class="form-control form-control-sm" id="cfgMedia" placeholder="https://example.com/image.jpg" oninput="updateBuilderPreview()" /></div>`;
      document.getElementById("tbActionsArea").style.display = "block";
      document.getElementById("tbItemsArea").style.display = "none";
      document.getElementById("tbCardsArea").style.display = "none";
      renderActions("WHATSAPP_CARD");
      break;
    case "twilio/carousel":
      area.innerHTML = `
        <div class="mb-2"><label class="form-label small">Body (above carousel)</label><textarea class="form-control form-control-sm" id="cfgBody" rows="2" placeholder="Message above carousel" oninput="updateBuilderPreview()"></textarea></div>`;
      document.getElementById("tbActionsArea").style.display = "none";
      document.getElementById("tbItemsArea").style.display = "none";
      document.getElementById("tbCardsArea").style.display = "block";
      renderCards();
      break;
    case "twilio/location":
      area.innerHTML = `
        <div class="mb-2"><label class="form-label small">Latitude</label><input class="form-control form-control-sm" id="cfgLat" type="number" step="any" placeholder="37.6216" oninput="updateBuilderPreview()" /></div>
        <div class="mb-2"><label class="form-label small">Longitude</label><input class="form-control form-control-sm" id="cfgLng" type="number" step="any" placeholder="-122.3789" oninput="updateBuilderPreview()" /></div>
        <div class="mb-2"><label class="form-label small">Label</label><input class="form-control form-control-sm" id="cfgLabel" placeholder="Location label" oninput="updateBuilderPreview()" /></div>`;
      document.getElementById("tbActionsArea").style.display = "none";
      document.getElementById("tbItemsArea").style.display = "none";
      document.getElementById("tbCardsArea").style.display = "none";
      break;
    // Baileys-native types
    case "baileys/buttons":
      area.innerHTML = `
        <div class="mb-2"><label class="form-label small">Body</label><textarea class="form-control form-control-sm" id="cfgBody" rows="3" placeholder="Message above buttons" oninput="updateBuilderPreview()"></textarea></div>`;
      document.getElementById("tbActionsArea").style.display = "block";
      document.getElementById("tbItemsArea").style.display = "none";
      document.getElementById("tbCardsArea").style.display = "none";
      renderActions("QUICK_REPLY");
      break;
    case "baileys/list":
      area.innerHTML = `
        <div class="mb-2"><label class="form-label small">Body</label><textarea class="form-control form-control-sm" id="cfgBody" rows="3" placeholder="Message above list" oninput="updateBuilderPreview()"></textarea></div>
        <div class="mb-2"><label class="form-label small">Button Text</label><input class="form-control form-control-sm" id="cfgButton" placeholder="Select an option" oninput="updateBuilderPreview()" /></div>`;
      document.getElementById("tbActionsArea").style.display = "none";
      document.getElementById("tbItemsArea").style.display = "block";
      document.getElementById("tbCardsArea").style.display = "none";
      renderListItems();
      break;
    case "baileys/template":
      area.innerHTML = `
        <div class="mb-2"><label class="form-label small">Body</label><textarea class="form-control form-control-sm" id="cfgBody" rows="3" placeholder="Message above buttons" oninput="updateBuilderPreview()"></textarea></div>`;
      document.getElementById("tbActionsArea").style.display = "block";
      document.getElementById("tbItemsArea").style.display = "none";
      document.getElementById("tbCardsArea").style.display = "none";
      renderActions("CTA");
      break;
    case "baileys/sticker":
      area.innerHTML = `
        <div class="mb-2"><label class="form-label small">Sticker URL</label><input class="form-control form-control-sm" id="cfgMedia" placeholder="https://example.com/sticker.webp" oninput="updateBuilderPreview()" /></div>
        <div class="mb-2"><label class="form-label small">Body (optional)</label><textarea class="form-control form-control-sm" id="cfgBody" rows="2" placeholder="Caption text" oninput="updateBuilderPreview()"></textarea></div>`;
      document.getElementById("tbActionsArea").style.display = "none";
      document.getElementById("tbItemsArea").style.display = "none";
      document.getElementById("tbCardsArea").style.display = "none";
      break;
  }
}

let actionCount = 0, itemCount = 0, cardCount = 0;

function renderActions(mode) {
  const list = document.getElementById("tbActionList");
  actionCount = 0;
  list.innerHTML = `
    <div class="action-row mb-2 p-2 border rounded" data-idx="0">
      <div class="row g-1">
        <div class="col-3">
          <select class="form-select form-select-sm action-type" onchange="updateBuilderPreview()">
            ${mode === "QUICK_REPLY" ? '<option value="QUICK_REPLY">Quick Reply</option>' : ''}
            ${mode === "CTA" || mode === "CARD" || mode === "WHATSAPP_CARD" ? '<option value="URL">URL</option><option value="PHONE_NUMBER">Phone</option><option value="QUICK_REPLY">Quick Reply</option>' : ''}
            ${mode === "CTA" || mode === "CARD" || mode === "WHATSAPP_CARD" ? '<option value="COPY_CODE">Copy Code</option>' : ''}
          </select>
        </div>
        <div class="col"><input class="form-control form-control-sm action-title" placeholder="Button text" oninput="updateBuilderPreview()" /></div>
        <div class="col action-extra"><input class="form-control form-control-sm action-url" placeholder="URL" oninput="updateBuilderPreview()" /></div>
        <div class="col-auto"><button class="btn btn-outline-danger btn-sm" onclick="removeAction(this)"><i class="bi bi-x"></i></button></div>
      </div>
    </div>`;
}

function removeAction(btn) { btn.closest(".action-row").remove(); updateBuilderPreview(); }

function addAction() {
  const list = document.getElementById("tbActionList");
  const mode = document.getElementById("tbContentType").dataset.selected;
  const isQR = mode === "twilio/quick-reply";
  const isCTA = mode === "twilio/call-to-action" || mode === "twilio/card" || mode === "whatsapp/card";
  list.innerHTML += `
    <div class="action-row mb-2 p-2 border rounded" data-idx="${++actionCount}">
      <div class="row g-1">
        <div class="col-3">
          <select class="form-select form-select-sm action-type" onchange="updateBuilderPreview()">
            ${isQR ? '<option value="QUICK_REPLY">Quick Reply</option>' : ''}
            ${isCTA ? '<option value="URL">URL</option><option value="PHONE_NUMBER">Phone</option><option value="QUICK_REPLY">Quick Reply</option><option value="COPY_CODE">Copy Code</option>' : ''}
          </select>
        </div>
        <div class="col"><input class="form-control form-control-sm action-title" placeholder="Button text" oninput="updateBuilderPreview()" /></div>
        <div class="col action-extra"><input class="form-control form-control-sm action-url" placeholder="URL or phone" oninput="updateBuilderPreview()" /></div>
        <div class="col-auto"><button class="btn btn-outline-danger btn-sm" onclick="removeAction(this)"><i class="bi bi-x"></i></button></div>
      </div>
    </div>`;
}

function renderListItems() {
  const list = document.getElementById("tbItemList");
  itemCount = 0;
  list.innerHTML = `
    <div class="item-row mb-2 p-2 border rounded" data-idx="0">
      <div class="row g-1">
        <div class="col-4"><input class="form-control form-control-sm item-title" placeholder="Title" oninput="updateBuilderPreview()" /></div>
        <div class="col-4"><input class="form-control form-control-sm item-desc" placeholder="Description" oninput="updateBuilderPreview()" /></div>
        <div class="col"><input class="form-control form-control-sm item-id" placeholder="ID" oninput="updateBuilderPreview()" /></div>
        <div class="col-auto"><button class="btn btn-outline-danger btn-sm" onclick="removeListItem(this)"><i class="bi bi-x"></i></button></div>
      </div>
    </div>`;
}

function removeListItem(btn) { btn.closest(".item-row").remove(); updateBuilderPreview(); }

function addListItem() {
  const list = document.getElementById("tbItemList");
  list.innerHTML += `
    <div class="item-row mb-2 p-2 border rounded" data-idx="${++itemCount}">
      <div class="row g-1">
        <div class="col-4"><input class="form-control form-control-sm item-title" placeholder="Title" oninput="updateBuilderPreview()" /></div>
        <div class="col-4"><input class="form-control form-control-sm item-desc" placeholder="Description" oninput="updateBuilderPreview()" /></div>
        <div class="col"><input class="form-control form-control-sm item-id" placeholder="ID" oninput="updateBuilderPreview()" /></div>
        <div class="col-auto"><button class="btn btn-outline-danger btn-sm" onclick="removeListItem(this)"><i class="bi bi-x"></i></button></div>
      </div>
    </div>`;
}

function renderCards() {
  const list = document.getElementById("tbCardList");
  cardCount = 0;
  list.innerHTML = `<div class="text-muted small mb-2">Add at least one card to the carousel</div>`;
}

function addCard() {
  const list = document.getElementById("tbCardList");
  const idx = cardCount++;
  list.innerHTML += `
    <div class="card-row mb-2 p-2 border rounded" data-idx="${idx}">
      <div class="row g-1 mb-1">
        <div class="col-6"><input class="form-control form-control-sm card-title" placeholder="Card title" oninput="updateBuilderPreview()" /></div>
        <div class="col"><input class="form-control form-control-sm card-media" placeholder="Media URL" oninput="updateBuilderPreview()" /></div>
        <div class="col-auto"><button class="btn btn-outline-danger btn-sm" onclick="removeCard(this)"><i class="bi bi-x"></i></button></div>
      </div>
      <div class="mb-1"><textarea class="form-control form-control-sm card-body" rows="2" placeholder="Card body" oninput="updateBuilderPreview()"></textarea></div>
      <div class="card-actions">
        <div class="row g-1 mb-1 card-action-row" data-idx="0">
          <div class="col-3">
            <select class="form-select form-select-sm ca-type"><option value="QUICK_REPLY">Quick Reply</option><option value="URL">URL</option></select>
          </div>
          <div class="col"><input class="form-control form-control-sm ca-title" placeholder="Button text" /></div>
          <div class="col ca-extra"><input class="form-control form-control-sm ca-url" placeholder="URL" /></div>
          <div class="col-auto"><button class="btn btn-outline-danger btn-sm" onclick="removeCardAction(this)"><i class="bi bi-x"></i></button></div>
        </div>
        <button class="btn btn-outline-secondary btn-sm" onclick="addCardAction(this)"><i class="bi bi-plus"></i> Button</button>
      </div>
    </div>`;
  updateBuilderPreview();
}

function removeCard(btn) { btn.closest(".card-row").remove(); updateBuilderPreview(); }

function addCardAction(btn) {
  const container = btn.closest(".card-actions");
  const row = container.querySelector(".card-action-row").cloneNode(true);
  row.querySelectorAll("input").forEach(i => i.value = "");
  const addBtn = container.querySelector("button:last-child");
  container.insertBefore(row, addBtn);
  updateBuilderPreview();
}

function removeCardAction(btn) { btn.closest(".card-action-row").remove(); updateBuilderPreview(); }

// Build the Types JSON from current form
function buildTypesJson() {
  const type = document.getElementById("tbContentType").dataset.selected || "twilio/text";
  const getVal = (id) => document.getElementById(id)?.value || "";
  const types = {};
  switch(type) {
    case "twilio/text":
      types["twilio/text"] = { body: getVal("cfgBody") };
      break;
    case "twilio/media":
      types["twilio/media"] = { body: getVal("cfgBody") || null, media: [getVal("cfgMedia")].filter(Boolean) };
      break;
    case "twilio/quick-reply":
      types["twilio/quick-reply"] = { body: getVal("cfgBody"), actions: collectActions("QUICK_REPLY") };
      break;
    case "twilio/call-to-action":
      types["twilio/call-to-action"] = { body: getVal("cfgBody"), actions: collectActions("CTA") };
      break;
    case "twilio/list-picker":
      types["twilio/list-picker"] = { body: getVal("cfgBody"), button: getVal("cfgButton"), items: collectItems() };
      break;
    case "twilio/card":
      types["twilio/card"] = { title: getVal("cfgTitle") || null, body: getVal("cfgBody") || null, subtitle: getVal("cfgSubtitle") || null, media: getVal("cfgMedia") ? [getVal("cfgMedia")] : null, actions: collectActions("CARD") };
      break;
    case "whatsapp/card":
      types["whatsapp/card"] = { body: getVal("cfgBody"), footer: getVal("cfgFooter") || null, header_text: getVal("cfgHeader") || null, media: getVal("cfgMedia") ? [getVal("cfgMedia")] : null, actions: collectActions("WHATSAPP_CARD") };
      break;
    case "twilio/carousel":
      types["twilio/carousel"] = { body: getVal("cfgBody"), cards: collectCards() };
      break;
    case "twilio/location":
      types["twilio/location"] = { latitude: parseFloat(getVal("cfgLat")) || 0, longitude: parseFloat(getVal("cfgLng")) || 0, label: getVal("cfgLabel") || null };
      break;
    case "baileys/buttons":
      types["baileys/buttons"] = { body: getVal("cfgBody"), actions: collectActions("QUICK_REPLY") };
      break;
    case "baileys/list":
      types["baileys/list"] = { body: getVal("cfgBody"), button: getVal("cfgButton"), items: collectItems() };
      break;
    case "baileys/template":
      types["baileys/template"] = { body: getVal("cfgBody"), actions: collectActions("CTA") };
      break;
    case "baileys/sticker":
      types["baileys/sticker"] = { body: getVal("cfgBody") || null, media: [getVal("cfgMedia")].filter(Boolean) };
      break;
  }
  return JSON.stringify(types);
}

function collectActions(mode) {
  const rows = document.querySelectorAll("#tbActionList .action-row");
  return Array.from(rows).map(row => {
    const type = row.querySelector(".action-type")?.value || "QUICK_REPLY";
    const title = row.querySelector(".action-title")?.value || "";
    const extra = row.querySelector(".action-url")?.value || "";
    const action = { type, title };
    if (type === "URL") action.url = extra;
    else if (type === "PHONE_NUMBER") action.phone = extra;
    else if (type === "QUICK_REPLY") action.id = extra || title;
    else if (type === "COPY_CODE") action.code = extra;
    return action;
  }).filter(a => a.title);
}

function collectItems() {
  const rows = document.querySelectorAll("#tbItemList .item-row");
  return Array.from(rows).map(row => ({
    item: row.querySelector(".item-title")?.value || "",
    description: row.querySelector(".item-desc")?.value || "",
    id: row.querySelector(".item-id")?.value || ""
  })).filter(i => i.item);
}

function collectCards() {
  const rows = document.querySelectorAll("#tbCardList .card-row");
  return Array.from(rows).map(row => ({
    title: row.querySelector(".card-title")?.value || null,
    body: row.querySelector(".card-body")?.value || "",
    media: row.querySelector(".card-media")?.value || "",
    actions: Array.from(row.querySelectorAll(".card-action-row")).map(ar => ({
      type: ar.querySelector(".ca-type")?.value || "QUICK_REPLY",
      title: ar.querySelector(".ca-title")?.value || "",
      url: ar.querySelector(".ca-url")?.value || "",
      id: ar.querySelector(".ca-title")?.value || ""
    })).filter(a => a.title)
  }));
}

function updateBuilderPreview() {
  const preview = document.getElementById("builderPreview");
  if (!preview) return;
  const type = document.getElementById("tbContentType").dataset.selected || "twilio/text";
  const getVal = (id) => document.getElementById(id)?.value || "";
  const body = getVal("cfgBody") || getVal("cfgTitle") || "Preview";
  preview.innerHTML = renderTwilioPreview(type, buildTypesJson(), body);
}

function renderTwilioPreview(type, typesJson, fallbackBody) {
  let html = `<div class="twilio-preview-container">`;
  // Header
  html += `<div style="background:#075e54;color:#fff;padding:8px 12px;border-radius:8px 8px 0 0;font-size:13px;font-weight:600">Your App <span style="float:right;font-size:11px">${type}</span></div>`;
  html += `<div class="twilio-preview-body" style="padding:12px;background:#fff;border:1px solid #e9edef;border-top:0;border-radius:0 0 8px 8px;font-size:14px;color:#111">`;
  try {
    const types = JSON.parse(typesJson || "{}");
    const renderBody = (text) => text ? `<div class="mb-1" style="white-space:pre-wrap">${esc(text)}</div>` : "";
    const renderActions = (actions, cls) => (actions||[]).map(a => `<span class="twilio-preview-btn ${cls||''}">${esc(a.title||a.item||"")}</span>`).join(' ');
    const renderMedia = (url) => url ? `<div style="background:#e9edef;height:80px;border-radius:6px;display:flex;align-items:center;justify-content:center;margin-bottom:6px;color:#8696a0;font-size:12px"><i class="bi bi-image me-1"></i> Media</div>` : "";
    
    if (types["twilio/text"]) {
      html += renderBody(types["twilio/text"].body);
    } else if (types["twilio/media"]) {
      html += renderMedia(types["twilio/media"].media?.[0]);
      html += renderBody(types["twilio/media"].body);
    } else if (types["twilio/quick-reply"]) {
      html += renderBody(types["twilio/quick-reply"].body);
      html += `<div class="mt-2 d-flex flex-wrap gap-1">${renderActions(types["twilio/quick-reply"].actions, "twilio-preview-qr")}</div>`;
    } else if (types["twilio/call-to-action"]) {
      html += renderBody(types["twilio/call-to-action"].body);
      html += `<div class="mt-2 d-flex flex-wrap gap-1">${renderActions(types["twilio/call-to-action"].actions, "twilio-preview-cta")}</div>`;
    } else if (types["twilio/list-picker"]) {
      html += renderBody(types["twilio/list-picker"].body);
      html += `<div class="mt-2"><span class="twilio-preview-btn twilio-preview-list">${esc(types["twilio/list-picker"].button||"View Options")} ▾</span></div>`;
      html += `<div class="mt-1 small text-muted">${(types["twilio/list-picker"].items||[]).length} items</div>`;
    } else if (types["twilio/card"]) {
      const c = types["twilio/card"];
      html += renderMedia(c.media?.[0]);
      if (c.title) html += `<div style="font-weight:600;font-size:15px">${esc(c.title)}</div>`;
      if (c.subtitle) html += `<div style="font-size:12px;color:#667781">${esc(c.subtitle)}</div>`;
      html += renderBody(c.body);
      html += `<div class="mt-2 d-flex flex-wrap gap-1">${renderActions(c.actions, "twilio-preview-card")}</div>`;
    } else if (types["whatsapp/card"]) {
      const c = types["whatsapp/card"];
      if (c.header_text) html += `<div style="font-weight:600;font-size:13px;color:#667781">${esc(c.header_text)}</div>`;
      html += renderMedia(c.media?.[0]);
      html += renderBody(c.body);
      if (c.footer) html += `<div style="font-size:11px;color:#8696a0;border-top:1px solid #e9edef;padding-top:4px;margin-top:4px">${esc(c.footer)}</div>`;
      html += `<div class="mt-2 d-flex flex-wrap gap-1">${renderActions(c.actions, "twilio-preview-card")}</div>`;
    } else if (types["twilio/carousel"]) {
      const car = types["twilio/carousel"];
      html += renderBody(car.body);
      html += `<div style="display:flex;gap:8px;overflow-x:auto;padding:4px 0">`;
      (car.cards||[]).slice(0,3).forEach(card => {
        html += `<div style="min-width:120px;border:1px solid #e9edef;border-radius:8px;padding:8px;font-size:12px">
          ${card.media ? `<div style="background:#e9edef;height:60px;border-radius:4px;display:flex;align-items:center;justify-content:center;margin-bottom:4px;font-size:10px;color:#8696a0"><i class="bi bi-image me-1"></i></div>` : ""}
          ${card.title ? `<div style="font-weight:600">${esc(card.title)}</div>` : ""}
          ${esc(card.body).substring(0,50)}
          <div class="mt-1">${renderActions(card.actions, "twilio-preview-card")}</div>
        </div>`;
      });
      html += `</div>`;
    } else if (types["twilio/location"]) {
      html += `<div style="background:#e9edef;height:80px;border-radius:6px;display:flex;align-items:center;justify-content:center;margin-bottom:6px;color:#8696a0;font-size:12px"><i class="bi bi-geo-alt-fill me-1" style="color:#ef5350"></i> Location</div>`;
      if (types["twilio/location"].label) html += `<div style="font-size:13px">${esc(types["twilio/location"].label)}</div>`;
      html += `<div style="font-size:11px;color:#8696a0">${types["twilio/location"].latitude}, ${types["twilio/location"].longitude}</div>`;
    } else if (types["baileys/buttons"]) {
      html += `<div style="font-size:11px;color:#8696a0;margin-bottom:4px">Baileys Buttons</div>`;
      html += renderBody(types["baileys/buttons"].body);
      html += `<div class="mt-2 d-flex flex-wrap gap-1">${renderActions(types["baileys/buttons"].actions, "twilio-preview-qr")}</div>`;
    } else if (types["baileys/list"]) {
      html += `<div style="font-size:11px;color:#8696a0;margin-bottom:4px">Baileys List</div>`;
      html += renderBody(types["baileys/list"].body);
      html += `<div class="mt-2"><span class="twilio-preview-btn twilio-preview-list">${esc(types["baileys/list"].button||"View Options")} ▾</span></div>`;
      html += `<div class="mt-1 small text-muted">${(types["baileys/list"].items||[]).length} items</div>`;
    } else if (types["baileys/template"]) {
      html += `<div style="font-size:11px;color:#8696a0;margin-bottom:4px">Baileys Template</div>`;
      html += renderBody(types["baileys/template"].body);
      html += `<div class="mt-2 d-flex flex-wrap gap-1">${renderActions(types["baileys/template"].actions, "twilio-preview-cta")}</div>`;
    } else if (types["baileys/sticker"]) {
      html += `<div style="font-size:11px;color:#8696a0;margin-bottom:4px">Baileys Sticker</div>`;
      html += `<div style="background:#e9edef;height:80px;width:80px;border-radius:6px;display:flex;align-items:center;justify-content:center;margin-bottom:6px;color:#8696a0;font-size:12px"><i class="bi bi-stickies-fill me-1"></i> Sticker</div>`;
      html += renderBody(types["baileys/sticker"].body);
    }
  } catch(e) {
    html += esc(fallbackBody || "Preview not available");
  }
  html += `<div class="text-muted" style="font-size:10px;margin-top:6px;text-align:right">${new Date().toLocaleTimeString("en-US", {hour:'2-digit',minute:'2-digit'})}</div>`;
  html += `</div></div>`;
  return html;
}

function showCreateTemplateModal() {
  document.getElementById("templateBuilderTitle").textContent = "New Template";
  document.getElementById("tbId").value = "";
  document.getElementById("tbName").value = "";
  document.getElementById("tbCategory").value = "UTILITY";
  document.getElementById("tbLanguage").value = "en";
  document.getElementById("tbContentType").dataset.selected = "twilio/text";
  document.querySelectorAll(".template-type-item").forEach(el => el.classList.toggle("active", el.dataset.type === "twilio/text"));
  renderConfigFields("twilio/text");
  updateBuilderPreview();
  new bootstrap.Modal(document.getElementById("templateModal")).show();
}

async function saveBuilderTemplate() {
  const type = document.getElementById("tbContentType").dataset.selected || "twilio/text";
  const name = document.getElementById("tbName").value.trim();
  if (!name) { showToast("Template name is required"); return; }
  const category = document.getElementById("tbCategory").value;
  const language = document.getElementById("tbLanguage").value;
  const typesJson = buildTypesJson();
  const body = JSON.parse(typesJson || "{}");
  const fallback = Object.values(body)[0]?.body || name;
  const payload = {
    whatsAppAccountId: "00000000-0000-0000-0000-000000000001",
    name, category, language,
    body: fallback,
    header: "", footer: "",
    contentType: type,
    typesJson
  };
  try {
    const id = document.getElementById("tbId")?.value;
    if (id) {
      payload.id = id;
      await fetch(`/api/templates/${id}`, { method:"PUT", headers:{"Content-Type":"application/json"}, body:JSON.stringify(payload) });
    } else {
      await fetch("/api/templates", { method:"POST", headers:{"Content-Type":"application/json"}, body:JSON.stringify(payload) });
    }
    bootstrap.Modal.getInstance(document.getElementById("templateModal")).hide();
    loadTemplates();
    showToast("Template saved!");
  } catch(e) { console.error(e); showToast("Failed to save template"); }
}

async function loadTemplates() {
  try {
    const r = await fetch("/api/templates");
    const list = await r.json();
    const typeLabels = { "twilio/text":"Text","twilio/media":"Media","twilio/quick-reply":"Quick Reply","twilio/call-to-action":"Call to Action","twilio/list-picker":"List Picker","twilio/card":"Card","whatsapp/card":"WhatsApp Card","twilio/carousel":"Carousel","twilio/location":"Location","baileys/buttons":"Baileys Buttons","baileys/list":"Baileys List","baileys/template":"Baileys Template","baileys/sticker":"Baileys Sticker" };
    document.getElementById("templatesGrid").innerHTML = list.map(t => `
      <div class="col-md-6 col-lg-4 mb-3">
        <div class="card template-card" onclick="previewTemplate('${t.id}')">
          <div class="card-body">
            <div class="d-flex justify-content-between align-items-start mb-2">
              <h6 class="mb-0">${esc(t.name)}</h6>
              <div>
                <span class="badge bg-${t.status==='APPROVED'?'success':'warning'} me-1">${t.status}</span>
                <span class="badge bg-secondary">${t.category}</span>
              </div>
            </div>
            <div class="d-flex gap-1 mb-1">
              <span class="badge bg-info text-dark">${typeLabels[t.contentType]||t.contentType}</span>
            </div>
            <div class="small text-secondary mb-2" style="display:-webkit-box;-webkit-line-clamp:3;-webkit-box-orient:vertical;overflow:hidden">${esc((t.body||"").substring(0,120))}${t.body&&t.body.length>120?'...':''}</div>
            ${t.templateId ? `<div class="small text-muted"><i class="bi bi-hash"></i> ${esc(t.templateId)}</div>` : ''}
            ${t.rejectionReason ? `<div class="small text-danger mt-1">Rejected: ${esc(t.rejectionReason)}</div>` : ''}
            <div class="mt-2 d-flex gap-1">
              <button class="btn btn-sm btn-outline-primary" onclick="event.stopPropagation();previewTemplate('${t.id}')"><i class="bi bi-eye"></i></button>
              <button class="btn btn-sm btn-outline-success" onclick="event.stopPropagation();sendTemplateToConv('${t.id}','${esc(t.name)}')"><i class="bi bi-send"></i></button>
              <button class="btn btn-sm btn-outline-warning" onclick="event.stopPropagation();editTemplate('${t.id}')"><i class="bi bi-pencil"></i></button>
              <button class="btn btn-sm btn-outline-danger" onclick="event.stopPropagation();deleteTemplate('${t.id}')"><i class="bi bi-trash"></i></button>
            </div>
            <small class="text-secondary d-block mt-1">${new Date(t.createdAt).toLocaleDateString()}</small>
          </div>
        </div>
      </div>`).join("");
  } catch(e) { console.error(e); }
}

// Template preview in Twilio style
function previewTemplate(id) {
  fetch("/api/templates").then(r=>r.json()).then(list => {
    const t = list.find(x => x.id === id);
    if (!t) return;
    const modal = document.getElementById("templatePreviewModal");
    document.getElementById("previewName").textContent = t.name;
    document.getElementById("previewBadge").innerHTML = `<span class="badge bg-${t.status==='APPROVED'?'success':'warning'}">${t.status}</span> <span class="badge bg-secondary">${t.category}</span> <span class="badge bg-info text-dark">${t.contentType}</span>`;
    document.getElementById("previewType").textContent = `Type: ${t.contentType}`;
    if (t.typesJson) {
      document.getElementById("previewBubble").innerHTML = renderTwilioPreview(t.contentType, t.typesJson, t.body);
    } else {
      // Legacy fallback
      document.getElementById("previewBubble").innerHTML = `
        <div class="twilio-preview-container">
          <div style="background:#075e54;color:#fff;padding:8px 12px;border-radius:8px 8px 0 0;font-size:13px;font-weight:600">${esc(t.name)}</div>
          <div style="padding:12px;background:#fff;border:1px solid #e9edef;border-top:0;border-radius:0 0 8px 8px;font-size:14px;color:#111">
            ${t.header ? `<div style="font-weight:600">${esc(t.header)}</div>` : ''}
            <div style="white-space:pre-wrap">${esc(t.body)}</div>
            ${t.footer ? `<div style="font-size:11px;color:#8696a0;border-top:1px solid #e9edef;padding-top:4px;margin-top:4px">${esc(t.footer)}</div>` : ''}
            <div style="font-size:10px;color:#8696a0;margin-top:6px;text-align:right">${new Date().toLocaleTimeString("en-US", {hour:'2-digit',minute:'2-digit'})}</div>
          </div>
        </div>`;
    }
    currentEditingTemplateId = t.id;
    new bootstrap.Modal(modal).show();
  }).catch(console.error);
}

function openSendFromPreview() {
  bootstrap.Modal.getInstance(document.getElementById("templatePreviewModal"))?.hide();
  sendTemplateToConv(currentEditingTemplateId, document.getElementById("previewName")?.textContent || "");
}

// Send template to conversation
function sendTemplateToConv(id, name) {
  fetch("/api/templates").then(r=>r.json()).then(list => {
    const t = list.find(x => x.id === id);
    if (!t) return;
    const vars = new Set();
    const allText = [t.body, t.header||"", t.footer||"", t.typesJson||""].join(" ");
    allText.replace(/\{\{(\d+)\}\}/g, (_, n) => { vars.add(n); return ""; });
    if (vars.size > 0) {
      showTemplateVariableModal(id, name, t, [...vars].sort());
    } else {
      sendTemplateDirectly(id, t, {});
    }
  }).catch(console.error);
}

function showTemplateVariableModal(id, name, t, varNames) {
  const modal = document.getElementById("templateVarModal");
  const body = document.getElementById("templateVarBody");
  document.getElementById("templateVarTitle").textContent = `Fill Variables & Send: ${name}`;
  body.innerHTML = `
    <div class="row">
      <div class="col-md-7">
        ${varNames.map(v => `
          <div class="mb-2">
            <label class="form-label small">${esc("{{" + v + "}}")}</label>
            <input class="form-control form-control-sm template-var-input" data-var="${v}" placeholder="Enter value for {{${v}}}" />
          </div>
        `).join('')}
        <div class="mb-2">
          <label class="form-label small">Send to conversation</label>
          <select class="form-select form-select-sm" id="templateVarConv">
            ${convs.map(c => `<option value="${c.remoteJid}" data-name="${esc(c.contactName)}" data-phone="${esc(c.contactPhone||'')}">${esc(c.contactName)}</option>`).join('')}
          </select>
        </div>
      </div>
      <div class="col-md-5">
        <label class="form-label small">Preview</label>
        <div id="varPreviewBubble" class="twilio-preview-container" style="transform:scale(0.85);transform-origin:top left">
          ${t.typesJson ? renderTwilioPreview(t.contentType, t.typesJson, t.body) : `<div style="padding:12px;background:#fff;border:1px solid #e9edef;font-size:14px;color:#111">${esc(t.body)}</div>`}
        </div>
      </div>
    </div>`;
  document.getElementById("templateVarSendBtn").onclick = () => {
    const vars = {};
    body.querySelectorAll(".template-var-input").forEach(inp => {
      vars[inp.dataset.var] = inp.value || `{{${inp.dataset.var}}}`;
    });
    const sel = document.getElementById("templateVarConv");
    sendTemplateDirectly(id, t, vars, sel.value, sel.options[sel.selectedIndex]?.dataset?.name, sel.options[sel.selectedIndex]?.dataset?.phone);
    bootstrap.Modal.getInstance(modal).hide();
  };
  new bootstrap.Modal(modal).show();
}

async function sendTemplateDirectly(id, t, vars, remoteJid, contactName, contactPhone) {
  if (!remoteJid) {
    remoteJid = document.getElementById("templateVarConv")?.value;
    const sel = document.getElementById("templateVarConv");
    if (sel) { contactName = sel.options[sel.selectedIndex]?.dataset?.name; contactPhone = sel.options[sel.selectedIndex]?.dataset?.phone; }
  }
  if (!remoteJid) { showToast("No conversation selected"); return; }
  try {
    await fetch(`/api/templates/${id}/send`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ remoteJid, contactName, contactPhone, variables: vars })
    });
    showToast("Template sent!");
  } catch(e) { console.error(e); showToast("Failed to send template"); }
}

function editTemplate(id) {
  fetch("/api/templates").then(r=>r.json()).then(list => {
    const t = list.find(x => x.id === id);
    if (!t) return;
    // Open the builder modal pre-filled
    const modal = document.getElementById("templateModal");
    document.getElementById("templateBuilderTitle").textContent = "Edit Template";
    document.getElementById("tbId").value = t.id;
    document.getElementById("tbName").value = t.name;
    document.getElementById("tbCategory").value = t.category;
    document.getElementById("tbLanguage").value = t.language;
    document.getElementById("tbContentType").dataset.selected = t.contentType || "twilio/text";
    document.querySelectorAll(".template-type-item").forEach(el => el.classList.toggle("active", el.dataset.type === (t.contentType || "twilio/text")));
    renderConfigFields(t.contentType || "twilio/text");
    // Populate from TypesJson
    if (t.typesJson) {
      try {
        const types = JSON.parse(t.typesJson);
        const populateIfExists = (id, val) => { const el = document.getElementById(id); if (el && val !== undefined && val !== null) el.value = val; };
        const content = types[t.contentType] || {};
        if (t.contentType === "twilio/text") { populateIfExists("cfgBody", content.body); }
        else if (t.contentType === "twilio/media") { populateIfExists("cfgMedia", content.media?.[0]); populateIfExists("cfgBody", content.body); }
        else if (t.contentType === "twilio/quick-reply") {
          populateIfExists("cfgBody", content.body);
          if (content.actions?.length) {
            document.getElementById("tbActionList").innerHTML = content.actions.map(a => `
              <div class="action-row mb-2 p-2 border rounded">
                <div class="row g-1">
                  <div class="col-3"><select class="form-select form-select-sm action-type"><option value="QUICK_REPLY" selected>Quick Reply</option></select></div>
                  <div class="col"><input class="form-control form-control-sm action-title" value="${esc(a.title)}" /></div>
                  <div class="col action-extra"><input class="form-control form-control-sm action-url" value="${esc(a.id||'')}" /></div>
                  <div class="col-auto"><button class="btn btn-outline-danger btn-sm" onclick="removeAction(this)"><i class="bi bi-x"></i></button></div>
                </div>
              </div>`).join('');
          }
        }
        else if (t.contentType === "twilio/call-to-action") {
          populateIfExists("cfgBody", content.body);
          if (content.actions?.length) {
            document.getElementById("tbActionList").innerHTML = content.actions.map(a => `
              <div class="action-row mb-2 p-2 border rounded">
                <div class="row g-1">
                  <div class="col-3"><select class="form-select form-select-sm action-type"><option value="${a.type}" selected>${a.type}</option></select></div>
                  <div class="col"><input class="form-control form-control-sm action-title" value="${esc(a.title)}" /></div>
                  <div class="col action-extra"><input class="form-control form-control-sm action-url" value="${esc(a.url||a.phone||'')}" /></div>
                  <div class="col-auto"><button class="btn btn-outline-danger btn-sm" onclick="removeAction(this)"><i class="bi bi-x"></i></button></div>
                </div>
              </div>`).join('');
          }
        }
        else if (t.contentType === "twilio/list-picker") {
          populateIfExists("cfgBody", content.body);
          populateIfExists("cfgButton", content.button);
          if (content.items?.length) {
            document.getElementById("tbItemList").innerHTML = content.items.map(i => `
              <div class="item-row mb-2 p-2 border rounded">
                <div class="row g-1">
                  <div class="col-4"><input class="form-control form-control-sm item-title" value="${esc(i.item)}" /></div>
                  <div class="col-4"><input class="form-control form-control-sm item-desc" value="${esc(i.description)}" /></div>
                  <div class="col"><input class="form-control form-control-sm item-id" value="${esc(i.id)}" /></div>
                  <div class="col-auto"><button class="btn btn-outline-danger btn-sm" onclick="removeListItem(this)"><i class="bi bi-x"></i></button></div>
                </div>
              </div>`).join('');
            document.getElementById("tbItemsArea").style.display = "block";
          }
        }
        else if (t.contentType === "twilio/card") {
          populateIfExists("cfgTitle", content.title);
          populateIfExists("cfgBody", content.body);
          populateIfExists("cfgSubtitle", content.subtitle);
          populateIfExists("cfgMedia", content.media?.[0]);
          if (content.actions?.length) {
            document.getElementById("tbActionList").innerHTML = content.actions.map(a => `
              <div class="action-row mb-2 p-2 border rounded">
                <div class="row g-1">
                  <div class="col-3"><select class="form-select form-select-sm action-type"><option value="${a.type}" selected>${a.type}</option></select></div>
                  <div class="col"><input class="form-control form-control-sm action-title" value="${esc(a.title)}" /></div>
                  <div class="col action-extra"><input class="form-control form-control-sm action-url" value="${esc(a.url||a.phone||'')}" /></div>
                  <div class="col-auto"><button class="btn btn-outline-danger btn-sm" onclick="removeAction(this)"><i class="bi bi-x"></i></button></div>
                </div>
              </div>`).join('');
          }
        }
        else if (t.contentType === "whatsapp/card") {
          populateIfExists("cfgBody", content.body);
          populateIfExists("cfgHeader", content.header_text);
          populateIfExists("cfgFooter", content.footer);
          populateIfExists("cfgMedia", content.media?.[0]);
          if (content.actions?.length) {
            document.getElementById("tbActionList").innerHTML = content.actions.map(a => `
              <div class="action-row mb-2 p-2 border rounded">
                <div class="row g-1">
                  <div class="col-3"><select class="form-select form-select-sm action-type"><option value="${a.type}" selected>${a.type}</option></select></div>
                  <div class="col"><input class="form-control form-control-sm action-title" value="${esc(a.title)}" /></div>
                  <div class="col action-extra"><input class="form-control form-control-sm action-url" value="${esc(a.url||a.phone||'')}" /></div>
                  <div class="col-auto"><button class="btn btn-outline-danger btn-sm" onclick="removeAction(this)"><i class="bi bi-x"></i></button></div>
                </div>
              </div>`).join('');
          }
        }
        else if (t.contentType === "twilio/carousel") {
          populateIfExists("cfgBody", content.body);
          if (content.cards?.length) {
            document.getElementById("tbCardsArea").style.display = "block";
            document.getElementById("tbCardList").innerHTML = content.cards.map((c, idx) => `
              <div class="card-row mb-2 p-2 border rounded" data-idx="${idx}">
                <div class="row g-1 mb-1">
                  <div class="col-6"><input class="form-control form-control-sm card-title" value="${esc(c.title||'')}" /></div>
                  <div class="col"><input class="form-control form-control-sm card-media" value="${esc(c.media||'')}" /></div>
                  <div class="col-auto"><button class="btn btn-outline-danger btn-sm" onclick="removeCard(this)"><i class="bi bi-x"></i></button></div>
                </div>
                <div class="mb-1"><textarea class="form-control form-control-sm card-body" rows="2">${esc(c.body||'')}</textarea></div>
                <div class="card-actions">
                  ${(c.actions||[]).map((a, ai) => `
                    <div class="row g-1 mb-1 card-action-row" data-idx="${ai}">
                      <div class="col-3"><select class="form-select form-select-sm ca-type"><option value="${a.type}" selected>${a.type}</option></select></div>
                      <div class="col"><input class="form-control form-control-sm ca-title" value="${esc(a.title)}" /></div>
                      <div class="col ca-extra"><input class="form-control form-control-sm ca-url" value="${esc(a.url||'')}" /></div>
                      <div class="col-auto"><button class="btn btn-outline-danger btn-sm" onclick="removeCardAction(this)"><i class="bi bi-x"></i></button></div>
                    </div>`).join('')}
                  <button class="btn btn-outline-secondary btn-sm" onclick="addCardAction(this)"><i class="bi bi-plus"></i> Button</button>
                </div>
              </div>`).join('');
          }
        }
        else if (t.contentType === "twilio/location") {
          populateIfExists("cfgLat", content.latitude);
          populateIfExists("cfgLng", content.longitude);
          populateIfExists("cfgLabel", content.label);
        }
        else if (t.contentType === "baileys/buttons") {
          populateIfExists("cfgBody", content.body);
          if (content.actions?.length) {
            document.getElementById("tbActionList").innerHTML = content.actions.map(a => `
              <div class="action-row mb-2 p-2 border rounded">
                <div class="row g-1">
                  <div class="col-3"><select class="form-select form-select-sm action-type"><option value="QUICK_REPLY" selected>Quick Reply</option></select></div>
                  <div class="col"><input class="form-control form-control-sm action-title" value="${esc(a.title)}" /></div>
                  <div class="col action-extra"><input class="form-control form-control-sm action-url" value="${esc(a.id||'')}" /></div>
                  <div class="col-auto"><button class="btn btn-outline-danger btn-sm" onclick="removeAction(this)"><i class="bi bi-x"></i></button></div>
                </div>
              </div>`).join('');
          }
        }
        else if (t.contentType === "baileys/list") {
          populateIfExists("cfgBody", content.body);
          populateIfExists("cfgButton", content.button);
          if (content.items?.length) {
            document.getElementById("tbItemList").innerHTML = content.items.map(i => `
              <div class="item-row mb-2 p-2 border rounded">
                <div class="row g-1">
                  <div class="col-4"><input class="form-control form-control-sm item-title" value="${esc(i.item)}" /></div>
                  <div class="col-4"><input class="form-control form-control-sm item-desc" value="${esc(i.description)}" /></div>
                  <div class="col"><input class="form-control form-control-sm item-id" value="${esc(i.id)}" /></div>
                  <div class="col-auto"><button class="btn btn-outline-danger btn-sm" onclick="removeListItem(this)"><i class="bi bi-x"></i></button></div>
                </div>
              </div>`).join('');
            document.getElementById("tbItemsArea").style.display = "block";
          }
        }
        else if (t.contentType === "baileys/template") {
          populateIfExists("cfgBody", content.body);
          if (content.actions?.length) {
            document.getElementById("tbActionList").innerHTML = content.actions.map(a => `
              <div class="action-row mb-2 p-2 border rounded">
                <div class="row g-1">
                  <div class="col-3"><select class="form-select form-select-sm action-type"><option value="${a.type}" selected>${a.type}</option></select></div>
                  <div class="col"><input class="form-control form-control-sm action-title" value="${esc(a.title)}" /></div>
                  <div class="col action-extra"><input class="form-control form-control-sm action-url" value="${esc(a.url||a.phone||'')}" /></div>
                  <div class="col-auto"><button class="btn btn-outline-danger btn-sm" onclick="removeAction(this)"><i class="bi bi-x"></i></button></div>
                </div>
              </div>`).join('');
          }
        }
        else if (t.contentType === "baileys/sticker") {
          populateIfExists("cfgMedia", content.media?.[0]);
          populateIfExists("cfgBody", content.body);
        }
      } catch(e) { console.error("Failed to parse typesJson", e); }
    } else {
      // Legacy: populate from flat fields
      const cfgBody = document.getElementById("cfgBody");
      if (cfgBody) cfgBody.value = t.body;
      const cfgHeader = document.getElementById("cfgHeader");
      if (cfgHeader) cfgHeader.value = t.header || "";
      const cfgFooter = document.getElementById("cfgFooter");
      if (cfgFooter) cfgFooter.value = t.footer || "";
    }
    updateBuilderPreview();
    new bootstrap.Modal(modal).show();
  }).catch(console.error);
}

async function saveEditTemplate() {
  const id = document.getElementById("teId")?.value;
  if (!id) return;
  await fetch(`/api/templates/${id}`, {
    method:"PUT",
    headers:{"Content-Type":"application/json"},
    body:JSON.stringify({
      id, name: document.getElementById("teName").value,
      category: document.getElementById("teCategory").value,
      language: document.getElementById("teLanguage").value,
      header: document.getElementById("teHeader")?.value || "",
      body: document.getElementById("teBody")?.value || "",
      footer: document.getElementById("teFooter")?.value || "",
      contentType: "twilio/text",
      typesJson: JSON.stringify({ "twilio/text": { body: document.getElementById("teBody")?.value || "" } })
    })
  });
  bootstrap.Modal.getInstance(document.getElementById("editTemplateModal")).hide();
  loadTemplates();
  showToast("Template updated");
}

async function deleteTemplate(id) {
  if (!confirm("Delete this template?")) return;
  await fetch(`/api/templates/${id}`, { method:"DELETE" });
  loadTemplates();
  showToast("Template deleted");
}

// ===== WORKFLOWS =====
async function loadWorkflows() {
  try {
    const r = await fetch("/api/workflows");
    const list = await r.json();
    document.getElementById("workflowsGrid").innerHTML = list.map(w => `<div class="col-md-4 mb-3"><div class="card">
      <div class="card-body"><div class="d-flex justify-content-between"><h6>${esc(w.name)}</h6>
        <div class="form-check form-switch"><input class="form-check-input" type="checkbox" ${w.isActive?'checked':''} onchange="toggleWorkflow('${w.id}', this.checked)" /></div></div>
      ${w.description ? `<div class="small text-secondary mb-2">${esc(w.description)}</div>` : ''}
      <div class="small">Triggers: ${(w.triggers||[]).map(t => `<span class="badge bg-info me-1">${t.eventType}</span>`).join('')}</div>
      <div class="small mt-1">Actions: ${(w.actions||[]).map(a => `<span class="badge bg-primary me-1">${a.actionType}</span>`).join('')}</div>
    </div></div></div>`).join("");
  } catch(e) { console.error(e); }
}

function showCreateWorkflowModal() { new bootstrap.Modal(document.getElementById("workflowModal")).show(); }
async function createWorkflow() {
  const triggers = [{ eventType:document.getElementById("wfEvent").value, order:0 }];
  const actions = [{ actionType:document.getElementById("wfAction").value, configuration:"{}", order:0 }];
  const body = { name:document.getElementById("wfName").value, description:document.getElementById("wfDesc").value, triggers, actions };
  await fetch("/api/workflows", { method:"POST", headers:{"Content-Type":"application/json"}, body:JSON.stringify(body) });
  bootstrap.Modal.getInstance(document.getElementById("workflowModal")).hide();
  document.getElementById("wfName").value = ""; document.getElementById("wfDesc").value = "";
  loadWorkflows();
}
async function toggleWorkflow(id, active) {
  await fetch(`/api/workflows/${id}/toggle`, { method:"PATCH", headers:{"Content-Type":"application/json"}, body:JSON.stringify({isActive:active}) });
}

// ===== TEAM =====
async function loadTeam() {
  try {
    const r = await fetch("/api/team/agents");
    const list = await r.json();
    document.getElementById("agentsTable").innerHTML = list.map(a => `<tr>
      <td>${avatar(a.name, a.avatarUrl, 32)} <strong>${esc(a.name)}</strong></td><td>${esc(a.email)}</td>
      <td><span class="badge bg-${a.role==='admin'?'danger':a.role==='manager'?'warning':'info'}">${a.role}</span></td>
      <td><span class="status-dot status-${a.status==='online'?'connected':'disconnected'}"></span>${a.status}</td>
      <td><small class="text-secondary">${new Date(a.createdAt).toLocaleDateString()}</small></td>
    </tr>`).join("");
  } catch(e) { console.error(e); }
}
function showAddAgentModal() { new bootstrap.Modal(document.getElementById("addAgentModal")).show(); }
async function createAgent() {
  const body = { name:document.getElementById("aName").value, email:document.getElementById("aEmail").value, role:document.getElementById("aRole").value };
  await fetch("/api/team/agents", { method:"POST", headers:{"Content-Type":"application/json"}, body:JSON.stringify(body) });
  bootstrap.Modal.getInstance(document.getElementById("addAgentModal")).hide();
  document.getElementById("aName").value = ""; document.getElementById("aEmail").value = "";
  loadTeam();
}

// ===== ANALYTICS =====
async function loadAnalytics() {
  try {
    const r = await fetch("/api/analytics/dashboard");
    const d = await r.json();
    document.getElementById("statsCards").innerHTML = `
      <div class="col-md-3"><div class="stat-card text-center"><h3>${d.totalConversations||0}</h3><small class="text-secondary">Total Conversations</small></div></div>
      <div class="col-md-3"><div class="stat-card text-center"><h3>${d.activeConversations||0}</h3><small class="text-secondary">Active</small></div></div>
      <div class="col-md-3"><div class="stat-card text-center"><h3>${d.messagesToday||0}</h3><small class="text-secondary">Messages Today</small></div></div>
      <div class="col-md-3"><div class="stat-card text-center"><h3>${d.totalContacts||0}</h3><small class="text-secondary">Contacts</small></div></div>`;
    const perfTbl = document.getElementById("agentPerfTable");
    perfTbl.innerHTML = (d.agentPerformance||[]).map(p => `<tr><td>Agent</td><td>${p.conversationsHandled||0}</td><td>${p.messagesSent||0}</td><td>${Math.round(p.avgResponseTimeSeconds||0)}s</td></tr>`).join("") || '<tr><td colspan="4" class="text-center text-secondary">No data</td></tr>';

    if (conversationChart) conversationChart.destroy();
    if (channelChart) channelChart.destroy();
    const ctx1 = document.getElementById("conversationChart").getContext("2d");
    conversationChart = new Chart(ctx1, { type:"line", data:{ labels:["Mon","Tue","Wed","Thu","Fri","Sat","Sun"], datasets:[{ label:"Conversations", data:[12,19,15,22,18,25,20], borderColor:"#0d6efd", tension:0.3 }]}, options:{ responsive:true, maintainAspectRatio:false, plugins:{legend:{display:false}} } });
    const ctx2 = document.getElementById("channelChart").getContext("2d");
    channelChart = new Chart(ctx2, { type:"doughnut", data:{ labels:["WhatsApp","Telegram","Email"], datasets:[{ data:[65,20,15], backgroundColor:["#25D366","#0088cc","#EA4335"] }]}, options:{ responsive:true, maintainAspectRatio:false, plugins:{legend:{position:"bottom"}} } });
  } catch(e) { console.error(e); }
}

// ===== CHANNELS =====
async function loadChannels() {
  try {
    const r = await fetch("/api/channels");
    const list = await r.json();
    document.getElementById("channelsGrid").innerHTML = list.map(c => `<div class="col-md-4 mb-3"><div class="card">
      <div class="card-body"><div class="d-flex align-items-center gap-2 mb-2">
        <i class="bi bi-${c.channelType==='telegram'?'telegram':c.channelType==='email'?'envelope':c.channelType==='facebook'?'facebook':'whatsapp'} fs-3"></i>
        <div><h6 class="mb-0">${esc(c.name)}</h6><small class="text-secondary">${c.channelType}</small></div>
        <span class="ms-auto badge bg-${c.isConnected?'success':'secondary'}">${c.isConnected?'Connected':'Offline'}</span>
      </div></div></div></div>`).join("");
  } catch(e) { console.error(e); }
}

function showConnectChannelModal() {
  onChannelTypeChange();
  new bootstrap.Modal(document.getElementById("channelModal")).show();
}
function onChannelTypeChange() {
  const type = document.getElementById("chType").value;
  const area = document.getElementById("chConfigArea");
  const help = document.getElementById("chHelpLink");
  if (type === "telegram") {
    area.innerHTML = `<input class="form-control" id="chToken" placeholder="Bot Token (from @BotFather)" />`;
    help.innerHTML = `<a href="https://t.me/botfather" target="_blank">Get bot token from @BotFather</a>`;
  } else if (type === "email") {
    area.innerHTML = `
      <div class="mb-1"><input class="form-control" id="chSmtpHost" placeholder="SMTP Host (e.g. smtp.gmail.com)" /></div>
      <div class="mb-1"><input class="form-control" id="chSmtpPort" type="number" placeholder="SMTP Port (e.g. 587)" /></div>
      <div class="mb-1"><input class="form-control" id="chEmailUser" placeholder="Email Username" /></div>
      <div class="mb-1"><input class="form-control" id="chEmailPass" type="password" placeholder="Email Password" /></div>`;
    help.innerHTML = ``;
  } else if (type === "facebook") {
    area.innerHTML = `
      <div class="mb-1"><input class="form-control" id="chFbPageId" placeholder="Facebook Page ID" /></div>
      <div class="mb-1"><input class="form-control" id="chFbToken" placeholder="Page Access Token" /></div>
      <div class="mb-1"><input class="form-control" id="chFbAppSecret" type="password" placeholder="App Secret (for webhook verification)" /></div>`;
    help.innerHTML = `<a href="https://developers.facebook.com/docs/messenger-platform" target="_blank">How to get Facebook Page Access Token</a>`;
  }
}
async function connectChannel() {
  const type = document.getElementById("chType").value;
  const name = document.getElementById("chName").value;
  let accessToken = "";
  let accountId = "";

  if (type === "telegram") {
    accessToken = document.getElementById("chToken").value;
  } else if (type === "email") {
    const host = document.getElementById("chSmtpHost").value;
    const port = document.getElementById("chSmtpPort").value;
    const user = document.getElementById("chEmailUser").value;
    const pass = document.getElementById("chEmailPass").value;
    accessToken = `${host}|${port}|${user}|${pass}`;
  } else if (type === "facebook") {
    const pageId = document.getElementById("chFbPageId").value;
    const token = document.getElementById("chFbToken").value;
    const appSecret = document.getElementById("chFbAppSecret").value;
    accountId = pageId;
    accessToken = JSON.stringify({ pageId, accessToken: token, appSecret });
  }

  const body = { channelType: type, name, accountId, accessToken };
  await fetch("/api/channels", { method:"POST", headers:{"Content-Type":"application/json"}, body:JSON.stringify(body) });
  bootstrap.Modal.getInstance(document.getElementById("channelModal")).hide();
  document.getElementById("chName").value = "";
  loadChannels();
}

// ===== SETTINGS =====
function toggleOllamaUrl() {
  const show = document.getElementById("sProvider").value === "ollama";
  document.getElementById("ollamaUrlGroup").style.display = show ? "" : "none";
}
async function loadAiSettings() {
  try {
    const r = await fetch("/api/config/ai");
    const list = await r.json();
    if (list.length) {
      const c = list[0]; aiConfigId = c.id;
      document.getElementById("sProvider").value = c.provider||"ollama";
      document.getElementById("sOllamaUrl").value = c.ollamaBaseUrl||"http://localhost:11434";
      document.getElementById("sModel").value = c.model||"";
      document.getElementById("sTemp").value = c.temperature;
      document.getElementById("sTemp").nextElementSibling.textContent = c.temperature;
      document.getElementById("sMaxTokens").value = c.maxTokens;
      document.getElementById("sPrompt").value = c.systemPrompt;
      toggleOllamaUrl();
    }
  } catch(e) { console.error(e); }
}
async function saveAiSettings() {
  const body = { name:"Default", provider:document.getElementById("sProvider").value, model:document.getElementById("sModel").value, temperature:parseFloat(document.getElementById("sTemp").value), maxTokens:parseInt(document.getElementById("sMaxTokens").value), systemPrompt:document.getElementById("sPrompt").value, isActive:true };
  if (body.provider==="ollama") body.ollamaBaseUrl = document.getElementById("sOllamaUrl").value || "http://localhost:11434";
  await fetch(`/api/config/ai/${aiConfigId}`, { method:"PUT", headers:{"Content-Type":"application/json"}, body:JSON.stringify(body) });
  showToast("AI config saved");
}

let qrPollInterval = null;

function setQrState(state) {
  const qrDiv = document.getElementById("qrDisplay");
  if (state === "connecting") qrDiv.innerHTML = '<div class="spinner-border text-primary" role="status"></div><p class="small text-secondary mt-2">Connecting to WhatsApp...</p>';
  else if (state === "scan") qrDiv.innerHTML = '<div class="spinner-border text-warning" role="status"></div><p class="small text-secondary mt-2">Scan QR code to connect</p>';
  else if (state === "connected") qrDiv.innerHTML = '<i class="bi bi-check-circle-fill text-success fs-1 d-block mb-2"></i><p class="small text-success mt-2">WhatsApp connected</p>';
  else qrDiv.innerHTML = '<div class="spinner-border text-primary" role="status"></div><p class="small text-secondary mt-2">Connecting to WhatsApp...</p>';
}

async function loadQr() {
  try {
    const r = await fetch("/api/config/qr");
    const d = await r.json();
    const qrDiv = document.getElementById("qrDisplay");
    if (d.qr && d.qr.includes("base64")) {
      qrDiv.innerHTML = `<div class="text-center"><img src="${d.qr}" class="img-fluid" style="max-width:200px" alt="QR" /><p class="small text-warning mt-2">Scan with WhatsApp</p></div>`;
      updateConnBadge("awaiting_scan");
    } else if (d.qr) {
      qrDiv.innerHTML = `<div class="text-center"><pre class="small" style="font-size:8px;line-height:1">${d.qr}</pre><p class="small text-warning mt-2">Scan with WhatsApp</p></div>`;
      updateConnBadge("awaiting_scan");
    } else {
      setQrState("connecting");
    }
  } catch(e) {
    console.error("QR load error:", e);
    setQrState("connecting");
  }
}

function refreshQr() {
  if (qrPollInterval) clearInterval(qrPollInterval);
  setQrState("connecting");
  loadQr();
  qrPollInterval = setInterval(loadQr, 5000);
}

function stopQrPolling() { if (qrPollInterval) { clearInterval(qrPollInterval); qrPollInterval = null; } }

async function loadWidget() {
  try {
    const r = await fetch("/api/widget/config");
    const w = await r.json();
    if (w) { document.getElementById("wGreeting").value = w.greetingText||""; document.getElementById("wColor").value = w.primaryColor||"#075e54"; }
  } catch(e) {}
}
async function saveWidget() {
  const body = { greetingText:document.getElementById("wGreeting").value, primaryColor:document.getElementById("wColor").value };
  await fetch("/api/widget/config", { method:"PUT", headers:{"Content-Type":"application/json"}, body:JSON.stringify(body) });
  showToast("Widget settings saved");
}

// ===== UTILITY =====
async function checkConn() {
  try { const r = await fetch("/api/config/status"); const d = await r.json(); updateConnBadge(d.connected?"connected":"disconnected"); } catch { updateConnBadge("disconnected"); }
}
function updateConnBadge(s) {
  const b = document.getElementById("connBadge");
  if (s==="connected") { b.className="badge bg-success"; b.innerHTML='<span class="status-dot status-connected"></span>Connected'; }
  else if (s==="awaiting_scan") { b.className="badge bg-warning"; b.innerHTML='<span class="status-dot status-awaiting"></span>Scan QR'; }
  else { b.className="badge bg-danger"; b.innerHTML='<span class="status-dot status-disconnected"></span>Disconnected'; }
}
let replyContext = null;

function showMsgContextMenu(e, msgId) {
  e.preventDefault(); e.stopPropagation();
  closeMsgContextMenu();
  const menu = document.getElementById("msgContextMenu");
  if (!menu) return;
  menu.style.left = e.clientX + "px";
  menu.style.top = e.clientY + "px";
  menu.classList.remove("d-none");
  menu.dataset.msgId = msgId;
}

function closeMsgContextMenu() {
  const menu = document.getElementById("msgContextMenu");
  if (menu) menu.classList.add("d-none");
}

// ===== REPLY =====
function startReply(msgId) {
  closeMsgContextMenu();
  const bar = document.getElementById("replyBar");
  if (!bar) return;
  bar.dataset.replyToId = msgId;
  bar.classList.remove("d-none");
  const msgEl = document.getElementById("msg-" + msgId);
  const txt = msgEl ? (msgEl.querySelector(".msg-text")?.textContent?.substring(0,50) || "Media") : "";
  document.getElementById("replyBarPreview").textContent = txt;
  document.getElementById("msgInput").focus();
}

function cancelReply() {
  const bar = document.getElementById("replyBar");
  if (bar) { bar.classList.add("d-none"); bar.dataset.replyToId = ""; }
  replyContext = null;
}

// ===== FORWARD =====
function showForwardDialog(msgId) {
  closeMsgContextMenu();
  const modal = document.getElementById("forwardModal");
  if (!modal) return;
  modal.dataset.msgId = msgId;
  document.getElementById("forwardConvList").innerHTML = '<div class="spinner-border spinner-border-sm"></div>';
  const fModal = new bootstrap.Modal(modal);
  fModal.show();
  // Load conversations
  fetch("/api/conversations").then(r=>r.json()).then(list => {
    const h = list.map(c => `<div class="forward-conv-item" onclick="selectForwardConv('${c.remoteJid}','${esc(c.contactName)}','${esc(c.contactPhone||'')}')">
      <span class="fw-bold">${esc(c.contactName)}</span><span class="text-secondary small ms-2">${esc(c.remoteJid)}</span>
    </div>`).join('');
    document.getElementById("forwardConvList").innerHTML = h||'<div class="text-muted">No conversations</div>';
  }).catch(()=>document.getElementById("forwardConvList").innerHTML='<div class="text-danger">Failed to load</div>');
}

let forwardTarget = null;
function selectForwardConv(jid, name, phone) {
  forwardTarget = { jid, name, phone };
  document.querySelectorAll(".forward-conv-item").forEach(el => el.classList.remove("selected"));
  event.currentTarget.classList.add("selected");
}

async function doForward() {
  const modal = document.getElementById("forwardModal");
  if (!forwardTarget) { showToast("Select a conversation"); return; }
  const msgId = modal.dataset.msgId;
  try {
    await fetch(`/api/messages/${msgId}/forward`, { method:"POST", headers:{"Content-Type":"application/json"}, body:JSON.stringify({targetRemoteJid:forwardTarget.jid,targetContactName:forwardTarget.name,targetContactPhone:forwardTarget.phone}) });
    showToast("Message forwarded");
    bootstrap.Modal.getInstance(modal).hide();
    forwardTarget = null;
  } catch(e) { showToast("Forward failed"); }
}

// ===== EDIT =====
function editMessage(msgId) {
  closeMsgContextMenu();
  const msgEl = document.getElementById("msg-" + msgId);
  if (!msgEl) return;
  const textDiv = msgEl.querySelector(".msg-text");
  if (!textDiv) { showToast("Only text messages can be edited"); return; }
  const orig = textDiv.textContent;
  textDiv.innerHTML = `<textarea class="form-control form-control-sm edit-textarea" rows="2">${esc(orig)}</textarea>
    <div class="mt-1"><button class="btn btn-sm btn-success me-1" onclick="saveEdit('${msgId}')">Save</button>
    <button class="btn btn-sm btn-secondary" onclick="cancelEdit('${msgId}','${esc(orig)}')">Cancel</button></div>`;
}

async function saveEdit(msgId) {
  const msgEl = document.getElementById("msg-" + msgId);
  const ta = msgEl?.querySelector(".edit-textarea");
  if (!ta || !ta.value.trim()) return;
  const remoteJid = msgEl?.dataset.remoteJid || "";
  const messageKeyId = msgEl?.dataset.providerMessageId || "";
  try {
    await fetch(`/api/messages/${msgId}`, { method:"PUT", headers:{"Content-Type":"application/json"}, body:JSON.stringify({content:ta.value.trim(), remoteJid, messageKeyId}) });
    showToast("Message edited");
  } catch(e) { showToast("Edit failed"); }
}

function cancelEdit(msgId, orig) {
  const msgEl = document.getElementById("msg-" + msgId);
  const textDiv = msgEl?.querySelector(".msg-text");
  if (textDiv) textDiv.textContent = orig;
}

// ===== DELETE =====
function deleteMessage(msgId, forEveryone) {
  closeMsgContextMenu();
  if (!confirm(forEveryone ? "Delete for everyone?" : "Delete for me?")) return;
  const msgEl = document.getElementById("msg-"+msgId);
  const remoteJid = msgEl?.dataset.remoteJid || "";
  const messageKeyId = msgEl?.dataset.providerMessageId || "";
  const qs = `?forEveryone=${forEveryone}&remoteJid=${encodeURIComponent(remoteJid)}&messageKeyId=${encodeURIComponent(messageKeyId)}`;
  fetch(`/api/messages/${msgId}${qs}`, { method:"DELETE" }).then(() => {
    showToast("Message deleted");
    if (forEveryone) {
      const el = document.getElementById("msg-"+msgId);
      if (el) el.closest(".msg-wrapper")?.remove();
    }
  }).catch(() => showToast("Delete failed"));
}

// ===== SEND CONTACT =====
function showContactModal() {
  document.getElementById("attachMenu").classList.remove("show");
  document.getElementById("contactName").value = "";
  document.getElementById("contactPhone").value = "";
  new bootstrap.Modal(document.getElementById("contactModal")).show();
}
async function sendContact() {
  const name = document.getElementById("contactName").value.trim();
  const phone = document.getElementById("contactPhone").value.trim();
  if (!name || !phone) { showToast("Name and phone required"); return; }
  const c = convs.find(x => x.id === currentConvId);
  if (!c) { showToast("No conversation selected"); return; }
  try {
    await fetch("/api/conversations/send-contact", {
      method:"POST", headers:{"Content-Type":"application/json"},
      body:JSON.stringify({ remoteJid: c.remoteJid, contactName: name, contactPhone: phone })
    });
    bootstrap.Modal.getInstance(document.getElementById("contactModal")).hide();
    showToast("Contact sent!");
  } catch(e) { showToast("Failed to send contact"); }
}

// ===== CREATE POLL =====
let pollOptionCount = 2;
function showPollModal() {
  document.getElementById("attachMenu").classList.remove("show");
  document.getElementById("pollQuestion").value = "";
  document.getElementById("pollOptions").innerHTML =
    '<div class="mb-1"><input class="form-control form-control-sm" placeholder="Option 1" /></div>' +
    '<div class="mb-1"><input class="form-control form-control-sm" placeholder="Option 2" /></div>';
  pollOptionCount = 2;
  new bootstrap.Modal(document.getElementById("pollModal")).show();
}
function addPollOption() {
  pollOptionCount++;
  const div = document.createElement("div");
  div.className = "mb-1";
  div.innerHTML = `<input class="form-control form-control-sm" placeholder="Option ${pollOptionCount}" />`;
  document.getElementById("pollOptions").appendChild(div);
}
async function sendPoll() {
  const question = document.getElementById("pollQuestion").value.trim();
  const inputs = document.querySelectorAll("#pollOptions input");
  const options = Array.from(inputs).map(i => i.value.trim()).filter(Boolean);
  if (!question || options.length < 2) { showToast("Question and at least 2 options required"); return; }
  const c = convs.find(x => x.id === currentConvId);
  if (!c) { showToast("No conversation selected"); return; }
  try {
    await fetch("/api/conversations/send-poll", {
      method:"POST", headers:{"Content-Type":"application/json"},
      body:JSON.stringify({ remoteJid: c.remoteJid, pollName: question, options })
    });
    bootstrap.Modal.getInstance(document.getElementById("pollModal")).hide();
    showToast("Poll sent!");
  } catch(e) { showToast("Failed to send poll"); }
}

// ===== POST STATUS =====
function showStatusModal() {
  document.getElementById("attachMenu").classList.remove("show");
  document.getElementById("statusText").value = "";
  document.getElementById("statusMediaUrl").value = "";
  new bootstrap.Modal(document.getElementById("statusModal")).show();
}
async function sendStatus() {
  const text = document.getElementById("statusText").value.trim();
  const mediaUrl = document.getElementById("statusMediaUrl").value.trim();
  if (!text && !mediaUrl) { showToast("Enter text or media URL"); return; }
  try {
    await fetch("/api/status", {
      method:"POST", headers:{"Content-Type":"application/json"},
      body:JSON.stringify({ text: text || null, mediaUrl: mediaUrl || null, mediaType: mediaUrl ? "image" : null })
    });
    bootstrap.Modal.getInstance(document.getElementById("statusModal")).hide();
    showToast("Status posted!");
  } catch(e) { showToast("Failed to post status"); }
}

// ===== GROUP INFO =====
async function showGroupInfo() {
  const c = convs.find(x => x.id === currentConvId);
  if (!c || !c.remoteJid?.includes("@g.us")) {
    showToast("Not a group conversation");
    return;
  }
  try {
    const res = await fetch(`/api/groups/${encodeURIComponent(c.remoteJid)}`);
    if (!res.ok) { showToast("Failed to load group info"); return; }
    const g = await res.json();
    const body = document.getElementById("groupModalBody");
    body.innerHTML = `
      <div class="mb-3">
        <h5>${esc(g.subject||"Unnamed Group")}</h5>
        <small class="text-secondary">${g.id}</small>
        <p class="mt-2">${esc(g.desc||"")}</p>
        <span class="badge bg-info">${g.size||0} participants</span>
        ${g.ephemeralDuration ? `<span class="badge bg-secondary ms-1">Disappearing: ${g.ephemeralDuration}s</span>` : ""}
      </div>
      <h6>Participants</h6>
      <div class="list-group">${(g.participants||[]).map(p => `
        <div class="list-group-item d-flex justify-content-between align-items-center">
          <span>${esc(p.jid)} ${p.admin ? '<span class="badge bg-warning">Admin</span>' : ''}</span>
        </div>`).join("")}
      </div>`;
    new bootstrap.Modal(document.getElementById("groupModal")).show();
  } catch(e) { showToast("Failed to load group info"); }
}

// ===== BLOCK CONTACT =====
async function blockCurrentContact() {
  const c = convs.find(x => x.id === currentConvId);
  if (!c || !c.remoteJid) return;
  if (!confirm("Block this contact?")) return;
  try {
    await fetch(`/api/contacts/${encodeURIComponent(c.remoteJid)}/block`, {
      method:"POST", headers:{"Content-Type":"application/json"},
      body:JSON.stringify({ block: true })
    });
    showToast("Contact blocked");
  } catch(e) { showToast("Failed to block contact"); }
}

// ===== UPDATE PROFILE (settings) =====
async function updateWhatsAppProfile() {
  const name = document.getElementById("sProfileName")?.value.trim();
  const status = document.getElementById("sProfileStatus")?.value.trim();
  const picUrl = document.getElementById("sProfilePicUrl")?.value.trim();
  if (!name && !status && !picUrl) { showToast("Nothing to update"); return; }
  try {
    await fetch("/api/profile", {
      method:"PUT", headers:{"Content-Type":"application/json"},
      body:JSON.stringify({ name: name||null, status: status||null, profilePictureUrl: picUrl||null })
    });
    showToast("Profile updated");
  } catch(e) { showToast("Failed to update profile"); }
}

// ===== COMPOSE HELPERS =====
async function sendContactDirect(remoteJid, contactName, contactPhone) {
  await fetch("/api/conversations/send-contact", {
    method:"POST", headers:{"Content-Type":"application/json"},
    body:JSON.stringify({ remoteJid, contactName, contactPhone })
  });
}
async function sendPollDirect(remoteJid, pollName, options) {
  await fetch("/api/conversations/send-poll", {
    method:"POST", headers:{"Content-Type":"application/json"},
    body:JSON.stringify({ remoteJid, pollName, options })
  });
}

function esc(t) { const d = document.createElement("div"); d.textContent = t||""; return d.innerHTML; }
function timeAgo(d) { if (!d) return ""; const s = Math.floor((new Date()-new Date(d))/1000); if (s<60) return "now"; if (s<3600) return Math.floor(s/60)+"m"; if (s<86400) return Math.floor(s/3600)+"h"; return Math.floor(s/86400)+"d"; }
function showToast(m) {
  const t = document.createElement("div"); t.className="toast-container position-fixed bottom-0 end-0 p-3";
  t.innerHTML = `<div class="toast show align-items-center text-bg-success border-0"><div class="d-flex"><div class="toast-body">${m}</div><button class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button></div></div>`;
  document.body.appendChild(t); setTimeout(() => t.remove(), 3000);
}
