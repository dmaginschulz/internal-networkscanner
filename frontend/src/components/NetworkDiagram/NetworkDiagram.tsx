import { useCallback, useMemo, useState, useEffect } from 'react';
import {
  ReactFlow,
  Node,
  Edge,
  Background,
  Controls,
  MiniMap,
  useNodesState,
  useEdgesState,
  ConnectionLineType,
  NodeTypes,
} from '@xyflow/react';
import '@xyflow/react/dist/style.css';
import { Device } from '../../types/Device';
import { DeviceType } from '../../types/DeviceType';
import DeviceNode from './DeviceNode';
import './NetworkDiagram.css';

export type TopologyMode = 'gateway' | 'lldp' | 'inference';

interface NetworkDiagramProps {
  devices: Device[];
  onSelectDevice: (device: Device | null) => void;
  selectedDeviceId?: string;
}

const nodeTypes = {
  device: DeviceNode,
} satisfies NodeTypes;

const getDeviceIcon = (type: DeviceType): string => {
  switch (type) {
    case DeviceType.Router:
      return '🔀';
    case DeviceType.Switch:
      return '🔌';
    case DeviceType.Server:
      return '🖥️';
    case DeviceType.Computer:
      return '💻';
    case DeviceType.Printer:
      return '🖨️';
    case DeviceType.MobileDevice:
      return '📱';
    case DeviceType.IoTDevice:
      return '💡';
    case DeviceType.NetworkStorage:
      return '💾';
    default:
      return '❓';
  }
};

const getDeviceColor = (type: DeviceType): string => {
  switch (type) {
    case DeviceType.Router:
      return '#ff6b6b';
    case DeviceType.Switch:
      return '#ffa500';
    case DeviceType.Server:
      return '#4ecdc4';
    case DeviceType.Computer:
      return '#95e1d3';
    case DeviceType.Printer:
      return '#f38181';
    case DeviceType.MobileDevice:
      return '#aa96da';
    case DeviceType.IoTDevice:
      return '#fcbad3';
    case DeviceType.NetworkStorage:
      return '#ffffd2';
    default:
      return '#666';
  }
};

const NetworkDiagram = ({ devices, onSelectDevice, selectedDeviceId }: NetworkDiagramProps) => {
  const [topologyMode, setTopologyMode] = useState<TopologyMode>('gateway');

  // Group devices by subnet
  const devicesBySubnet = useMemo(() => {
    const subnets = new Map<string, Device[]>();

    devices.forEach(device => {
      const subnet = device.ipv4Addresses[0]?.split('.').slice(0, 3).join('.') || 'unknown';
      if (!subnets.has(subnet)) {
        subnets.set(subnet, []);
      }
      subnets.get(subnet)!.push(device);
    });

    return subnets;
  }, [devices]);

  // Create edges based on topology mode
  const createEdges = useCallback((mode: TopologyMode): Edge[] => {
    const edges: Edge[] = [];

    switch (mode) {
      case 'gateway':
        // Gateway-based connections
        devices.forEach(device => {
          if (device.defaultGateway) {
            const gatewayDevice = devices.find(d =>
              d.ipv4Addresses.includes(device.defaultGateway!)
            );

            if (gatewayDevice) {
              edges.push({
                id: `e-${gatewayDevice.id}-${device.id}`,
                source: gatewayDevice.id,
                target: device.id,
                type: ConnectionLineType.Straight,
                animated: device.isOnline,
                style: { stroke: device.isOnline ? '#4ecdc4' : '#666' },
                label: 'Gateway',
              });
            }
          }
        });
        break;

      case 'lldp':
        // LLDP/CDP based connections (to be implemented)
        devices.forEach(device => {
          device.connectedTo.forEach(connectedId => {
            const connectedDevice = devices.find(d => d.id === connectedId);
            if (connectedDevice) {
              edges.push({
                id: `e-${device.id}-${connectedId}`,
                source: device.id,
                target: connectedId,
                type: ConnectionLineType.Straight,
                animated: device.isOnline && connectedDevice.isOnline,
                style: { stroke: '#4ecdc4' },
                label: 'Physical',
              });
            }
          });
        });
        break;

      case 'inference':
        // Inference-based connections (subnet-based fallback)
        devicesBySubnet.forEach((subnetDevices) => {
          const routers = subnetDevices.filter(d =>
            d.deviceType === DeviceType.Router || d.deviceType === DeviceType.Switch
          );
          const otherDevices = subnetDevices.filter(d =>
            d.deviceType !== DeviceType.Router && d.deviceType !== DeviceType.Switch
          );

          otherDevices.forEach(device => {
            if (routers.length > 0) {
              edges.push({
                id: `e-${routers[0].id}-${device.id}`,
                source: routers[0].id,
                target: device.id,
                type: ConnectionLineType.Straight,
                animated: device.isOnline,
                style: { stroke: device.isOnline ? '#4ecdc4' : '#666' },
                label: 'Inferred',
              });
            }
          });
        });
        break;
    }

    return edges;
  }, [devices, devicesBySubnet]);

  // Create nodes and edges
  const { initialNodes, initialEdges } = useMemo(() => {
    const nodes: Node[] = [];

    let yOffset = 0;
    const subnetSpacing = 400;

    // Create a central router/gateway node for each subnet
    devicesBySubnet.forEach((subnetDevices) => {
      const routers = subnetDevices.filter(d => d.deviceType === DeviceType.Router || d.deviceType === DeviceType.Switch);
      const otherDevices = subnetDevices.filter(d => d.deviceType !== DeviceType.Router && d.deviceType !== DeviceType.Switch);

      // Add gateway/router nodes
      routers.forEach((router, idx) => {
        nodes.push({
          id: router.id,
          type: 'device',
          position: { x: 400, y: yOffset + idx * 150 },
          data: {
            device: router,
            icon: getDeviceIcon(router.deviceType),
            color: getDeviceColor(router.deviceType),
            selected: router.id === selectedDeviceId,
          },
        });
      });

      const gatewayY = yOffset + (routers.length * 150) / 2;

      // Add other devices in a circular pattern around the gateway
      const radius = 300;
      otherDevices.forEach((device, idx) => {
        const angle = (idx / otherDevices.length) * 2 * Math.PI;
        const x = 400 + radius * Math.cos(angle);
        const y = gatewayY + radius * Math.sin(angle);

        nodes.push({
          id: device.id,
          type: 'device',
          position: { x, y },
          data: {
            device,
            icon: getDeviceIcon(device.deviceType),
            color: getDeviceColor(device.deviceType),
            selected: device.id === selectedDeviceId,
          },
        });
      });

      yOffset += subnetSpacing;
    });

    const edges = createEdges(topologyMode);

    return { initialNodes: nodes, initialEdges: edges };
  }, [devices, devicesBySubnet, selectedDeviceId, topologyMode, createEdges]);

  const [nodes, setNodes, onNodesChange] = useNodesState(initialNodes);
  const [edges, setEdges, onEdgesChange] = useEdgesState(initialEdges);

  // Update edges when topology mode changes
  useEffect(() => {
    const newEdges = createEdges(topologyMode);
    setEdges(newEdges);
  }, [topologyMode, createEdges, setEdges]);

  // Update nodes when selection changes
  useMemo(() => {
    setNodes(nodes =>
      nodes.map(node => ({
        ...node,
        data: {
          ...node.data,
          selected: node.id === selectedDeviceId,
        },
      }))
    );
  }, [selectedDeviceId, setNodes]);

  const onNodeClick = useCallback(
    (_event: React.MouseEvent, node: Node) => {
      const device = devices.find(d => d.id === node.id);
      if (device) {
        onSelectDevice(device);
      }
    },
    [devices, onSelectDevice]
  );

  const onPaneClick = useCallback(() => {
    onSelectDevice(null);
  }, [onSelectDevice]);

  return (
    <div className="network-diagram">
      <ReactFlow
        nodes={nodes}
        edges={edges}
        onNodesChange={onNodesChange}
        onEdgesChange={onEdgesChange}
        onNodeClick={onNodeClick}
        onPaneClick={onPaneClick}
        nodeTypes={nodeTypes}
        connectionLineType={ConnectionLineType.Straight}
        fitView
        minZoom={0.1}
        maxZoom={2}
      >
        <Background />
        <Controls />
        <MiniMap
          nodeColor={(node) => {
            const device = devices.find(d => d.id === node.id);
            return device ? getDeviceColor(device.deviceType) : '#666';
          }}
          maskColor="rgba(0, 0, 0, 0.6)"
        />
      </ReactFlow>
      <div className="diagram-legend">
        <h4>Topologie-Modus</h4>
        <div className="topology-mode-selector">
          <button
            className={topologyMode === 'gateway' ? 'active' : ''}
            onClick={() => setTopologyMode('gateway')}
            title="Gateway-basierte Verbindungen"
          >
            🌐 Gateway
          </button>
          <button
            className={topologyMode === 'lldp' ? 'active' : ''}
            onClick={() => setTopologyMode('lldp')}
            title="LLDP/CDP physische Verbindungen"
          >
            🔌 LLDP/CDP
          </button>
          <button
            className={topologyMode === 'inference' ? 'active' : ''}
            onClick={() => setTopologyMode('inference')}
            title="Intelligente Inferenz"
          >
            🧠 Inferenz
          </button>
        </div>

        <h4 style={{ marginTop: '1rem' }}>Gerätetypen</h4>
        <div className="legend-items">
          {Object.values(DeviceType).map(type => (
            <div key={type} className="legend-item">
              <span className="legend-icon" style={{ color: getDeviceColor(type) }}>
                {getDeviceIcon(type)}
              </span>
              <span>{type}</span>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
};

export default NetworkDiagram;
