import { useEffect, useState } from "react"
import { HubConnection, HubConnectionState } from "@microsoft/signalr"
import { getConnectionTo } from "./webSocketsHelper"

export function useWebsocketConnection(url: string, params?: UseWebsocketHookParams) {
    const [connection, setConnection] = useState<HubConnection>()
    const [loading, setLoading] = useState(true)
    const [error, setError] = useState<any>(null)

    useEffect(() => {
        setConnection(getConnectionTo(url))
        if (params?.whenCreatingConnection) params.whenCreatingConnection()
    }, [])

    useEffect(() => {
        if (!connection || connection.state !== HubConnectionState.Disconnected) return () => { }

        setLoading(true)
        setError(null)
        if (params?.whenConnectionCreated) params.whenConnectionCreated(connection)

        connection.start()
            .then(() => {
                if (params?.whenConnected)
                    params.whenConnected(connection)
            })
            .catch(e => {
                if (params?.debugInConsole) {
                    console.log('Connection to hub failed with error: ')
                    console.log(e)
                }
                setError(e?.toString())
            })
            .finally(() => setLoading(false))

        return () => {
            connection.stop()
        }
    }, [connection])

    return { connection, loading, setLoading, error, setError }
}



type UseWebsocketHookParams = {
    debugInConsole?: boolean,
    whenCreatingConnection?: () => void,
    whenConnectionCreated?: (connection: HubConnection) => void,
    whenConnected?: (connection: HubConnection) => void,
}