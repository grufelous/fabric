import { useRef, useEffect, useMemo, useCallback } from 'react';
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import { ClientEvents } from '../constants';

export const useSignalRConnection = () => {
    const connection = useRef<HubConnection>(null);
    const isInitialized = useRef<boolean>(false);

    const onConnectedHandler = useCallback((...args: any[]) => {
        console.log('Received on connection: ', ...args);
    }, []);

    useEffect(() => {
        if(!isInitialized.current) {
            isInitialized.current = true;
            const newConnection = new HubConnectionBuilder()
                .withUrl('http://localhost:5000/fabric_core_hub', {
                    withCredentials: true,
                })
                .withAutomaticReconnect()
                .build();
            
            newConnection.on(ClientEvents.CONNECTED, onConnectedHandler);

            connection.current = newConnection;
            connection.current.start()
                .then(() => {
                    console.log("🚀 ~ SignalR connection established!");
                })
                .catch((error: any) => {
                    console.error("🚀 ~ SignalR connection failed!", error);
                });
        };

        return () => {
            connection.current?.stop();
        };
    }, []);

    const connectionState = useMemo(() => connection.current?.state, [connection.current?.state]);

    return useMemo(() => ({
        connection: connection.current,
        state: connectionState,
    }), [connection, connectionState]);
};
