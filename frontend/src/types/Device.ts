import { DeviceType } from './DeviceType';
import { NetworkPort } from './NetworkPort';

export interface Device {
  id: string;
  ipv4Addresses: string[];
  ipv6Addresses: string[];
  hostname: string | null;
  macAddress: string | null;
  openPorts: NetworkPort[];
  deviceType: DeviceType;
  operatingSystem: string | null;
  lastSeen: string; // ISO date string
  firstDiscovered: string; // ISO date string
  isOnline: boolean;
}
