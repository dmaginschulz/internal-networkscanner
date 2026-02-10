import { useState, useMemo } from 'react';
import { Device } from '../../types/Device';
import './DeviceTable.css';

interface DeviceTableProps {
  devices: Device[];
  onSelectDevice: (device: Device) => void;
  selectedDeviceId?: string;
}

type SortField = 'ip' | 'hostname' | 'deviceType' | 'os' | 'lastSeen';
type SortDirection = 'asc' | 'desc';

const DeviceTable = ({ devices, onSelectDevice, selectedDeviceId }: DeviceTableProps) => {
  const [sortField, setSortField] = useState<SortField>('ip');
  const [sortDirection, setSortDirection] = useState<SortDirection>('asc');
  const [searchTerm, setSearchTerm] = useState('');

  const handleSort = (field: SortField) => {
    if (sortField === field) {
      setSortDirection(sortDirection === 'asc' ? 'desc' : 'asc');
    } else {
      setSortField(field);
      setSortDirection('asc');
    }
  };

  const filteredAndSortedDevices = useMemo(() => {
    let result = [...devices];

    // Filter
    if (searchTerm) {
      const search = searchTerm.toLowerCase();
      result = result.filter(device =>
        device.ipv4Addresses.some(ip => ip.toLowerCase().includes(search)) ||
        device.ipv6Addresses.some(ip => ip.toLowerCase().includes(search)) ||
        device.hostname?.toLowerCase().includes(search) ||
        device.macAddress?.toLowerCase().includes(search) ||
        device.deviceType.toLowerCase().includes(search)
      );
    }

    // Sort
    result.sort((a, b) => {
      let aValue: string | number;
      let bValue: string | number;

      switch (sortField) {
        case 'ip':
          aValue = a.ipv4Addresses[0] || a.ipv6Addresses[0] || '';
          bValue = b.ipv4Addresses[0] || b.ipv6Addresses[0] || '';
          break;
        case 'hostname':
          aValue = a.hostname || '';
          bValue = b.hostname || '';
          break;
        case 'deviceType':
          aValue = a.deviceType;
          bValue = b.deviceType;
          break;
        case 'os':
          aValue = a.operatingSystem || '';
          bValue = b.operatingSystem || '';
          break;
        case 'lastSeen':
          aValue = new Date(a.lastSeen).getTime();
          bValue = new Date(b.lastSeen).getTime();
          break;
        default:
          aValue = '';
          bValue = '';
      }

      if (aValue < bValue) return sortDirection === 'asc' ? -1 : 1;
      if (aValue > bValue) return sortDirection === 'asc' ? 1 : -1;
      return 0;
    });

    return result;
  }, [devices, searchTerm, sortField, sortDirection]);

  const getSortIndicator = (field: SortField) => {
    if (sortField !== field) return ' ↕️';
    return sortDirection === 'asc' ? ' ↑' : ' ↓';
  };

  return (
    <div className="device-table-container">
      <div className="table-controls">
        <input
          type="text"
          placeholder="🔍 Search by IP, hostname, MAC, or device type..."
          value={searchTerm}
          onChange={(e) => setSearchTerm(e.target.value)}
          className="search-input"
        />
        <span className="result-count">
          Showing {filteredAndSortedDevices.length} of {devices.length} devices
        </span>
      </div>

      <div className="table-wrapper">
        <table className="device-table">
          <thead>
            <tr>
              <th onClick={() => handleSort('ip')}>
                IP Address{getSortIndicator('ip')}
              </th>
              <th onClick={() => handleSort('hostname')}>
                Hostname{getSortIndicator('hostname')}
              </th>
              <th>MAC Address</th>
              <th onClick={() => handleSort('deviceType')}>
                Device Type{getSortIndicator('deviceType')}
              </th>
              <th onClick={() => handleSort('os')}>
                OS{getSortIndicator('os')}
              </th>
              <th>Open Ports</th>
              <th onClick={() => handleSort('lastSeen')}>
                Last Seen{getSortIndicator('lastSeen')}
              </th>
            </tr>
          </thead>
          <tbody>
            {filteredAndSortedDevices.map((device) => (
              <tr
                key={device.id}
                onClick={() => onSelectDevice(device)}
                className={selectedDeviceId === device.id ? 'selected' : ''}
              >
                <td>
                  <span className="online-indicator">
                    {device.isOnline ? '🟢' : '🔴'}
                  </span>
                  {device.ipv4Addresses[0] || device.ipv6Addresses[0]}
                </td>
                <td>{device.hostname || <em>Unknown</em>}</td>
                <td>{device.macAddress || <em>N/A</em>}</td>
                <td>
                  <span className={`device-type ${device.deviceType.toLowerCase()}`}>
                    {device.deviceType}
                  </span>
                </td>
                <td>{device.operatingSystem || <em>Unknown</em>}</td>
                <td>
                  <span className="port-count">
                    {device.openPorts.length > 0
                      ? `${device.openPorts.length} ports`
                      : 'None'}
                  </span>
                </td>
                <td>
                  {new Date(device.lastSeen).toLocaleString()}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {filteredAndSortedDevices.length === 0 && (
        <div className="empty-table">
          {searchTerm ? 'No devices match your search.' : 'No devices found.'}
        </div>
      )}
    </div>
  );
};

export default DeviceTable;
