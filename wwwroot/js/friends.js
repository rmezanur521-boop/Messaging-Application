// Online status dots in sidebar
if (typeof connection === 'undefined') {
    var sidebarConnection = new signalR.HubConnectionBuilder()
        .withUrl("/chatHub")
        .withAutomaticReconnect()
        .build();

    sidebarConnection.on("UserOnline", function (userId) {
        const dot = document.getElementById(`dot_${userId}`);
        if (dot) dot.style.display = 'block';
    });

    sidebarConnection.on("UserOffline", function (userId) {
        const dot = document.getElementById(`dot_${userId}`);
        if (dot) dot.style.display = 'none';
    });

    sidebarConnection.start().catch(err => console.error("Sidebar SignalR:", err));
}