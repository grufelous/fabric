import { useState, useCallback, useEffect } from 'react';
import { useConnectionProvider } from "../contexts";
import { ClientEvents, HostEvents } from '../constants';

export const ConnectionUI = () => {
    const { connection, state } = useConnectionProvider();

    const [messages, setMessages] = useState<string[]>([]);
    const [input, setInput] = useState<string>('');

    const handleSend = useCallback((messageToSend: string) => {
        connection?.invoke(HostEvents.SEND_MESSAGE, messageToSend);
        setInput('');
    }, [connection]);

    const updateReceivedMessages = useCallback((message: string) => {
        setMessages(currentMessages => [...currentMessages, message]);
    }, []);


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
            <h4>Connection state: {state}</h4>
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
