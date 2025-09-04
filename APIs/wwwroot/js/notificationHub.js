/**
 * SignalR Client for Real-time Notifications
 * Usage: Include this script in your frontend application
 */

class NotificationHubClient {
  constructor() {
    this.connection = null;
    this.isConnected = false;
    this.reconnectAttempts = 0;
    this.maxReconnectAttempts = 5;
    this.reconnectDelay = 5000; // 5 seconds
    this.eventHandlers = {
      onNotificationReceived: null,
      onNotificationCountUpdated: null,
      onConnected: null,
      onDisconnected: null,
      onError: null,
    };
  }

  /**
   * Initialize the SignalR connection
   * @param {string} baseUrl - Base URL of the API (e.g., 'https://localhost:7000')
   * @param {string} accessToken - JWT access token for authentication
   */
  async initialize(baseUrl, accessToken) {
    try {
      // Import SignalR library (make sure to include @microsoft/signalr in your project)
      const signalR = await import("@microsoft/signalr");

      this.connection = new signalR.HubConnectionBuilder()
        .withUrl(`${baseUrl}/notificationHub`, {
          accessTokenFactory: () => accessToken,
          transport: signalR.HttpTransportType.WebSockets,
        })
        .withAutomaticReconnect([0, 2000, 10000, 30000]) // Reconnect attempts
        .configureLogging(signalR.LogLevel.Information)
        .build();

      this.setupEventHandlers();
      await this.start();

      console.log("NotificationHub connected successfully");
    } catch (error) {
      console.error("Error initializing NotificationHub:", error);
      this.handleError(error);
    }
  }

  /**
   * Setup event handlers for the connection
   */
  setupEventHandlers() {
    // Connection events
    this.connection.onclose((error) => {
      this.isConnected = false;
      console.log("NotificationHub connection closed:", error);
      if (this.eventHandlers.onDisconnected) {
        this.eventHandlers.onDisconnected(error);
      }
    });

    this.connection.onreconnecting((error) => {
      console.log("NotificationHub reconnecting:", error);
    });

    this.connection.onreconnected((connectionId) => {
      this.isConnected = true;
      this.reconnectAttempts = 0;
      console.log("NotificationHub reconnected:", connectionId);
      if (this.eventHandlers.onConnected) {
        this.eventHandlers.onConnected(connectionId);
      }
    });

    // Notification events
    this.connection.on("ReceiveNotification", (notification) => {
      console.log("Received notification:", notification);
      if (this.eventHandlers.onNotificationReceived) {
        this.eventHandlers.onNotificationReceived(notification);
      }
    });

    this.connection.on("UpdateNotificationCount", (count) => {
      console.log("Notification count updated:", count);
      if (this.eventHandlers.onNotificationCountUpdated) {
        this.eventHandlers.onNotificationCountUpdated(count);
      }
    });
  }

  /**
   * Start the connection
   */
  async start() {
    try {
      await this.connection.start();
      this.isConnected = true;
      this.reconnectAttempts = 0;

      if (this.eventHandlers.onConnected) {
        this.eventHandlers.onConnected();
      }
    } catch (error) {
      console.error("Error starting NotificationHub:", error);
      this.handleError(error);
    }
  }

  /**
   * Stop the connection
   */
  async stop() {
    if (this.connection) {
      await this.connection.stop();
      this.isConnected = false;
    }
  }

  /**
   * Join a specific group (e.g., for specific leave requests)
   * @param {string} groupName - Name of the group to join
   */
  async joinGroup(groupName) {
    if (this.isConnected) {
      try {
        await this.connection.invoke("JoinGroup", groupName);
        console.log(`Joined group: ${groupName}`);
      } catch (error) {
        console.error(`Error joining group ${groupName}:`, error);
      }
    }
  }

  /**
   * Leave a specific group
   * @param {string} groupName - Name of the group to leave
   */
  async leaveGroup(groupName) {
    if (this.isConnected) {
      try {
        await this.connection.invoke("LeaveGroup", groupName);
        console.log(`Left group: ${groupName}`);
      } catch (error) {
        console.error(`Error leaving group ${groupName}:`, error);
      }
    }
  }

  /**
   * Set event handlers
   * @param {Object} handlers - Object containing event handler functions
   */
  setEventHandlers(handlers) {
    this.eventHandlers = { ...this.eventHandlers, ...handlers };
  }

  /**
   * Handle errors
   * @param {Error} error - Error object
   */
  handleError(error) {
    console.error("NotificationHub error:", error);
    if (this.eventHandlers.onError) {
      this.eventHandlers.onError(error);
    }
  }

  /**
   * Get connection state
   */
  getConnectionState() {
    return this.connection ? this.connection.state : "Disconnected";
  }

  /**
   * Check if connected
   */
  isHubConnected() {
    return (
      this.isConnected &&
      this.connection &&
      this.connection.state === signalR.HubConnectionState.Connected
    );
  }
}

// Example usage:
/*
// Initialize the notification hub
const notificationHub = new NotificationHubClient();

// Set up event handlers
notificationHub.setEventHandlers({
    onNotificationReceived: (notification) => {
        // Handle new notification
        console.log('New notification:', notification);
        // Update UI, show toast, etc.
        showNotificationToast(notification);
        updateNotificationList(notification);
    },
    
    onNotificationCountUpdated: (count) => {
        // Update notification badge/counter
        console.log('Notification count:', count);
        updateNotificationBadge(count);
    },
    
    onConnected: () => {
        console.log('Connected to notification hub');
        // Update connection status in UI
    },
    
    onDisconnected: (error) => {
        console.log('Disconnected from notification hub:', error);
        // Update connection status in UI
    },
    
    onError: (error) => {
        console.error('Notification hub error:', error);
        // Show error message to user
    }
});

// Initialize with your API base URL and JWT token
const apiBaseUrl = 'https://localhost:7000'; // Replace with your API URL
const accessToken = localStorage.getItem('accessToken'); // Get from your auth system

notificationHub.initialize(apiBaseUrl, accessToken);

// Join specific groups if needed
// notificationHub.joinGroup('LeaveRequest_123');
*/

// Export for use in modules
if (typeof module !== "undefined" && module.exports) {
  module.exports = NotificationHubClient;
}

// Make available globally
if (typeof window !== "undefined") {
  window.NotificationHubClient = NotificationHubClient;
}
