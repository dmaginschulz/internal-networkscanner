import { useState } from 'react';
import { useNetworkScan } from './hooks/useNetworkScan';
import { useDevices } from './hooks/useDevices';
import DeviceTable from './components/DeviceTable/DeviceTable';
import DeviceDetails from './components/DeviceDetails/DeviceDetails';
import { exportToExcel } from './services/exportService';
import './App.css';

function App() {
  const [cidr, setCidr] = useState('');
  const { scanning, error, startScan } = useNetworkScan();
  const { devices, selectedDevice, setSelectedDevice, addDevices } = useDevices();

  const handleScan = async () => {
    try {
      const result = await startScan(cidr || undefined);
      addDevices(result.devices);
    } catch (err) {
      console.error('Scan error:', err);
    }
  };

  const handleExport = () => {
    if (devices.length > 0) {
      exportToExcel(devices);
    }
  };

  return (
    <div className="app">
      <header className="app-header">
        <h1>🌐 Network Scanner</h1>
        <p>Scan and discover all devices in your network</p>
      </header>

      <div className="controls">
        <div className="scan-controls">
          <input
            type="text"
            placeholder="CIDR (e.g., 192.168.1.0/24)"
            value={cidr}
            onChange={(e) => setCidr(e.target.value)}
            disabled={scanning}
          />
          <button onClick={handleScan} disabled={scanning}>
            {scanning ? '🔄 Scanning...' : '🔍 Scan Network'}
          </button>
          {devices.length > 0 && (
            <button onClick={handleExport}>
              📊 Export to Excel
            </button>
          )}
        </div>
        {error && <div className="error">{error}</div>}
      </div>

      {scanning && (
        <div className="scanning-indicator">
          <div className="spinner"></div>
          <p>Scanning network... This may take a few minutes.</p>
        </div>
      )}

      {devices.length > 0 && (
        <div className="content">
          <div className="table-section">
            <h2>Discovered Devices ({devices.length})</h2>
            <DeviceTable
              devices={devices}
              onSelectDevice={setSelectedDevice}
              selectedDeviceId={selectedDevice?.id}
            />
          </div>

          {selectedDevice && (
            <div className="details-section">
              <DeviceDetails
                device={selectedDevice}
                onClose={() => setSelectedDevice(null)}
              />
            </div>
          )}
        </div>
      )}

      {!scanning && devices.length === 0 && (
        <div className="empty-state">
          <p>👋 Welcome! Enter a CIDR range or use the default to start scanning.</p>
          <p>Example: 192.168.1.0/24</p>
        </div>
      )}
    </div>
  );
}

export default App;
