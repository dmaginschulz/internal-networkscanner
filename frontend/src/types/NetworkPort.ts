export interface NetworkPort {
  portNumber: number;
  protocol: string;
  serviceName: string | null;
  state: string;
}
