import { Device } from './Device';

export interface ScanNetworkResponse {
  devices: Device[];
  totalDevicesFound: number;
  scanStartTime: string;
  scanEndTime: string;
  networkScanned: string;
}
