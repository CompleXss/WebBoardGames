import { useEffect, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { HubConnection } from '@microsoft/signalr';
import { useWebsocketConnection } from '../../../utilities/useWebsocketHook';
import { useWinnerDialog } from '../WinnerDialog/winnerDialog';
import { GameNames } from 'src/utilities/GameNames';
import { sleep } from 'src/utilities/utils';
import ENDPOINTS from '../../../utilities/Api_Endpoints';
import Loading from "../../Loading/loading";
import './checkersGame.css'

interface GameData {
    myColor: 'white' | 'black'
    allyPositions: DraughtInfo[]
    enemyPositions: DraughtInfo[]
    isMyTurn: boolean
    isEnemyConnected: boolean
    ongoingMoveFrom?: { x: number, y: number }
    lastMove: Move
}

interface DraughtInfo {
    x: number
    y: number
    isQueen: boolean
}

interface Move {
    from: { x: number, y: number }
    to: { x: number, y: number }
}



const animationDuration = '0.25s'
const gameName = GameNames.checkers

export default function CheckersGame() {
    const navigate = useNavigate()
    const [gameData, setGameData] = useState<GameData | undefined>()
    const [fromCell, setFromCell] = useState<{ x: number, y: number }>()
    const [showAnimation, setShowAnimation] = useState<boolean>(true)
    const whosTurn = useRef<HTMLHeadingElement>(null)
    const enemyIsOffline = useRef<HTMLHeadingElement>(null)
    const surrenderDialog = useRef<HTMLDialogElement>(null)
    const { showWinner, element: winnerDialog, } = useWinnerDialog()

    useEffect(() => {
        document.title = 'Шашки'
    }, [])

    // create connection
    const [reloading, setReloading] = useState(false)
    const { connection, loading, error }
        = useWebsocketConnection(ENDPOINTS.Hubs.GAME + gameName, {
            whenConnectionCreated: addEventHandlers,
            whenConnected: getBoardState,
            debugInConsole: true,
        })

    function addEventHandlers(connection: HubConnection) {
        connection.on('NotAllowed', () => {
            navigate('/')
        })

        connection.on('GameStateChanged', getBoardState)
        connection.on('UserDisconnected', () => setEnemyIsConnected(false))
        connection.on('UserReconnected', () => setEnemyIsConnected(true))

        connection.onreconnecting(() => setReloading(true))
        connection.onreconnected(() => getBoardState())
        connection.onclose(() => setReloading(true))

        // decide the winner
        connection.on('GameClosed', winnerID => {
            showWinner(winnerID)
        })
    }

    function getBoardState() {
        if (!connection) return

        connection.invoke('GetGameState')
            .then(response => {

                if (response.value) {
                    setGameData(response.value)
                    setReloading(false)
                }
                else setGameData(undefined)

                setFromCell(cell => {
                    setFromCellColor(cell, 'transparent')
                    return undefined;
                })
            })
            .catch(e => console.log(e))
    }

    useEffect(() => {
        if (!gameData) return

        if (whosTurn.current) {
            if (gameData.isMyTurn) {
                whosTurn.current.textContent = 'Ваш ход'
                whosTurn.current.style.backgroundColor = '#005500'
            }
            else {
                whosTurn.current.textContent = 'Ход противника'
                whosTurn.current.style.backgroundColor = '#3a3a3a'
            }
        }

        setEnemyIsConnected(gameData.isEnemyConnected)

        if (gameData.ongoingMoveFrom) {
            draughtClick(gameData.ongoingMoveFrom.x, gameData.ongoingMoveFrom.y)
        }

        if (gameData.lastMove) {
            setShowAnimation(true)
        }
    }, [gameData])



    function setEnemyIsConnected(state: boolean) {
        if (!enemyIsOffline.current) return

        if (state) {
            enemyIsOffline.current.textContent = 'Противник онлайн'
            enemyIsOffline.current.style.color = 'whitesmoke'
        }
        else {
            enemyIsOffline.current.textContent = 'Противник отключён'
            enemyIsOffline.current.style.color = '#cc0000' // red
        }
    }

    function spawnCells() {
        const cells: JSX.Element[] = new Array(64)

        for (let y = 0; y < 8; y++) {
            for (let x = 0; x < 8; x++) {
                const className = (x & 1) === (y & 1)
                    ? 'cell white'
                    : 'cell black'

                const id = `cell ${x}${7 - y}`;
                cells[y * 8 + x] =
                    <button
                        id={id}
                        key={id}
                        className={className}
                        onClick={() => cellClick(x, 7 - y)}
                    >
                        <div className='inner'></div>
                    </button>
            }
        }
        return cells
    }

    function spawnDraughts(data: GameData) {
        if (!data.allyPositions || !data.enemyPositions || !data.myColor) return []

        const myColor = data.myColor
        const enemyColor = data.myColor === 'black' ? 'white' : 'black'

        const cells: JSX.Element[] = new Array(data.allyPositions.length + data.enemyPositions.length)

        let index = 0
        index = addDraughtsToArr(cells, data.allyPositions, myColor, index)
        addDraughtsToArr(cells, data.enemyPositions, enemyColor, index)

        if (gameData?.lastMove && showAnimation) {
            window.requestAnimationFrame(() => {
                moveDraught(gameData.lastMove.from, gameData.lastMove.to)
                setShowAnimation(false)
            })
        }

        return cells
    }

    function addDraughtsToArr(cells: JSX.Element[], draughts: DraughtInfo[], color: string, index: number): number {
        for (let i = 0; i < draughts.length; i++, index++) {
            const dr = draughts[i]
            const id = `unit ${dr.x}${dr.y}`
            const isQueen = dr.isQueen ? ' queen' : ''

            cells[index] =
                <button
                    id={id}
                    key={id}
                    className={'unit ' + color + isQueen}
                    onClick={() => draughtClick(dr.x, dr.y)}
                    style={{
                        transform: `
                        translateX(${dr.x * 100}%)
                        translateY(${(7 - dr.y) * 100}%)
                        `
                    }}>
                </button>
        }
        return index
    }

    function setFromCellColor(fromCell: { x: number, y: number } | undefined, color: string) {
        if (!fromCell) return

        const cell = document.getElementById(`cell ${fromCell.x}${fromCell.y}`)?.getElementsByClassName('inner')[0] as HTMLElement | undefined
        if (!cell) return

        cell.style.backgroundColor = color
    }

    function cellClick(x: number, y: number) {
        if (!gameData?.isMyTurn || !fromCell) return

        makeMove({ from: fromCell, to: { x, y } })

        setFromCell(cell => {
            setFromCellColor(cell, 'transparent')
            return undefined;
        })
    }

    function draughtClick(x: number, y: number) {
        if (!gameData?.isMyTurn) return

        const unit = document.getElementById(`unit ${x}${y}`) as HTMLElement
        if (!unit?.classList.contains(gameData.myColor)) return

        setFromCell(cell => {
            setFromCellColor(cell, 'transparent')
            setFromCellColor({ x, y }, '#00cc00') // green
            return { x, y }
        })
    }

    function makeMove(move: Move) {
        connection?.invoke('MakeMove', move)
            .then(response => {
                if (response?.statusCode !== 200) {
                    console.log(response.value)
                }
            })
            .catch(e => console.log(e))
    }

    function showSurrenderDialog() {
        surrenderDialog.current?.showModal()
    }

    function surrender() {
        connection?.invoke('Surrender')
            .then(x => {
                if (x.statusCOde && x.statusCode !== 200) {
                    console.log(x.value)
                }
            })
            .catch(_ => { })
    }



    if (loading || reloading) return <Loading />
    if (error) return error

    return <div className="checkersContainer">
        {winnerDialog}

        <div className='enemyZone'>
            <h1 ref={enemyIsOffline}>Противник онлайн</h1>
        </div>

        <h1 className='whosTurn' ref={whosTurn}>?</h1>

        <div className='boardContainer'>
            <div className="board">
                {spawnCells()}
                {gameData && spawnDraughts(gameData)}
            </div>
        </div>

        <div className='myZone'>
            <button className='surrenderBtn' onClick={showSurrenderDialog}>Сдаться 💀</button>
        </div>

        <dialog ref={surrenderDialog} className='surrenderDialog'>
            <h2>Вы уверены, что хотите сдаться?</h2>
            <div className='buttons'>
                <button onClick={() => {
                    surrender()
                    surrenderDialog.current?.close()
                }}>Да</button>
                <button onClick={() => {
                    surrenderDialog.current?.close()
                }}>Нет</button>
            </div>
        </dialog>
    </div>
}



async function moveDraught(from: { x: number, y: number }, to: { x: number, y: number }) {
    if (from.x === to.x && from.y === to.y) return

    const unit = document.getElementById(`unit ${to.x}${to.y}`)
    if (!unit) return

    unit.style.transitionDuration = '0s'
    unit.style.transform = `
        translateX(${from.x * 100}%)
        translateY(${(7 - from.y) * 100}%)
    `
    await sleep(1)

    unit.style.transitionDuration = animationDuration
    unit.style.transform = `
        translateX(${to.x * 100}%)
        translateY(${(7 - to.y) * 100}%)
    `
    await sleep(1)
}
