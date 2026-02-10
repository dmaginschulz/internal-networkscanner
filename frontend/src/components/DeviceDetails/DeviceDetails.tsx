import { Device } from '../../types/Device';
import './DeviceDetails.css';

interface DeviceDetailsProps {
  device: Device;
  onClose: () => void;
}

const DeviceDetails = ({ device, onClose }: DeviceDetailsProps) => {
  return (
    <div className="device-details">
      <div className="details-header">
        <h3>Device Details</h3>
        <button onClick={onClose} className="close-button">✕</button>
      </div>

      <div className="details-content">
        <div className="detail-section">
          <h4>General Information</h4>
          <div className="detail-item">
            <span className="label">Status:</span>
            <span className="value">
              {device.isOnline ? '🟢 Online' : '🔴 Offline'}
            </span>
          </div>
          <div className="detail-item">
            <span className="label">Device Type:</span>
            <span className="value">{device.deviceType}</span>
          </div>
          <div className="detail-item">
            <span className="label">Operating System:</span>
            <span className="value">{device.operatingSystem || 'Unknown'}</span>
          </div>
        </div>

        <div className="detail-section">
          <h4>Network Information</h4>
          {device.ipv4Addresses.length > 0 && (
            <div className="detail-item">
              <span className="label">IPv4 Addresses:</span>
              <span className="value">
                {device.ipv4Addresses.map((ip, i) => (
                  <div key={i}>{ip}</div>
                ))}
              </span>
            </div>
          )}
          {device.ipv6Addresses.length > 0 && (
            <div className="detail-item">
              <span className="label">IPv6 Addresses:</span>
              <span className="value">
                {device.ipv6Addresses.map((ip, i) => (
                  <div key={i}>{ip}</div>
                ))}
              </span>
            </div>
          )}
          <div className="detail-item">
            <span className="label">Hostname:</span>
            <span className="value">{device.hostname || 'Unknown'}</span>
          </div>
          <div className="detail-item">
            <span className="label">MAC Address:</span>
            <span className="value">{device.macAddress || 'N/A'}</span>
          </div>
        </div>

        <div className="detail-section">
          <h4>Open Ports ({device.openPorts.length})</h4>
          {device.openPorts.length > 0 ? (
            <div className="ports-list">
              {device.openPorts.map((port, i) => (
                <div key={i} className="port-item">
                  <span className="port-number">{port.portNumber}</span>
                  <span className="port-protocol">{port.protocol}</span>
                  {port.serviceName && (
                    <span className="port-service">{port.serviceName}</span>
                  )}
                </div>
              ))}
            </div>
          ) : (
            <p className="no-data">No open ports detected</p>
          )}
        </div>

        <div className="detail-section">
          <h4>Timestamps</h4>
          <div className="detail-item">
            <span className="label">First Discovered:</span>
            <span className="value">
              {new Date(device.firstDiscovered).toLocaleString()}
            </span>
          </div>
          <div className="detail-item">
            <span className="label">Last Seen:</span>
            <span className="value">
              {new Date(device.lastSeen).toLocaleString()}
            </span>
          </div>
        </div>
      </div>
    </div>
  );
};

export default DeviceDetails;
