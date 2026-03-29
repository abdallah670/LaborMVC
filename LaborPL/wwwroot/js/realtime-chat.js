// Global Real-time connection script
(function () {
    // Prevent duplicate connections
    if (window.globalConnection) return;

    // Create a single global connection
    window.globalConnection = new signalR.HubConnectionBuilder()
        .withUrl("/DirectChatHub")
        .withAutomaticReconnect()
        .build();

    // Start connection
    window.globalConnection.start()
        .then(() => {})
        .catch(err => console.error('❌ Connection failed:', err));

    // Handle UserOnline event
    window.globalConnection.on("UserOnline", function (userId) {
        document.querySelectorAll(`[data-user-id="${userId}"] .online-indicator`).forEach(el => {
            el.style.display = 'inline-block';
        });
    });

    // Handle UserOffline event
    window.globalConnection.on("UserOffline", function (userId) {
        document.querySelectorAll(`[data-user-id="${userId}"] .online-indicator`).forEach(el => {
            el.style.display = 'none';
        });
    });
})();
