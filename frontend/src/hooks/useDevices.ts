import { useState, useCallback } from 'react';
import { Device } from '../types/Device';

export const useDevices = () => {
  const [devices, setDevices] = useState<Device[]>([]);
  const [selectedDevice, setSelectedDevice] = useState<Device | null>(null);

  const addDevices = useCallback((newDevices: Device[]) => {
    setDevices(newDevices);
  }, []);

  const updateDevice = useCallback((updatedDevice: Device) => {
    setDevices(prev =>
      prev.map(d => d.id === updatedDevice.id ? updatedDevice : d)
    );
  }, []);

  return {
    devices,
    selectedDevice,
    setSelectedDevice,
    addDevices,
    updateDevice,
  };
};
