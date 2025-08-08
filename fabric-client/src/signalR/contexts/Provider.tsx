import { createContext, useMemo, useContext } from "react";
import { useSignalRConnection } from "../hooks/useSignalRConnection";

import type { ReactNode } from "react";
import type { ConnectionCtxValue } from "../types";

const ConnectionContext = createContext<ConnectionCtxValue>({} as ConnectionCtxValue);

export const ConnectionProvider = ({ children } : {children: ReactNode }) => {
    const value = useSignalRConnection();
    console.log("🚀 ~ ConnectionProvider ~ value:", value)

    return (
        <ConnectionContext.Provider value={value}>
            {children}
        </ConnectionContext.Provider>
    );
};

export const useConnectionProvider = () => useContext(ConnectionContext);
