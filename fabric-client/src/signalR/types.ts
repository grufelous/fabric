import type { HubConnection, HubConnectionState } from '@microsoft/signalr';

export type ConnectionCtxValue = {
    connection: HubConnection | null;
    state: HubConnectionState | undefined;
};
