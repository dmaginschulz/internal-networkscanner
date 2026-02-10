import { useState } from 'react';
import { useNetworkScan } from './hooks/useNetworkScan';
import { useDevices } from './hooks/useDevices';
import DeviceTable from './components/DeviceTable/DeviceTable';
import DeviceDetails from './components/DeviceDetails/DeviceDetails';
import { exportToExcel } from './services/exportService';
import './App.css';

function App() {
  const [scanMode, setScanMode] = useState<'cidr' | 'range'>('range');
  const [cidr, setCidr] = useState('');
  const [startIp, setStartIp] = useState('');
  const [endIp, setEndIp] = useState('');
  const { scanning, error, startScan } = useNetworkScan();
  const { devices, selectedDevice, setSelectedDevice, addDevices } = useDevices();

  const handleScan = async () => {
    try {
      let scanParameter: string | undefined;

      if (scanMode === 'range') {
        if (startIp && endIp) {
          scanParameter = `${startIp}-${endIp}`;
        } else if (!startIp && !endIp) {
          scanParameter = undefined; // Use default from config
        } else {
          alert('Bitte geben Sie sowohl Start- als auch End-IP ein');
          return;
        }
      } else {
        scanParameter = cidr || undefined;
      }

      const result = await startScan(scanParameter);
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
        <div className="scan-mode-selector">
          <button
            className={scanMode === 'range' ? 'active' : ''}
            onClick={() => setScanMode('range')}
            disabled={scanning}
          >
            📍 IP Range
          </button>
          <button
            className={scanMode === 'cidr' ? 'active' : ''}
            onClick={() => setScanMode('cidr')}
            disabled={scanning}
          >
            🌐 CIDR
          </button>
        </div>

        <div className="scan-controls">
          {scanMode === 'range' ? (
            <>
              <input
                type="text"
                placeholder="Start IP (z.B. 192.168.1.1)"
                value={startIp}
                onChange={(e) => setStartIp(e.target.value)}
                disabled={scanning}
                className="ip-input"
              />
              <span className="range-separator">bis</span>
              <input
                type="text"
                placeholder="End IP (z.B. 192.168.1.254)"
                value={endIp}
                onChange={(e) => setEndIp(e.target.value)}
                disabled={scanning}
                className="ip-input"
              />
            </>
          ) : (
            <input
              type="text"
              placeholder="CIDR (z.B. 192.168.1.0/24)"
              value={cidr}
              onChange={(e) => setCidr(e.target.value)}
              disabled={scanning}
              className="cidr-input"
            />
          )}
          <button onClick={handleScan} disabled={scanning} className="scan-button">
            {scanning ? '🔄 Scannen...' : '🔍 Netzwerk Scannen'}
          </button>
          {devices.length > 0 && (
            <button onClick={handleExport} className="export-button">
              📊 Export Excel
            </button>
          )}
        </div>
        {error && <div className="error">{error}</div>}
        <div className="scan-hint">
          {scanMode === 'range'
            ? 'Geben Sie Start- und End-IP ein, oder lassen Sie beide leer für Standard-Scan'
            : 'Geben Sie CIDR ein, oder lassen Sie leer für Standard-Scan (192.168.1.0/24)'}
        </div>
      </div>

      {scanning && (
        <div className="scanning-indicator">
          <div className="spinner"></div>
          <p>Netzwerk wird gescannt... Dies kann einige Minuten dauern.</p>
        </div>
      )}

      {devices.length > 0 && (
        <div className="content">
          <div className="table-section">
            <h2>Gefundene Geräte ({devices.length})</h2>
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
          <p>👋 Willkommen! Wählen Sie einen Scan-Modus und starten Sie den Scan.</p>
          <p><strong>IP Range:</strong> 192.168.1.1 bis 192.168.1.254</p>
          <p><strong>CIDR:</strong> 192.168.1.0/24</p>
        </div>
      )}
    </div>
  );
}

export default App;
