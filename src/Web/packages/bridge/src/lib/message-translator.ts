import logger from './logger.js';
import SocketIOServer from './socketio-server.js';

interface DataPoint {
  _id?: string;
  id?: string;
  sgv?: number;
  value?: number;
  date?: number;
  timestamp?: number;
  dateString?: string;
  trend?: number;
  direction?: string;
  filtered?: number;
  unfiltered?: number;
  rssi?: number;
  noise?: number;
  type?: string;
  [key: string]: any;
}

interface AnnouncementMessage {
  message?: string;
  text?: string;
  title?: string;
  level?: string;
  timestamp?: string;
  [key: string]: any;
}

interface AlarmData {
  level?: string;
  title?: string;
  message?: string;
  plugin?: string;
  source?: string;
  timestamp?: string;
  key?: string;
  id?: string;
  [key: string]: any;
}

interface NotificationData {
  title?: string;
  message?: string;
  level?: string;
  plugin?: string;
  source?: string;
  timestamp?: string;
  [key: string]: any;
}

interface StatusData {
  status?: string;
  state?: string;
  message?: string;
  timestamp?: string;
  [key: string]: any;
}

class MessageTranslator {
  private socketIOServer: SocketIOServer;
  private tenantSlug?: string;

  constructor(socketIOServer: SocketIOServer, tenantSlug?: string) {
    this.socketIOServer = socketIOServer;
    this.tenantSlug = tenantSlug;
  }

  handleDataUpdate(data: any): void {
    try {
      const translatedData = this.translateDataUpdate(data);
      this.socketIOServer.broadcastDataUpdate(translatedData, this.tenantSlug);
    } catch (error) {
      logger.error('Error translating data update:', error);
    }
  }

  handleAnnouncement(message: AnnouncementMessage): void {
    try {
      const translatedMessage = this.translateAnnouncement(message);
      this.socketIOServer.broadcastAnnouncement(translatedMessage, this.tenantSlug);
    } catch (error) {
      logger.error('Error translating announcement:', error);
    }
  }

  handleAlarm(alarm: AlarmData): void {
    try {
      const translatedAlarm = this.translateAlarm(alarm);
      this.socketIOServer.broadcastAlarm(translatedAlarm as any, this.tenantSlug);
    } catch (error) {
      logger.error('Error translating alarm:', error);
    }
  }

  handleClearAlarm(): void {
    try {
      this.socketIOServer.broadcastClearAlarm(this.tenantSlug);
    } catch (error) {
      logger.error('Error handling clear alarm:', error);
    }
  }

  handleNotification(notification: NotificationData): void {
    try {
      const translatedNotification = this.translateNotification(notification);
      this.socketIOServer.broadcastNotification(translatedNotification, this.tenantSlug);
    } catch (error) {
      logger.error('Error translating notification:', error);
    }
  }

  handleStatusUpdate(status: StatusData): void {
    try {
      const translatedStatus = this.translateStatusUpdate(status);
      this.socketIOServer.broadcastStatusUpdate(translatedStatus, this.tenantSlug);
    } catch (error) {
      logger.error('Error translating status update:', error);
    }
  }

  handleStorageCreate(data: any): void {
    try {
      const translatedData = this.translateStorageEvent(data);
      this.socketIOServer.broadcastStorageEvent('create', translatedData, this.tenantSlug);
    } catch (error) {
      logger.error('Error translating storage create:', error);
    }
  }

  handleStorageUpdate(data: any): void {
    try {
      const translatedData = this.translateStorageEvent(data);
      this.socketIOServer.broadcastStorageEvent('update', translatedData, this.tenantSlug);
    } catch (error) {
      logger.error('Error translating storage update:', error);
    }
  }

  handleStorageDelete(data: any): void {
    try {
      const translatedData = this.translateStorageEvent(data);
      this.socketIOServer.broadcastStorageEvent('delete', translatedData, this.tenantSlug);
    } catch (error) {
      logger.error('Error translating storage delete:', error);
    }
  }

  handleNotificationCreated(data: any): void {
    try {
      this.socketIOServer.broadcastInAppNotification('notificationCreated', data, this.tenantSlug);
    } catch (error) {
      logger.error('Error handling notification created:', error);
    }
  }

  handleNotificationArchived(data: any): void {
    try {
      this.socketIOServer.broadcastInAppNotification('notificationArchived', data, this.tenantSlug);
    } catch (error) {
      logger.error('Error handling notification archived:', error);
    }
  }

  handleNotificationUpdated(data: any): void {
    try {
      this.socketIOServer.broadcastInAppNotification('notificationUpdated', data, this.tenantSlug);
    } catch (error) {
      logger.error('Error handling notification updated:', error);
    }
  }

  handleSyncProgress(data: any): void {
    try {
      this.socketIOServer.broadcastSyncProgress(data, this.tenantSlug);
    } catch (error) {
      logger.error('Error handling sync progress:', error);
    }
  }

  handleConfigChanged(data: any): void {
    try {
      this.socketIOServer.broadcastConfigChanged(data, this.tenantSlug);
    } catch (error) {
      logger.error('Error handling config changed:', error);
    }
  }

  // Translation methods - these ensure compatibility with legacy Nightscout client expectations

  private translateDataUpdate(data: DataPoint[]): any {
    // Ensure the data structure matches what legacy clients expect
    if (Array.isArray(data)) {
      return data.map(item => this.translateSingleDataPoint(item));
    } else if (data && typeof data === 'object') {
      return this.translateSingleDataPoint(data);
    }
    return data;
  }

  private translateSingleDataPoint(item: DataPoint): DataPoint {
    // Ensure required fields are present for legacy compatibility
    return {
      _id: item._id || item.id,
      sgv: item.sgv || item.value,
      date: item.date || item.timestamp,
      dateString: item.dateString || new Date(item.date || item.timestamp || Date.now()).toISOString(),
      trend: item.trend,
      direction: item.direction,
      filtered: item.filtered,
      unfiltered: item.unfiltered,
      rssi: item.rssi,
      noise: item.noise,
      type: item.type || 'sgv',
      ...item // Include any additional fields
    };
  }

  private translateAnnouncement(message: AnnouncementMessage): AnnouncementMessage {
    // Ensure announcement format matches legacy expectations
    return {
      message: message.message || message.text || String(message),
      title: message.title || 'Announcement',
      level: message.level || 'info',
      timestamp: message.timestamp || new Date().toISOString(),
      ...message
    };
  }

  private translateAlarm(alarm: AlarmData): AlarmData {
    // Ensure alarm format matches legacy expectations
    return {
      level: alarm.level || 'warn', // 'urgent', 'warn', 'info'
      title: alarm.title || 'Alarm',
      message: alarm.message,
      plugin: alarm.plugin || alarm.source,
      timestamp: alarm.timestamp || new Date().toISOString(),
      key: alarm.key || alarm.id,
      ...alarm
    };
  }

  private translateNotification(notification: NotificationData): NotificationData {
    // Ensure notification format matches legacy expectations
    return {
      title: notification.title,
      message: notification.message,
      level: notification.level || 'info',
      plugin: notification.plugin || notification.source,
      timestamp: notification.timestamp || new Date().toISOString(),
      ...notification
    };
  }

  private translateStatusUpdate(status: StatusData): StatusData {
    // Ensure status format matches legacy expectations
    return {
      status: status.status || status.state,
      message: status.message,
      timestamp: status.timestamp || new Date().toISOString(),
      ...status
    };
  }

  private translateStorageEvent(data: any): any {
    // Ensure storage event format matches legacy expectations
    // Legacy Nightscout expects { colName: 'entries', doc: {...} } format
    return {
      colName: data.colName || data.collection,
      doc: data.doc || data.document || data,
      ...data
    };
  }

}

export default MessageTranslator;
