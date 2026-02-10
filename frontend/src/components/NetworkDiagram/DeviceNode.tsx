import { memo } from 'react';
import { Handle, Position, NodeProps } from '@xyflow/react';
import { Device } from '../../types/Device';

export interface DeviceNodeData extends Record<string, unknown> {
  device: Device;
  icon: string;
  color: string;
  selected: boolean;
}

const DeviceNode = ({ data }: NodeProps) => {
  const { device, icon, color, selected } = data as unknown as DeviceNodeData;

  return (
    <div className={`device-node ${selected ? 'selected' : ''} ${device.isOnline ? 'online' : 'offline'}`}>
      <Handle type="target" position={Position.Top} />

      <div className="device-node-content" style={{ borderColor: color }}>
        <div className="device-icon" style={{ color }}>
          {icon}
        </div>
        <div className="device-info">
          <div className="device-name">
            {device.hostname || device.ipv4Addresses[0] || device.ipv6Addresses[0]}
          </div>
          <div className="device-ip">
            {device.ipv4Addresses[0]}
          </div>
          {device.macAddress && (
            <div className="device-mac">
              {device.macAddress.substring(0, 17)}
            </div>
          )}
        </div>
        <div className="device-status">
          {device.isOnline ? (
            <span className="status-indicator online" title="Online">●</span>
          ) : (
            <span className="status-indicator offline" title="Offline">●</span>
          )}
        </div>
      </div>

      <Handle type="source" position={Position.Bottom} />
    </div>
  );
};

export default memo(DeviceNode);
