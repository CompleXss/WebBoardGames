import axios from "axios"
import { useEffect, useRef, useState } from "react"
import { useNavigate } from "react-router-dom"
import { HubConnection } from "@microsoft/signalr"
import { useWebsocketConnection } from "../../../../utilities/useWebsocketHook"
import ENDPOINTS from "../../../../utilities/Api_Endpoints"
import Loading from "../../../Loading/loading"
import './checkersLobby.css'

// TODO: при обновлении страницы лобби закрывается
// TODO: lobby key click to copy

const CREATE_LOBBY = 'CreateLobby'
const ENTER_LOBBY = 'EnterLobby'
const LEAVE_LOBBY = 'LeaveLobby'

interface LobbyInfo {
    hostID: number,
    key: string,
    secondPlayerID: number | null,
}

interface PLayerInfo {
    id: number,
    name: string,
}

export default function CheckersLobby() {
    const navigate = useNavigate()

    const [lobbyKey, setLobbyKey] = useState<string>()
    const [isHost, setIsHost] = useState(false)
    const [lobbyPlayerInfos, setLobbyPlayerInfos] = useState<PLayerInfo[]>([])
    const [lobbyPlayers, setLobbyPlayers] = useState<JSX.Element[]>([])

    const joinLobbyDialog = useRef<HTMLDialogElement>(null)
    const lobbyKeyInput = useRef<HTMLInputElement>(null)
    const lobbyKeyWarningMessage = useRef<HTMLParagraphElement>(null)
    const startGameWarningMessage = useRef<HTMLParagraphElement>(null)

    useEffect(() => {
        document.title = 'Шашки (лобби)'
    }, [])

    // create connection
    const { connection, loading, setLoading, error } = useWebsocketConnection(ENDPOINTS.Hubs.CHECKERS_LOBBY, {
        whenCreatingConnection: clearLobbyInfo,
        whenConnectionCreated: addEventHandlers,
        debugInConsole: true,
    })

    // server side event handlers
    function addEventHandlers(connection: HubConnection) {
        connection.on('UserConnected', async message => {
            const userID = message as number
            const userInfo = await getUserInfoByID(userID)

            setLobbyPlayerInfos(infos => {
                return [...infos, userInfo!]
            })
        })

        connection.on('UserDisconnected', message => {
            const userID = message as number

            setLobbyPlayerInfos(infos => {
                return infos.filter(x => x.id !== userID)
            })
        })

        connection.on('LobbyClosed', () => {
            clearLobbyInfo()

            alert('Комната была закрыта!') // TODO: change alert to smth nice
        })

        connection.on('GameStarted', () => {
            navigate('/play/checkers')
        })

        connection.onreconnecting(() => setLoading(true))
        connection.onreconnected(() => setLoading(false))
        connection.onclose(() => clearLobbyInfo())
    }



    // lobby info functions
    async function trySetLobbyInfo(lobby: LobbyInfo): Promise<boolean> {
        if (!lobby) {
            return false
        }
        setLobbyKey(lobby.key)

        const hostInfo = await getUserInfoByID(lobby.hostID)

        const secondPlayerInfo = lobby.secondPlayerID
            ? await getUserInfoByID(lobby.secondPlayerID)
            : null

        const playerInfos: PLayerInfo[] = secondPlayerInfo
            ? [hostInfo, secondPlayerInfo]
            : [hostInfo]

        setLobbyPlayerInfos(playerInfos)
        return true
    }

    function clearLobbyInfo() {
        setLobbyKey(undefined)
        setIsHost(false)
        setLobbyPlayerInfos([])
    }

    useEffect(() => {
        setLobbyPlayers(lobbyPlayerInfos.map((player, index) => {
            return <p key={index}>{player.name}</p>
        }))
    }, [lobbyPlayerInfos])



    // interactivity
    function createLobby() {
        if (!connection) return

        connection.invoke(CREATE_LOBBY)
            .then(response => {
                const lobby = response?.value?.lobby as LobbyInfo
                if (!lobby) {
                    console.log(response?.value)
                    return
                }

                if (!trySetLobbyInfo(lobby)) {
                    clearLobbyInfo()
                    console.error('Can not get lobbyinfo!')
                }
                setIsHost(true)
            })
            .catch(e => console.log(e))
    }

    function showLobbyKeyWarningText(show: string | false, color?: string) {
        if (!lobbyKeyWarningMessage.current) return

        lobbyKeyWarningMessage.current.textContent = show ? show : ''

        if (show) {
            lobbyKeyWarningMessage.current.textContent = show
        }
        lobbyKeyWarningMessage.current.style.opacity = show ? '1' : '0';
        lobbyKeyWarningMessage.current.style.color = color ?? 'red'
    }

    function showStartGameWarningText(show: string | false, color?: string) {
        if (!startGameWarningMessage.current) return

        startGameWarningMessage.current.textContent = show ? show : '?'

        if (show) {
            startGameWarningMessage.current.textContent = show
        }
        startGameWarningMessage.current.style.opacity = show ? '1' : '0';
        startGameWarningMessage.current.style.color = color ?? 'red'
    }

    function showJoinDialog() {
        showLobbyKeyWarningText(false)
        joinLobbyDialog.current?.showModal()
    }

    function hideJoinDialog() {
        joinLobbyDialog.current?.close()
    }

    function joinLobby() {
        if (!connection || !lobbyKeyInput.current) return

        const lobbyKey = lobbyKeyInput.current.value
        if (!lobbyKey || lobbyKey.length !== 4) {
            showLobbyKeyWarningText('Введите код из 4 цифр')
            return
        }
        showLobbyKeyWarningText(false)

        connection.invoke(ENTER_LOBBY, lobbyKey)
            .then(async response => {
                if (response.value.lobby && await trySetLobbyInfo(response.value.lobby)) {
                    showLobbyKeyWarningText(false)
                }
                else {
                    showLobbyKeyWarningText(response.value)
                }
            })
            .catch(e => {
                showLobbyKeyWarningText('Не могу получить ответ от сервера!')
                console.log(e)
            })
    }

    function leaveLobby() {
        if (!connection) return

        connection.invoke(LEAVE_LOBBY)
            .then(clearLobbyInfo)
            .catch(e => console.log(e))
    }

    function startGame() {
        showStartGameWarningText(false)

        connection?.invoke('StartGame')
            .then(response => {
                if (response.statusCode !== 200) {
                    console.log(response.value)
                    showStartGameWarningText(response.value)
                }
            })
            .catch(e => console.log(e))
    }

    function lobbyKeyInputOnKeyDown(e: React.KeyboardEvent<HTMLInputElement>) {
        if (e.key === 'Enter') joinLobby()
    }



    if (loading) return <Loading />
    if (error) return <h1>{error}</h1>



    // Created lobby room
    if (lobbyKey && lobbyPlayers.length > 0) return <div className="checkersLobbyWrapper open">
        <h1>Лобби (Шашки)</h1>
        <hr />

        <div className="lobbyKeyWrapper">
            <h2>Код комнаты</h2>
            <h1 className="lobbyKey">{lobbyKey}</h1>
        </div>

        <div className="playerListButtonsWrapper">
            <fieldset className="playersList">
                <legend>Список игроков</legend>
                <hr />
                {lobbyPlayers}
            </fieldset>

            <div className="btnWrapper">
                <p ref={startGameWarningMessage}>Тут будут ошибки</p>
                {isHost && <button className="startGameBtn" onClick={startGame}>Начать игру</button>}
                <button className="exitBtn" onClick={leaveLobby}>{isHost ? 'Закрыть комнату' : 'Покинуть комнату'}<span className="icon"></span></button>
            </div>
        </div>
    </div>



    // Create or Enter buttons
    return <div className='checkersLobbyWrapper'>
        <button onClick={createLobby}>Создать игру</button>
        <button onClick={showJoinDialog}>Подключиться к другому игроку</button>

        <dialog ref={joinLobbyDialog}>
            <p>Введите код комнаты</p>
            <input ref={lobbyKeyInput} onKeyDown={lobbyKeyInputOnKeyDown} type="text" maxLength={4} autoFocus />
            <p id="lobbyKeyWarningMessage" ref={lobbyKeyWarningMessage}></p>
            <button className="enterLobbyBtn" onClick={joinLobby}>Войти</button>
            <button className="exitBtn" onClick={hideJoinDialog}>Отмена</button>
        </dialog>
    </div>
}



async function getUserInfoByID(userID: number) {
    try {
        const response = await axios.get(ENDPOINTS.Users.GET_USER_INFO_BY_ID_URL + userID)
        return response.data as PLayerInfo

    } catch (error) {
        console.log('Can not get user info!')

        return {
            id: userID,
            name: 'unknown',
        }
    }
}