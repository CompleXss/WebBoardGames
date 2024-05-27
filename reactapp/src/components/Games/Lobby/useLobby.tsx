import axios from "axios"
import { useEffect, useRef, useState } from "react"
import { useNavigate } from "react-router-dom"
import { HubConnection } from "@microsoft/signalr"
import { useWebsocketConnection } from "src/utilities/useWebsocketHook"
import { User as PlayerInfo } from "src/utilities/Api_DataTypes"
import { getImageUrl } from "src/utilities/frontend.utils"
import ENDPOINTS from "src/utilities/Api_Endpoints"
import Loading from "src/components/Loading/loading"
import './lobby.css'

// TODO: при обновлении страницы лобби закрывается
// TODO: lobby key click to copy

const CREATE_LOBBY = 'CreateLobby'
const ENTER_LOBBY = 'EnterLobby'
const LEAVE_LOBBY = 'LeaveLobby'
const CLOSE_LOBBY = 'CloseLobby'
const KEY_LENGTH = 4

interface LobbyInfo {
    hostID: string
    key: string
    playerIDs: string[]
}

export function useLobby(gameName: string, title: string, publicBackgroundPath: string) {
    const navigate = useNavigate()

    const [lobbyInfo, setLobbyInfo] = useState<LobbyInfo | undefined>()
    const [isHost, setIsHost] = useState(false)
    const [lobbyPlayerInfos, setLobbyPlayerInfos] = useState<PlayerInfo[]>([])

    const joinLobbyDialog = useRef<HTMLDialogElement>(null)
    const lobbyKeyInput = useRef<HTMLInputElement>(null)
    const lobbyKeyWarningMessage = useRef<HTMLParagraphElement>(null)
    const startGameWarningMessage = useRef<HTMLParagraphElement>(null)

    // create connection
    const { connection, loading, setLoading, error } = useWebsocketConnection(ENDPOINTS.Hubs.LOBBY + gameName, {
        whenCreatingConnection: clearLobbyInfo,
        whenConnectionCreated: addEventHandlers,
        debugInConsole: true,
    })

    // server side event handlers
    function addEventHandlers(connection: HubConnection) {
        connection.on('UserConnected', async userID => {
            const userInfo = await getUserInfoByID(userID)

            setLobbyPlayerInfos(infos => {
                return [...infos, userInfo!]
            })
        })

        connection.on('UserDisconnected', userID => {
            setLobbyPlayerInfos(infos => {
                return infos.filter(x => x.publicID !== userID)
            })
        })

        connection.on('LobbyClosed', () => {
            clearLobbyInfo()
            alert('Комната была закрыта!')
        })

        connection.on('HostChanged', async hostID => {
            if (!hostID) return

            setLobbyInfo(info => {
                if (!info) return info
                info.hostID = hostID
                return { ...info }
            })
        })

        connection.on('GameStarted', () => {
            navigate('/play/' + gameName)
        })

        connection.onreconnecting(() => setLoading(true))
        connection.onreconnected(() => setLoading(false))
        connection.onclose(() => clearLobbyInfo())
    }

    useEffect(() => {
        if (!lobbyInfo) return

        getMyUserInfo()
            .then(myInfo => {
                setIsHost(myInfo.publicID === lobbyInfo.hostID)
            })
            .catch(_ => { })
    }, [lobbyInfo])



    // lobby info functions
    async function trySetLobbyInfo(lobby: LobbyInfo): Promise<boolean> {
        if (!lobby) {
            return false
        }
        setLobbyInfo(lobby)

        const playerInfos = await Promise.all(lobby.playerIDs.map(async id => {
            return await getUserInfoByID(id)
        }))

        setLobbyPlayerInfos(playerInfos)

        return true
    }

    function clearLobbyInfo() {
        setLobbyInfo(undefined)
        setIsHost(false)
        setLobbyPlayerInfos([])
    }



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
                    console.error('Could not get lobbyinfo!')
                }
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
        if (!lobbyKey || lobbyKey.length !== KEY_LENGTH) {
            showLobbyKeyWarningText(`Введите код из ${KEY_LENGTH} цифр`)
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

    function closeLobby() {
        if (!connection) return

        connection.invoke(CLOSE_LOBBY)
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
            .catch(_ => { })
    }

    function lobbyKeyInputOnKeyDown(e: React.KeyboardEvent<HTMLInputElement>) {
        if (e.key === 'Enter') joinLobby()
    }

    const lobbyPlayers = lobbyPlayerInfos.map((player, index) => {
        let name = player.name
        if (lobbyInfo?.hostID && lobbyInfo.hostID === player.publicID) {
            name += ' 👑'
        }
        return <p key={index}>{name}</p>
    })



    if (loading) return { element: <Loading /> }
    if (error) return { element: <h1>{error}</h1> }

    // Created lobby room
    if (lobbyInfo?.key && lobbyPlayers.length > 0) return {
        element: (
            <div className="lobbyWrapper open">
                <h1>{title}</h1>
                <hr />

                <div className="lobbyKeyWrapper">
                    <h2>Код комнаты</h2>
                    <h1 className="lobbyKey">{lobbyInfo.key}</h1>
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
                        <button className="exitBtn" onClick={isHost ? closeLobby : leaveLobby}>
                            {isHost ? 'Закрыть комнату' : 'Покинуть комнату'}<span className="icon"></span>
                        </button>
                    </div>
                </div>
            </div>
        )
    }

    // Create or Enter buttons
    return {
        element: (
            <div className='lobbyWrapper' style={{ backgroundImage: getImageUrl(publicBackgroundPath) }}>
                <button onClick={createLobby}>Создать игру</button>
                <button onClick={showJoinDialog}>Подключиться к другому игроку</button>

                <dialog ref={joinLobbyDialog}>
                    <p>Введите код комнаты</p>
                    <input ref={lobbyKeyInput} onKeyDown={lobbyKeyInputOnKeyDown} type="text" maxLength={KEY_LENGTH} autoFocus placeholder={'x'.repeat(KEY_LENGTH)} />
                    <p id="lobbyKeyWarningMessage" ref={lobbyKeyWarningMessage}></p>
                    <button className="enterLobbyBtn" onClick={joinLobby}>Войти</button>
                    <button className="exitBtn" onClick={hideJoinDialog}>Отмена</button>
                </dialog>
            </div>
        )
    }
}



async function getUserInfoByID(userID: string): Promise<PlayerInfo> {
    try {
        const response = await axios.get(ENDPOINTS.Users.GET_USER_INFO_BY_ID_URL + userID)
        return response.data as PlayerInfo

    } catch (error) {
        console.log('Could not get user info!')

        return {
            publicID: userID,
            name: 'unknown',
        }
    }
}

async function getMyUserInfo(): Promise<PlayerInfo> {
    try {
        const response = await axios.get(ENDPOINTS.Users.GET_USER_INFO_URL)
        return response.data as PlayerInfo
    } catch (error) {
        console.log('Could not get user info!')

        return {
            publicID: '',
            name: 'unknown',
        }
    }
}
