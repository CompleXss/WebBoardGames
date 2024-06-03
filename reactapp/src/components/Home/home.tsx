import { useEffect, useState } from 'react'
import { NavLink, useNavigate } from 'react-router-dom'
import { useWebsocketConnection } from 'src/utilities/useWebsocketHook'
import { GameNamesRu, Games } from 'src/utilities/Api_DataTypes'
import { User as PlayerInfo } from 'src/utilities/Api_DataTypes';
import { addMeta } from 'src/utilities/utils'
import { getUserInfoByID } from 'src/utilities/EndpointHelpers';
import { HubConnection } from '@microsoft/signalr';
import Loading from 'src/components/Loading/loading'
import ENDPOINTS from 'src/utilities/Api_Endpoints'
import './home.css'
import './lobbyList.css'

interface LobbyInfo {
    hostID: string
    playersCount: number
    maxPlayersCount: number
    isFull: boolean
    key: string
}

const GET_LOBBY_LIST_INTERVAL = 2000
let interval: string | number | NodeJS.Timeout | undefined = undefined


export default function Home() {
    const navigate = useNavigate()
    const [playerInfos, setPlayerInfos] = useState<Map<string, PlayerInfo>>(new Map())
    const [gameName, setGameName] = useState<Games>(Games.checkers)
    const [lobbyList, setLobbyList] = useState<LobbyInfo[]>([])

    useEffect(() => {
        document.title = 'Главная'
        addMeta('description', 'Настольные игры онлайн')
        addMeta('keywords', 'Настольные игры, шашки, монополия')
    }, [])

    useEffect(() => {
        getLobbyList(connection, gameName)
        clearInterval(interval)
        interval = setInterval(() => getLobbyList(connection, gameName), GET_LOBBY_LIST_INTERVAL)
    }, [gameName])

    const { connection, loading, error } = useWebsocketConnection(ENDPOINTS.Hubs.LOBBY_LIST, {
        whenConnected: () => {
            getLobbyList(connection, gameName)
            interval = setInterval(() => getLobbyList(connection, gameName), GET_LOBBY_LIST_INTERVAL)
        }
    })

    async function getLobbyList(connection: HubConnection | undefined, gameName: string) {
        if (!connection) return

        return connection.invoke('GetLobbiesForGame', gameName)
            .then(response => {
                if (response?.statusCode === 200) {
                    setLobbyList(response?.value?.lobbies ?? [])
                }
            })
            .catch(_ => { })
    }

    const lobbies = lobbyList.map((x, i) => {
        if (!playerInfos?.has(x.hostID)) {
            getUserInfoByID(x.hostID)
                .then(info => {
                    setPlayerInfos(infos => {
                        infos.set(x.hostID, info)
                        return new Map(infos)
                    })
                })
                .catch(_ => { })
        }

        return (
            <div className='lobby' key={i}>
                <div className='playersCount'>{x.playersCount} / {x.maxPlayersCount}</div>
                <div className='playerName'>{playerInfos.get(x.hostID)?.name ?? '???'}</div>
                <button onClick={() => {
                    navigate('/lobby/' + gameName, { state: { key: x.key } })
                }}>Зайти</button>
            </div>
        )
    })

    const gameNames: JSX.Element[] = []
    let i = 0
    GameNamesRu.forEach((displayName, codeName) => {
        gameNames.push((
            <option value={codeName} key={i++}>{displayName}</option>
        ))
    })



    return <div className="home">
        <h1>Во что сыграем сегодня?</h1>

        <div className="gamesContainer">
            <NavLink className="game checkers" to={'/lobby/checkers'}>
                <p>Шашки</p>
            </NavLink>

            <NavLink className="game monopoly" to={'/lobby/monopoly'}>
                <p>Монополия</p>
            </NavLink>
        </div>

        <h1>Активные лобби</h1>
        <div style={{ textAlign: 'center' }}>
            {loading ? <Loading></Loading> :
                error ? <h2>Не удалось загрузить список лобби</h2> :

                    <div className='lobbyList'>
                        <select value={gameName} onChange={x => {
                            const value = (x.currentTarget.value) as Games
                            if (value in Games) {
                                setGameName(value)
                            }
                        }}>
                            {gameNames}
                        </select>

                        {lobbies?.length > 0 ? lobbies : <h2>Нет активных лобби</h2>}
                    </div>
            }
        </div>
    </div>
}
