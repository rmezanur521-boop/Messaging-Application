const connection = new signalR.HubConnectionBuilder()
    .withUrl("/chatHub")
    .withAutomaticReconnect()
    .build();

let editingMessageId = null;
let typingTimer = null;
let isTyping = false;
let typingUsers = {};

connection.start().then(() => {
    connection.invoke("JoinGroup", groupId)
        .catch(err => console.error("JoinGroup error:", err));
    scrollToBottom();
}).catch(err => console.error("SignalR error:", err));

connection.on("ReceiveGroupMessage", function (msg) {
    appendGroupMessage(msg);
    scrollToBottom();
});

connection.on("GroupMessageEdited", function (msg) {
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

connection.on("GroupMessageDeleted", function (messageId) {
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

connection.on("GroupUserTyping", function (userId) {
    typingUsers[userId] = true;
    showTypingIndicator();
});

connection.on("GroupUserStoppedTyping", function (userId) {
    delete typingUsers[userId];
    if (Object.keys(typingUsers).length === 0) {
        document.getElementById('typingIndicator').style.display = 'none';
    }
});
connection.on("GroupDeleted", function (deletedGroupId) {
    if (deletedGroupId === groupId) {
        alert("This group has been deleted.");
        window.location.href = "/Chat";
    }
});

connection.on("MemberLeft", function (data) {
    if (data.groupId !== groupId) return;
    const memberItems = document.querySelectorAll('.member-item');
    memberItems.forEach(item => {
        if (item.dataset.userId === data.userId) {
            item.remove();
        }
    });
});

connection.on("AdminChanged", function (data) {
    if (data.groupId !== groupId) return;
    const memberItems = document.querySelectorAll('.member-item');
    memberItems.forEach(item => {
        if (item.dataset.userId === data.newAdminId) {
            const infoDiv = item.querySelector('.member-info');
            if (infoDiv && !infoDiv.querySelector('.member-role-badge')) {
                const badge = document.createElement('span');
                badge.className = 'member-role-badge';
                badge.textContent = 'Admin';
                infoDiv.appendChild(badge);
            }
        }
    });
});

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

document.getElementById('messageInput').addEventListener('focus', function () {
    setTimeout(scrollToBottom, 300);
});

if (window.visualViewport) {
    window.visualViewport.addEventListener('resize', function () {
        scrollToBottom();
    });
}

function sendMessage() {
    const input = document.getElementById('messageInput');
    const text = input.value.trim();
    if (!text) return;

    if (editingMessageId) {
        connection.invoke("EditGroupMessage", editingMessageId, groupId, text)
            .catch(err => console.error(err));
    } else {
        connection.invoke("SendGroupMessage", groupId, text)
            .catch(err => console.error(err));
    }

    input.value = '';
    autoResize(input);
    stopTyping();
    input.focus();
    setTimeout(scrollToBottom, 50);
}

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

function deleteGroupMessage(messageId) {
    if (!confirm('Delete this message?')) return;
    connection.invoke("DeleteGroupMessage", messageId, groupId)
        .catch(err => console.error(err));
}

function handleTyping() {
    if (!isTyping) {
        isTyping = true;
        connection.invoke("SendGroupTyping", groupId).catch(() => { });
    }
    clearTimeout(typingTimer);
    typingTimer = setTimeout(stopTyping, 2000);
}

function stopTyping() {
    if (isTyping) {
        isTyping = false;
        connection.invoke("StopGroupTyping", groupId).catch(() => { });
    }
    clearTimeout(typingTimer);
}

function showTypingIndicator() {
    document.getElementById('typingIndicator').style.display = 'flex';
}

document.getElementById('membersToggle').addEventListener('click', function () {
    document.getElementById('membersPanel').classList.toggle('open');
});

document.getElementById('closeMembersPanel').addEventListener('click', function () {
    document.getElementById('membersPanel').classList.remove('open');
});

function appendGroupMessage(msg) {
    const area = document.getElementById('messagesArea');
    const isMine = msg.senderId === currentUserId;

    const wrap = document.createElement('div');
    wrap.className = `message-wrap ${isMine ? 'mine' : 'theirs'}`;
    wrap.id = `msg_${msg.id}`;

    const time = new Date(msg.sentAt)
        .toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });

    wrap.innerHTML = `
        ${!isMine ? `<div class="message-sender-name">${escapeHtml(msg.senderName)}</div>` : ''}
        <div class="message-bubble">
            <div class="message-text" id="msgText_${msg.id}">
                ${escapeHtml(msg.content)}
            </div>
            <div class="message-meta">
                <span class="message-time">${time}</span>
            </div>
            ${isMine ? `
            <div class="message-actions">
                <button class="msg-action-btn"
                    onclick="startEdit(${msg.id}, '${escapeHtml(msg.content).replace(/'/g, "\\'")}')">
                    ✏️
                </button>
                <button class="msg-action-btn delete-btn"
                    onclick="deleteGroupMessage(${msg.id})">
                    🗑️
                </button>
            </div>` : ''}
        </div>`;

    area.appendChild(wrap);
}

function scrollToBottom() {
    const area = document.getElementById('messagesArea');
    if (area) area.scrollTop = area.scrollHeight;
}

function autoResize(el) {
    el.style.height = 'auto';
    el.style.height = Math.min(el.scrollHeight, 120) + 'px';
    scrollToBottom();
}

function escapeHtml(text) {
    const d = document.createElement('div');
    d.appendChild(document.createTextNode(text));
    return d.innerHTML;
}