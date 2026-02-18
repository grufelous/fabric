import { useRef, useState, useEffect, useMemo, useCallback } from 'react';
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import { ClientEvents } from '../constants';

export const useSignalRConnection = () => {
    const [connection, setConnection] = useState<HubConnection | null>(null);
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
            newConnection.start()
                .then(() => {
                    console.log("🚀 ~ SignalR connection established!");
                })
                .catch((error: any) => {
                    console.error("🚀 ~ SignalR connection failed!", error);
                });

            setConnection(newConnection);
        };

        return () => {
            connection?.stop();
        };
    }, []);

    const connectionState = useMemo(() => connection?.state, [connection?.state]);
    console.log("🚀 ~ useSignalRConnection ~ connection:", connection)

    return ({
        connection,
        state: connectionState,
    });
};
