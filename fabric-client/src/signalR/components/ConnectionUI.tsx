import { useState, useCallback, useEffect, useRef } from 'react';
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import { useConnectionProvider } from "../contexts";
import { ClientEvents, HostEvents } from '../constants';

export const ConnectionUI = () => {
    const { connection, state } = useConnectionProvider();

    const [messages, setMessages] = useState<string[]>([]);
    const [input, setInput] = useState<string>('');

    const handleSend = useCallback((messageToSend: string) => {
        console.log("Sending...");
        console.log(connection);
        connection?.invoke('SendMessageToAgent', messageToSend);
        setInput('');
    }, [connection]);

    const updateReceivedMessages = useCallback((message: string) => {
        setMessages(currentMessages => [...currentMessages, message]);
    }, []);

    const handleStart = useCallback(() => {
        console.log("Starting...");
        connection?.invoke('StartAgent');
    }, [connection]);

    const handleStop = useCallback(() => {
        console.log("Stopping...");
        connection?.invoke('StopAgent');
    }, [connection]);

    useEffect(() => {
        console.log("🚀 ~ Connection state is: ", connection?.state);
        console.log("🚀 ~ Turning on RECEIVE_MESSAGE");
        connection?.on(ClientEvents.RECEIVE_MESSAGE, updateReceivedMessages);

        return () => {
            console.log("🚀 ~ Turning off RECEIVE_MESSAGE");
            connection?.off(ClientEvents.RECEIVE_MESSAGE);
        }
    }, [connection]);

    return (
        <div className='flex flex-col'>
            {/* <h4>Connection state: {state}</h4> */}
            <div className='flex'>
                <button onClick={handleStart}>Start process</button>
                <button onClick={handleStop}>Kill process</button>
            </div>
            <ul>
                {messages.map(message => (
                    <li>{message}</li>
                ))}
            </ul>
            <input
                className='border-1'
                value={input}
                onChange={e => setInput(e.target.value)}
                placeholder='Enter message to send...'
            />
            <button onClick={() => handleSend(input)}>Send message</button>
        </div>
    );
};
