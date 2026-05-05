const connection = new signalR.HubConnectionBuilder()
    .withUrl("/chatHub")
    .withAutomaticReconnect()
    .build();

let editingMessageId = null;
let typingTimer = null;
let isTyping = false;

// ── Connect ──────────────────────────────────────────────────

connection.start().then(() => {
    scrollToBottom();
}).catch(err => console.error("SignalR error:", err));

// ── Receive Events ───────────────────────────────────────────

connection.on("ReceiveMessage", function (msg) {
    appendMessage(msg);
    scrollToBottom();
});

connection.on("MessageEdited", function (msg) {
    const textEl = document.getElementById(`msgText_${msg.id}`);
    if (textEl) {
        textEl.textContent = msg.content;
        const metaEl = textEl.nextElementSibling;
        if (metaEl && !metaEl.querySelector('.message-edited')) {
            const editedSpan = document.createElement('span');
            editedSpan.className = 'message-edited';
            editedSpan.textContent = 'edited';
            metaEl.appendChild(editedSpan);
        }
    }
    cancelEdit();
});

connection.on("MessageDeleted", function (messageId) {
    const textEl = document.getElementById(`msgText_${messageId}`);
    if (textEl) {
        textEl.textContent = "This message was deleted";
        const bubble = textEl.closest('.message-bubble');
        if (bubble) {
            bubble.classList.add('deleted');
            const actions = bubble.querySelector('.message-actions');
            if (actions) actions.remove();
        }
    }
});

connection.on("UserTyping", function (userId) {
    if (userId === friendId) {
        document.getElementById('typingIndicator').style.display = 'flex';
    }
});

connection.on("UserStoppedTyping", function (userId) {
    if (userId === friendId) {
        document.getElementById('typingIndicator').style.display = 'none';
    }
});

connection.on("UserOnline", function (userId) {
    if (userId === friendId) setOnlineStatus(true);
});

connection.on("UserOffline", function (userId) {
    if (userId === friendId) setOnlineStatus(false);
});

// ── Send Message ─────────────────────────────────────────────

document.getElementById('sendBtn').addEventListener('click', sendMessage);

document.getElementById('messageInput').addEventListener('keydown', function (e) {
    if (e.key === 'Enter' && !e.shiftKey) {
        e.preventDefault();
        sendMessage();
    }
});

document.getElementById('messageInput').addEventListener('input', function () {
    autoResize(this);
    handleTyping();
});

function sendMessage() {
    const input = document.getElementById('messageInput');
    const text = input.value.trim();
    if (!text) return;

    if (editingMessageId) {
        connection.invoke("EditMessage", editingMessageId, text)
            .catch(err => console.error(err));
    } else {
        connection.invoke("SendMessage", friendId, text)
            .catch(err => console.error(err));
    }

    input.value = '';
    autoResize(input);
    stopTyping();
}

// ── Edit / Delete ─────────────────────────────────────────────

function startEdit(messageId, content) {
    editingMessageId = messageId;
    const input = document.getElementById('messageInput');
    input.value = content;
    input.focus();
    autoResize(input);
    document.getElementById('editBar').style.display = 'flex';
}

function cancelEdit() {
    editingMessageId = null;
    document.getElementById('messageInput').value = '';
    document.getElementById('editBar').style.display = 'none';
}

function deleteMessage(messageId, receiverId) {
    if (!confirm('Delete this message?')) return;
    connection.invoke("DeleteMessage", messageId, receiverId)
        .catch(err => console.error(err));
}

// ── Typing ────────────────────────────────────────────────────

function handleTyping() {
    if (!isTyping) {
        isTyping = true;
        connection.invoke("SendTyping", friendId).catch(() => { });
    }
    clearTimeout(typingTimer);
    typingTimer = setTimeout(stopTyping, 2000);
}

function stopTyping() {
    if (isTyping) {
        isTyping = false;
        connection.invoke("StopTyping", friendId).catch(() => { });
    }
    clearTimeout(typingTimer);
}

// ── Helpers ───────────────────────────────────────────────────

function appendMessage(msg) {
    const area = document.getElementById('messagesArea');
    const isMine = msg.senderId === currentUserId;

    const wrap = document.createElement('div');
    wrap.className = `message-wrap ${isMine ? 'mine' : 'theirs'}`;
    wrap.id = `msg_${msg.id}`;

    const time = new Date(msg.sentAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });

    wrap.innerHTML = `
        <div class="message-bubble">
            <div class="message-text" id="msgText_${msg.id}">${escapeHtml(msg.content)}</div>
            <div class="message-meta">
                <span class="message-time">${time}</span>
            </div>
            ${isMine ? `
            <div class="message-actions">
                <button class="msg-action-btn" onclick="startEdit(${msg.id}, '${escapeHtml(msg.content).replace(/'/g, "\\'")}')">✏️</button>
                <button class="msg-action-btn delete-btn" onclick="deleteMessage(${msg.id}, '${friendId}')">🗑️</button>
            </div>` : ''}
        </div>`;

    area.appendChild(wrap);
}

function setOnlineStatus(online) {
    const dot = document.getElementById('headerDot');
    const status = document.getElementById('headerStatus');
    if (dot) dot.style.display = online ? 'block' : 'none';
    if (status) status.textContent = online ? 'Online' : 'Offline';
}

function scrollToBottom() {
    const area = document.getElementById('messagesArea');
    if (area) area.scrollTop = area.scrollHeight;
}

function autoResize(el) {
    el.style.height = 'auto';
    el.style.height = Math.min(el.scrollHeight, 120) + 'px';
}

function escapeHtml(text) {
    const d = document.createElement('div');
    d.appendChild(document.createTextNode(text));
    return d.innerHTML;
}