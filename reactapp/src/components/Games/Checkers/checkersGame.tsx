import { useEffect, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { HubConnection } from '@microsoft/signalr';
import { useWebsocketConnection } from '../../../utilities/useWebsocketHook';
import { GameNames } from 'src/utilities/GameNames';
import ENDPOINTS from '../../../utilities/Api_Endpoints';
import Loading from "../../Loading/loading";
import { useWinnerDialog } from '../WinnerDialog/winnerDialog';
import './checkersGame.css'

interface DraughtInfo {
    x: number
    y: number
    isQueen: boolean
}

interface GameData {
    myColor: 'white' | 'black'
    allyPositions: DraughtInfo[]
    enemyPositions: DraughtInfo[]
    isMyTurn: boolean
    //lastMove?: { from: Point, to: Point }[]
}



const gameName = GameNames.checkers

export default function CheckersGame() {
    const navigate = useNavigate()
    const [gameData, setGameData] = useState<GameData | undefined>()
    const [fromCell, setFromCell] = useState<{ x: number, y: number }>()
    const enemyIsOffline = useRef<HTMLHeadingElement>(null)
    const whosTurn = useRef<HTMLHeadingElement>(null)
    const { showWinner, element: winnerDialog, } = useWinnerDialog()
    const surrenderDialog = useRef<HTMLDialogElement>(null)

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

        connection.on('UserDisconnected', userID => {
            if (!enemyIsOffline.current) return

            enemyIsOffline.current.textContent = 'Противник отключён'
            enemyIsOffline.current.style.color = '#cc0000' // red
        })

        connection.on('UserReconnected', userID => {
            if (!enemyIsOffline.current) return

            enemyIsOffline.current.textContent = 'Противник онлайн'
            enemyIsOffline.current.style.color = 'whitesmoke'
        })

        connection.onreconnecting(() => setReloading(true))
        connection.onreconnected(() => {
            getBoardState()
        })
        connection.onclose(() => {
            navigate('/')
        })

        // Определение победителя
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

    // ВАШ ХОД -- ХОД ПРОТИВНИКА
    useEffect(() => {
        if (!whosTurn.current) return
        if (!gameData) return

        if (gameData.isMyTurn) {
            whosTurn.current.textContent = 'Ваш ход'
            whosTurn.current.style.backgroundColor = '#005500'
        }
        else {
            whosTurn.current.textContent = 'Ход противника'
            whosTurn.current.style.backgroundColor = '#3a3a3a'
        }

    }, [gameData])



    function spawnCells() {
        const cells: JSX.Element[] = new Array(64)

        for (let y = 0; y < 8; y++) {
            for (let x = 0; x < 8; x++) {
                let className = (x & 1) === (y & 1)
                    ? 'cell white'
                    : 'cell black'

                let id = `cell ${x}${7 - y}`;
                cells[y * 8 + x] =
                    <button
                        id={id}
                        key={id}
                        className={className}
                        onClick={() => cellClick(x, 7 - y)}>
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

        return cells
    }

    function addDraughtsToArr(cells: JSX.Element[], draughts: DraughtInfo[], color: string, index: number): number {
        for (let i = 0; i < draughts.length; i++, index++) {
            let dr = draughts[i]
            let id = `unit ${dr.x}${dr.y}`
            let isQueen = dr.isQueen ? ' queen' : ''

            cells[index] =
                <button
                    id={id}
                    key={id}
                    className={'unit ' + color + isQueen}
                    onClick={() => draughtClick(dr.x, dr.y)}
                    style={{
                        transform: `
                    translateX(calc(${dr.x} * 100%))
                    translateY(calc(${7 - dr.y} * 100%))`
                    }}>
                </button>
        }
        return index
    }

    function setFromCellColor(fromCell: { x: number, y: number } | undefined, color: string) {
        if (!fromCell) return

        const cell = document.getElementById(`unit ${fromCell.x}${fromCell.y}`) as HTMLElement
        if (!cell) return

        cell.style.backgroundColor = color
    }

    function cellClick(x: number, y: number) {
        if (!gameData?.isMyTurn || !fromCell) return

        makeMove([{ from: fromCell, to: { x, y } }])

        setFromCell(cell => {
            setFromCellColor(cell, 'transparent')
            return undefined;
        })
    }

    function draughtClick(x: number, y: number) {
        if (!gameData?.isMyTurn) return

        const cell = document.getElementById(`unit ${x}${y}`) as HTMLElement
        if (!cell?.classList.contains(gameData.myColor)) return

        setFromCell(cell => {
            setFromCellColor(cell, 'transparent')
            setFromCellColor({ x, y }, '#00cc00')
            return { x, y };
        })
    }

    function makeMove(moves: { from: { x: number, y: number }, to: { x: number, y: number } }[]) {
        connection?.invoke('MakeMove', moves)
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



// function moveDraught(from: Draught, to: Draught) {
//     if (from.x === to.x && from.y === to.y) return

//     const unit = document.getElementById(`unit ${from.x}${from.y}`)
//     if (!unit) return

//     unit.style.transform = `
//         translateX(calc(${to.x} * 100%))
//         translateY(calc(${to.y} * 100%))`
// }
