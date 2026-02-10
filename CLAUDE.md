# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Internal Network Scanner - A full-stack application to scan and visualize all devices in a network.

**Architecture:**

- Backend: .NET Core 9 REST API (can be deployed to multiple networks)
- Frontend: React + TypeScript web application
- Communication: REST API

## Technology Stack

### Backend

- Framework: .NET Core 9
- API: [FastEndpoints](https://fast-endpoints.com/)
- NuGet Package: [FastEndpoints](https://www.nuget.org/packages/FastEndpoints/)

### Frontend

- Framework: React with TypeScript
- Network Diagram: [React Flow](https://reactflow.dev/)
- Table Features: Sortable, filterable, and exportable (Excel format)

## Project Structure

```plaintext
internal-networkscanner/
├── backend/              # .NET Core 9 API
│   ├── src/
│   │   ├── Endpoints/   # FastEndpoints API endpoints
│   │   ├── Services/    # Network scanning logic
│   │   └── Models/      # Device and network models
│   └── .gitignore
├── frontend/            # React + TypeScript app
│   ├── src/
│   │   ├── components/  # React components
│   │   ├── services/    # API client
│   │   └── types/       # TypeScript types
│   └── .gitignore
└── CLAUDE.md
```

## API Endpoints

The backend provides these REST API endpoints:

1. **Network Scan**
   - Scans the network and returns all discovered devices
   - Returns device list with basic information

2. **Device Details**
   - Search and query device details by IPv4 or IPv6 address
   - Returns comprehensive device information including:
     - IP addresses (IPv4 and IPv6)
     - Hostname
     - MAC address
     - Open ports
     - Device type/OS information
     - Last seen timestamp

## Development Commands

### Backend (.NET Core 9)

```bash
# Navigate to backend directory
cd backend

# Restore dependencies
dotnet restore

# Build
dotnet build

# Run the API
dotnet run --project src/NetworkScanner.Api

# Run with hot reload
dotnet watch run --project src/NetworkScanner.Api

# Run tests
dotnet test

# Run specific test
dotnet test --filter "FullyQualifiedName~NetworkScanner.Tests.ScannerServiceTests.ShouldScanNetwork"

# Clean build artifacts
dotnet clean
```

### Frontend (React + TypeScript)

```bash
# Navigate to frontend directory
cd frontend

# Install dependencies
npm install

# Run development server
npm run dev

# Build for production
npm run build

# Run tests
npm test

# Lint code
npm run lint

# Type check
npm run type-check
```

## Frontend Features

### Device Table View

- Tabellarische Darstellung aller Netzwerkgeräte
- Sortierbar nach allen Spalten
- Filterbar nach verschiedenen Kriterien
- Exportierbar im Excel-Format

### Device Search

- Suche nach Geräten über verschiedene Felder (IP, Hostname, MAC)
- Echtzeit-Filterung der Ergebnisse

### Device Details

- Detail-Ansicht beim Auswählen eines Geräts
- Zeigt alle verfügbaren Informationen des Geräts

### Network Diagram

- Visuelle Darstellung des Netzwerks mit React Flow
- Zeigt Geräte und ihre Verbindungen

## Important Implementation Notes

### Backend Implementation

- FastEndpoints verwendet eine endpoint-per-file Struktur
- Endpoints erben von `Endpoint<TRequest, TResponse>`
- Keine Controller nötig - FastEndpoints ersetzt MVC pattern
- Network scanning sollte asynchron implementiert werden
- IPv4 und IPv6 Support ist erforderlich

### Frontend Implementation

- Verwende React Hooks (useState, useEffect, useMemo)
- TypeScript strict mode ist aktiviert
- API calls sollten über einen zentralen API client erfolgen
- Tabellen-Komponenten sollten wiederverwendbar sein
- React Flow für Netzwerk-Diagramm Visualisierung

### Development Environment Configuration

- Windows-basierte Entwicklungsumgebung
- .env Dateien für lokale Konfiguration (werden ignoriert)
- Backend und Frontend laufen auf verschiedenen Ports während der Entwicklung

## Configuration

### Backend Configuration

- API Port und CORS-Einstellungen in appsettings.json
- Netzwerk-Scan Konfiguration (Timeout, Thread-Anzahl, etc.)

### Frontend Configuration

- Backend API URL in .env Datei konfigurieren
- Beispiel: `VITE_API_URL=http://localhost:5000`
