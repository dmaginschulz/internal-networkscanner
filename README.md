# 🌐 Internal Network Scanner

A full-stack network scanning application for discovering and analyzing devices in your local network. Built with .NET Core 9 and React + TypeScript.

## ✨ Features

- **Network Scanning**
  - CIDR notation support (e.g., `192.168.1.0/24`)
  - IP range scanning (e.g., `192.168.1.1` to `192.168.1.254`)
  - Configurable default network ranges
  - Concurrent scanning with configurable thread limits

- **Device Discovery**
  - IPv4 and IPv6 address detection
  - Hostname resolution
  - MAC address lookup
  - Device type identification (Computer, Server, Router, Printer, etc.)
  - Operating system detection
  - Port scanning for common services

- **User Interface**
  - Responsive React-based frontend
  - Light/Dark theme toggle
  - Sortable and filterable device table
  - Search functionality (IP, hostname, MAC, device type)
  - Detailed device information panel
  - Excel export functionality
  - Real-time scan progress indication

- **Performance & Caching**
  - In-memory caching for scanned devices
  - Configurable cache expiration
  - Optimized concurrent scanning

## 🛠️ Technology Stack

### Backend

- **.NET Core 9** - Modern, cross-platform framework
- **FastEndpoints** - High-performance REST API framework
- **Serilog** - Structured logging
- **FluentValidation** - Input validation
- **System.Net.NetworkInformation** - Network scanning capabilities

### Frontend

- **React 18** - UI library
- **TypeScript** - Type-safe JavaScript
- **Vite** - Fast build tool
- **Axios** - HTTP client
- **@tanstack/react-table** - Advanced table functionality
- **xlsx** - Excel export
- **@xyflow/react** - Network diagram visualization

## 📋 Prerequisites

- **.NET SDK 9.0** or higher
- **Node.js 18** or higher
- **npm** or **yarn**
- Administrator/elevated privileges (required for ICMP ping and raw socket access on Windows)

## 🚀 Installation

### 1. Clone the Repository

```bash
git clone https://github.com/yourusername/internal-networkscanner.git
cd internal-networkscanner
```

### 2. Backend Setup

```bash
cd backend/src/NetworkScanner.Api
dotnet restore
dotnet build
```

### 3. Frontend Setup

```bash
cd frontend
npm install
```

## ⚙️ Configuration

### Backend Configuration

Edit `backend/src/NetworkScanner.Api/appsettings.json`:

```json
{
  "ScannerConfiguration": {
    "NetworkCidr": "172.30.241.0/24",      // Default network to scan
    "PingTimeoutMs": 1000,                 // Ping timeout in milliseconds
    "PortScanTimeoutMs": 500,              // Port scan timeout
    "MaxConcurrentScans": 50,              // Max concurrent scan threads
    "CommonPorts": [21, 22, 23, 80, 443, 445, 3389, 8080, 8443],
    "CacheExpirationMinutes": 60
  },
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://localhost:5000"
      }
    }
  },
  "Cors": {
    "AllowedOrigins": ["http://localhost:5173"]
  }
}
```

### Frontend Configuration

Create `frontend/.env.development`:

```env
VITE_API_URL=http://localhost:5000
```

## 🏃 Running the Application

### Start Backend

```bash
cd backend/src/NetworkScanner.Api
dotnet run
```

The API will be available at `http://localhost:5000`

### Start Frontend

```bash
cd frontend
npm run dev
```

The frontend will be available at `http://localhost:5173`

## 📖 Usage

### Basic Network Scan

1. Open the application in your browser at `http://localhost:5173`
2. Choose scan mode:
   - **IP Range**: Enter start and end IP addresses
   - **CIDR**: Enter network in CIDR notation
3. Click "🔍 Netzwerk Scannen" to start scanning
4. View discovered devices in the table

### Advanced Features

- **Search**: Use the search bar to filter devices by IP, hostname, MAC, or device type
- **Sort**: Click on table headers to sort by any column
- **Details**: Click on a device row to view detailed information
- **Export**: Click "📊 Export Excel" to download device list as Excel file
- **Theme**: Toggle between light and dark mode using the ☀️/🌙 button

### Scan Modes

#### IP Range Mode

```text
Start IP: 192.168.1.1
End IP: 192.168.1.254
```

Scans all IPs from start to end (inclusive).

#### CIDR Mode

```text
CIDR: 192.168.1.0/24
```

Scans the entire subnet specified in CIDR notation.

#### Default Scan

Leave both fields empty to use the default network from `appsettings.json`.

## 🔌 API Documentation

### Endpoints

#### POST /api/scan

Scan a network range or CIDR block.

**Query Parameters:**

- `cidr` (optional): CIDR notation or IP range (e.g., `192.168.1.0/24` or `192.168.1.1-192.168.1.254`)

**Response:**

```json
{
  "devices": [
    {
      "id": "172.30.241.104",
      "ipv4Addresses": ["172.30.241.104"],
      "ipv6Addresses": [],
      "hostname": "NB-1077.schulz-elektrotechnik.de",
      "macAddress": "00:11:22:33:44:55",
      "openPorts": [
        {
          "portNumber": 80,
          "protocol": "TCP",
          "serviceName": "HTTP",
          "state": "Open"
        }
      ],
      "deviceType": "Computer",
      "operatingSystem": "Windows 10",
      "lastSeen": "2026-02-10T18:08:01.123Z",
      "firstDiscovered": "2026-02-10T18:08:01.123Z",
      "isOnline": true
    }
  ],
  "totalDevicesFound": 4,
  "scanStartTime": "2026-02-10T18:08:00.123Z",
  "scanEndTime": "2026-02-10T18:08:01.123Z",
  "networkScanned": "172.30.241.100-172.30.241.110"
}
```

#### GET /api/devices/{ipAddress}

Get detailed information about a specific device.

**Response:**

```json
{
  "id": "172.30.241.104",
  "ipv4Addresses": ["172.30.241.104"],
  "hostname": "NB-1077.schulz-elektrotechnik.de",
  "macAddress": "00:11:22:33:44:55",
  "openPorts": [...],
  "deviceType": "Computer",
  "operatingSystem": "Windows 10",
  "isOnline": true
}
```

#### Swagger Documentation

Available in development mode at: `http://localhost:5000/swagger`

## 📁 Project Structure

```text
internal-networkscanner/
├── backend/
│   └── src/
│       └── NetworkScanner.Api/
│           ├── Configuration/          # Configuration models
│           ├── Endpoints/             # FastEndpoints API endpoints
│           │   ├── NetworkScan/
│           │   └── DeviceDetails/
│           ├── Models/                # Domain models
│           │   ├── Device.cs
│           │   ├── DeviceType.cs
│           │   ├── NetworkPort.cs
│           │   └── PortState.cs
│           ├── Services/              # Business logic
│           │   ├── NetworkScannerService.cs
│           │   ├── PortScannerService.cs
│           │   ├── DeviceDiscoveryService.cs
│           │   └── DeviceRepository.cs
│           ├── Program.cs             # Application entry point
│           └── appsettings.json       # Configuration
├── frontend/
│   └── src/
│       ├── components/               # React components
│       │   ├── DeviceTable/
│       │   └── DeviceDetails/
│       ├── hooks/                    # Custom React hooks
│       │   ├── useNetworkScan.ts
│       │   └── useDevices.ts
│       ├── services/                 # API services
│       │   ├── api.ts
│       │   ├── deviceService.ts
│       │   └── exportService.ts
│       ├── types/                    # TypeScript types
│       │   ├── Device.ts
│       │   ├── DeviceType.ts
│       │   └── ApiResponse.ts
│       ├── App.tsx                   # Main application component
│       ├── App.css                   # Application styles
│       └── index.css                 # Global styles with theme variables
├── CLAUDE.md                         # Project documentation for Claude AI
└── README.md                         # This file
```

## 🔒 Security Considerations

- **Elevated Privileges**: The scanner requires administrator/root privileges for ICMP ping and raw socket access
- **Network Access**: Ensure the application is only accessible on trusted networks
- **Input Validation**: All IP addresses and CIDR inputs are validated using FluentValidation
- **CORS**: Configure allowed origins appropriately for production
- **Rate Limiting**: Consider implementing rate limiting on the scan endpoint in production

## 🎨 Theming

The application supports both light and dark themes:

- Default theme: Dark
- Theme preference is saved in browser's localStorage
- Toggle between themes using the ☀️/🌙 button in the header

### CSS Variables

All colors are defined using CSS variables for easy customization:

```css
/* Dark Theme */
--bg-primary: #242424;
--bg-secondary: #1a1a1a;
--text-primary: rgba(255, 255, 255, 0.87);

/* Light Theme */
--bg-primary: #ffffff;
--bg-secondary: #f5f5f5;
--text-primary: #213547;
```

## 🛠️ Development

### Build Frontend for Production

```bash
cd frontend
npm run build
```

### Run Tests

```bash
# Backend tests (if available)
cd backend
dotnet test

# Frontend tests (if available)
cd frontend
npm test
```

### Code Style

- Backend: Follow C# coding conventions
- Frontend: ESLint + Prettier configuration
- TypeScript: Strict mode enabled

## 🐛 Troubleshooting

### "Access Denied" Errors

Run the backend with administrator/elevated privileges:

```bash
# Windows (PowerShell as Administrator)
cd backend/src/NetworkScanner.Api
dotnet run

# Linux/macOS
sudo dotnet run
```

### Frontend Not Connecting to Backend

1. Verify backend is running on `http://localhost:5000`
2. Check CORS configuration in `appsettings.json`
3. Verify `VITE_API_URL` in `.env.development`

### No Devices Found

1. Verify the network CIDR matches your actual network
2. Check firewall settings (ICMP must be allowed)
3. Ensure you have network connectivity
4. Try a smaller IP range first

### Theme Not Working

1. Clear browser localStorage: `localStorage.clear()`
2. Hard refresh the page: `Ctrl+Shift+R` or `Cmd+Shift+R`

## 📄 License

This project is private and proprietary. All rights reserved.

## 🤝 Contributing

This is an internal project. For questions or contributions, please contact the development team.

## 📞 Support

For issues or questions, please:

1. Check the troubleshooting section above
2. Review the [CLAUDE.md](CLAUDE.md) file for detailed technical documentation
3. Contact the development team

---

## Credits

Built with ❤️ using .NET Core 9, React, and TypeScript

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
