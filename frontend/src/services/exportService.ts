import * as XLSX from 'xlsx';
import { Device } from '../types/Device';
import { format } from 'date-fns';

export const exportToExcel = (devices: Device[], filename: string = 'network-devices.xlsx') => {
  // Transform devices to flat structure for Excel
  const data = devices.map(device => ({
    'IP Address': device.ipv4Addresses[0] || device.ipv6Addresses[0] || 'N/A',
    'Hostname': device.hostname || 'Unknown',
    'MAC Address': device.macAddress || 'N/A',
    'Device Type': device.deviceType,
    'Operating System': device.operatingSystem || 'Unknown',
    'Open Ports': device.openPorts.map(p => p.portNumber).join(', '),
    'Online': device.isOnline ? 'Yes' : 'No',
    'Last Seen': format(new Date(device.lastSeen), 'yyyy-MM-dd HH:mm:ss'),
  }));

  const worksheet = XLSX.utils.json_to_sheet(data);
  const workbook = XLSX.utils.book_new();
  XLSX.utils.book_append_sheet(workbook, worksheet, 'Devices');

  // Auto-size columns
  const maxWidth = 50;
  const columnWidths = Object.keys(data[0] || {}).map(key => ({
    wch: Math.min(maxWidth, Math.max(key.length, 15))
  }));
  worksheet['!cols'] = columnWidths;

  XLSX.writeFile(workbook, filename);
};
