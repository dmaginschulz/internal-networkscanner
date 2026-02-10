import { useState } from 'react';
import { deviceService } from '../services/deviceService';
import { ScanNetworkResponse } from '../types/ApiResponse';

export const useNetworkScan = () => {
  const [scanning, setScanning] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [scanResult, setScanResult] = useState<ScanNetworkResponse | null>(null);

  const startScan = async (cidr?: string) => {
    setScanning(true);
    setError(null);
    try {
      const result = await deviceService.scanNetwork(cidr);
      setScanResult(result);
      return result;
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Scan failed';
      setError(errorMessage);
      throw err;
    } finally {
      setScanning(false);
    }
  };

  return { scanning, error, scanResult, startScan };
};
