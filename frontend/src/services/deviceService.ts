import { apiClient } from './api';
import { Device } from '../types/Device';
import { ScanNetworkResponse } from '../types/ApiResponse';

export const deviceService = {
  /**
   * Scan the network for devices
   * @param cidr Optional CIDR notation (e.g., "192.168.1.0/24")
   */
  scanNetwork: async (cidr?: string): Promise<ScanNetworkResponse> => {
    const params = cidr ? { cidr } : {};
    const response = await apiClient.post<ScanNetworkResponse>('/api/scan', null, { params });
    return response.data;
  },

  /**
   * Get device details by IP address
   * @param ipAddress IPv4 or IPv6 address
   */
  getDeviceDetails: async (ipAddress: string): Promise<Device> => {
    const response = await apiClient.get<Device>(`/api/devices/${encodeURIComponent(ipAddress)}`);
    return response.data;
  },
};
