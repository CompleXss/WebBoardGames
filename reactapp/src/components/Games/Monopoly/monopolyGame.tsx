import axios from 'axios';
import React, { useEffect, useRef, useState } from 'react';
import { useNavigate } from "react-router-dom";
import { HubConnection } from '@microsoft/signalr';
import { useWebsocketConnection } from 'src/utilities/useWebsocketHook';
import ENDPOINTS from 'src/utilities/Api_Endpoints';
import Loading from "src/components/Loading/loading"
import LoadingContent from 'src/components/LoadingContent/loadingContent';
import monopolyMap from './monopoly_map.json'
import cardsInfo from './monopoly_cards.json'
import { GameNames } from 'src/utilities/GameNames';
import { StringMap, numberWithCommas, sleep } from 'src/utilities/utils';
import { PlayerInfo } from '../Models';
import { ReactComponent as DiceIcon } from 'src/svg/dice.svg'
import { ReactComponent as StarIcon } from 'src/svg/star.svg'
import './monopolyGame.css'
import { DiceCube } from './DiceCube/diceCube';

interface GameState {
    myID: string
    players: StringMap<PlayerState>
    cellStates: StringMap<CellState>
}

interface PlayerState {
    color: string
    isOnline: boolean
    isDead: boolean
    money: number
    position: string
}

interface CellState {
    ownerID?: string
    cost: number
    upgradeLevel: number
    isSold: boolean
    movesLeftToLooseThisCell: number
    type: string
    info: {
        rent?: number[]
        buyCost: number
        sellCost: number,
        rebuyCost: number
        upgradeCost?: number
    }
    multipliers?: number[]
}

enum ActionType {
    Yes,
    No,
    Pay,
    DiceToMove,
    DiceToExitPrison,
    BuyCell,
    UpgradeCell,
    DowngradeCell,
    CreateContract,
}



// todo: get setup info instead of map json

const DICE_TIME = 1000
const AFTER_DICE_WAIT_TIME = 500
const MOVE_TIME = 1000

const globalData = {
    msToWait: 0,
}

const gameName = GameNames.monopoly

export default function MonopolyGame() {
    const navigate = useNavigate()
    const [playerInfos, setPlayerInfos] = useState<Map<string, PlayerInfo>>(new Map())
    const [gameState, setGameState] = useState<GameState | undefined>()
    const [cardGroupDescription, setCardGroupDescription] = useState<string>('')
    const [groupInfoParams, setGroupInfoParams] = useState<JSX.Element[]>([])
    const [cardInfoParams, setCardInfoParams] = useState<JSX.Element[]>([])
    const [gridTemplateAreas, setGridTemplateAreas] = useState<string>()
    const clickDialog = useRef<HTMLDialogElement>(null)
    const cardInfoDialog = useRef<HTMLDialogElement>(null)
    const clickDialogYesButton = useRef<HTMLButtonElement>(null)
    const clickDialogNoButton = useRef<HTMLButtonElement>(null)
    const clickDialogText = useRef<HTMLHeadingElement>(null)
    const clickDialogSubText = useRef<HTMLParagraphElement>(null)
    const diceCube1 = useRef<HTMLDivElement>(null)
    const diceCube2 = useRef<HTMLDivElement>(null)
    const playerDots = useRef<HTMLDivElement>(null)
    const [playerDotPositions, setPlayerDotPositions] = useState<Map<string, { x: number, y: number }>>()

    useEffect(() => {
        document.title = 'Монополия'

        if (diceCube1.current) diceCube1.current.hidden = true
        if (diceCube2.current) diceCube2.current.hidden = true
    }, [])

    useEffect(() => {
        const firstLine = getFirstLine(monopolyMap.cardsInLine)
        const midLines = getMidLines(monopolyMap.cardsInLine)
        const lastLine = getLastLine(monopolyMap.cardsInLine)

        setGridTemplateAreas(
            `
            ${firstLine}
            ${firstLine}
            ${midLines}
            ${lastLine}
            ${lastLine}
            `
        )
    }, [])

    useEffect(() => {
        if (connection) {
            // react captures state values in eventHandler so update we'll them every time
            // bruh
            addEventHandlers(connection)
        }
        requestLastOffer()

        if (!gameState?.players) return

        const playerIDs = Object.keys(gameState.players)
        for (const playerID of playerIDs) {
            const info = gameState.players[playerID]
            // todo update player state???

            movePlayerDot_direct(playerID, info.position)
        }
    }, [gameState])



    // create connection
    const [reloading, setReloading] = useState(false)
    const { connection, loading, error }
        = useWebsocketConnection(ENDPOINTS.Hubs.GAME + gameName, {
            whenConnectionCreated: addEventHandlers,
            whenConnected: getGameState,
            debugInConsole: true,
        })

    function addEventHandlers(connection: HubConnection) {
        connectionOnExclusive(connection, 'NotAllowed', () => {
            navigate('/')
        })

        connectionOnExclusive(connection, 'GameStateChanged', getGameState)

        connectionOnExclusive(connection, 'UserDisconnected', userID => {
            setGameState(state => {
                if (state) {
                    const player = state.players[userID]
                    if (player)
                        player.isOnline = false
                }
                return state
            })
        })

        connectionOnExclusive(connection, 'UserReconnected', async userID => {
            setGameState(state => {
                if (state) {
                    const player = state.players[userID]
                    if (player)
                        player.isOnline = true
                }
                return state
            })

            if (!playerInfos.get(userID)) {
                const playerInfo = await getUserInfoByID(userID)
                setPlayerInfos(players => {
                    players.set(userID, playerInfo)
                    return players
                })
            }
        });

        // remove all callbacks from following methods
        (connection as any)._reconnectingCallbacks = [] as any;
        (connection as any)._reconnectedCallbacks = [] as any;
        (connection as any)._closedCallbacks = [] as any;

        connection.onreconnecting(() => setReloading(true))
        connection.onreconnected(getGameState)
        connection.onclose(() => {
            //navigate('/') // todo uncomment
        })

        connectionOnExclusive(connection, 'GameClosed', winnerID => {
            if (!winnerID) {
                navigate('/')
                return
            }

            // todo gameClosed
        })



        // actions
        connectionOnExclusive(connection, 'ShowDiceRoll', async data => {
            rollDice(diceCube1, data.dice1)
            rollDice(diceCube2, data.dice2)
        })



        // offers
        connectionOnExclusive(connection, 'OfferDiceRoll', () => {
            showClickDialog(
                'Ваш ход!',
                'Время двигаться вперёд.',
                'Бросить кубики',
                () => makeMove(connection, ActionType.DiceToMove)
            )
        })

        connectionOnExclusive(connection, 'OfferPay', ({ amount, reason }) => {
            const { title, description } = getTextForPayReason(reason)
            const enoughMoneyToPay = isEnoughMoneyToPay(amount)

            showClickDialog(
                title,
                description,
                `Заплатить ${numberWithCommas(amount)}k`,
                enoughMoneyToPay ? () => makeMove(connection, ActionType.Pay) : false
            )
        })

        connectionOnExclusive(connection, 'OfferPayToPlayer', ({ payToPlayerIndex, amount }) => {
            const enoughMoneyToPay = isEnoughMoneyToPay(amount)

            showClickDialog(
                'Заплатите аренду.',
                `Вы попали на чужое поле. Нужно заплатить владельцу ${numberWithCommas(amount)}k`,
                `Заплатить ${numberWithCommas(amount)}k`,
                enoughMoneyToPay ? () => makeMove(connection, ActionType.Pay) : false
            )
        })

        connectionOnExclusive(connection, 'OfferExitPrison', ({ amount, triesLeft }) => {
            if (!gameState) return
            const enoughMoneyToPay = isEnoughMoneyToPay(amount)

            showClickDialog(
                'Вы попали в тюрьму.',
                `Для выхода нужно выбросить дубль (осталось попыток: ${triesLeft}) или заплатить ${numberWithCommas(amount)}k.`,
                `Заплатить ${numberWithCommas(amount)}k`,
                enoughMoneyToPay ? () => makeMove(connection, ActionType.Pay) : false,
                'Бросить кубики',
                () => makeMove(connection, ActionType.DiceToExitPrison)
            )
        })

        // connection.on('OfferBuyCell', offerBuyCell)

        connectionOnExclusive(connection, 'OfferBuyCell', ({ cellID }) => {
            console.log('OfferBuyCell')
            console.log(gameState)
            if (!gameState) return

            const cellCost = gameState.cellStates[cellID].cost
            const enoughMoneyToPay = isEnoughMoneyToPay(cellCost)

            showClickDialog(
                'Покупаем?',
                'Если вы откажетесь от покупки, то поле останется незанятым.',
                `Купить за ${numberWithCommas(cellCost)}k`,
                enoughMoneyToPay ? () => makeMove(connection, ActionType.BuyCell) : false,
                'Отказаться',
                () => makeMove(connection, ActionType.No)
            )
        })



        function isEnoughMoneyToPay(payAmount: number): boolean {
            if (!gameState) return false
    
            // todo
    
            return payAmount <= gameState.players[gameState.myID].money
        }
    }

    function connectionOnExclusive(connection: HubConnection, methodName: string, newMethod: (...args: any[]) => any) {
        connection.off(methodName)
        connection.on(methodName, newMethod)
    }



    function makeMove(connection: HubConnection, actionType?: ActionType, cellID?: number, number?: number) {
        connection.invoke('MakeMove', {
            actionType,
            cellID,
            number,
        })
            .then(x => {
                if (x.statusCode && x.statusCode !== 200) {
                    console.log(x.value)
                    requestLastOffer()
                }
            })
            .catch(x => console.log(x))
    }



    function getGameState() {
        if (!connection) return

        setPlayerDotPositions(new Map<string, { x: number, y: number }>())

        connection.invoke('GetGameState')
            .then(response => {

                // todo remove logs
                console.log('==================')
                console.log(response.value)

                if (response.value) {
                    setGameState(response.value)
                    setReloading(false)

                    // requestLastOffer()
                }
                else setGameState(undefined)
            })
            .catch(e => { })
    }

    // todo
    function getTextForPayReason(reason: string): { title: string, description: string } {
        switch (reason) {
            case 'ExitPrison':
                return {
                    title: 'Заплатите за освобождение.',
                    description: 'У вас кончились попытки на выбрасывание дубля. Теперь придётся заплатить, чтобый выйти из тюрьмы'
                }

            case '':
                return {
                    title: '',
                    description: ''
                }

            default:
                return {
                    title: '',
                    description: ''
                }
        }
    }

    function requestLastOffer() {
        if (!connection) return

        connection.invoke('Request', 'RepeatLastOffer')
            .catch(e => { })
    }



    function getCornerCardsElements() {
        if (monopolyMap.cornerCards.length !== 4) {
            console.error('Invalid monopoly map json. Amount of corner cards is not exactly 4.')
        }

        const count = Math.min(monopolyMap.cornerCards.length, 4)
        const arr = new Array<JSX.Element>(count)

        for (let i = 0; i < count; i++) {
            const card = monopolyMap.cornerCards[i]
            let icon = cardsInfo.cornerCards.find(x => x.id === card.id)?.icon ?? ''
            icon = getImageUrl(cardsInfo.iconFolder + icon)

            const key = card.id

            arr[i] = (
                <div id={key} key={key} className='cell' mnpl-corner={i} style={{ gridArea: `c${i}` }}>
                    <div className='cell-icon' style={{ backgroundImage: icon }}></div>
                </div>
            )
        }
        return arr
    }

    function getLineCardsElements() {
        if (!gameState) return

        if (monopolyMap.layout.length !== (4 * monopolyMap.cardsInLine)) {
            console.error('Invalid monopoly map json. Amount of line cards don\' match.')
        }

        const cardsInLine = monopolyMap.cardsInLine
        const count = Math.min(monopolyMap.layout.length, 4 * monopolyMap.cardsInLine)
        const arr = new Array<JSX.Element>(count)
        const cardsInGroupCount: StringMap<number> = {}

        for (let i = 0; i < count; i++) {
            const group_id = monopolyMap.layout[i]
            const cardNum = cardsInGroupCount[group_id] ?? 0
            const card_id = `${group_id}_${cardNum}`

            const isEvent = group_id.startsWith('event_')
            const needsRotate = group_id.includes('random')
            let icon = ''
            let color = ''
            let cardName = ''
            let groupName = ''

            const cardInfo = gameState.cellStates[card_id]
            if (!isEvent && !cardInfo) console.error('did not get info about cell: ' + card_id)

            if (isEvent) {
                icon = cardsInfo.eventCards.find(x => x.id === group_id)?.icon ?? ''
            }
            else {
                const groupInfo = cardsInfo.cardGroups.find(x => x.id === group_id)
                if (groupInfo) {
                    const cardInfo = groupInfo.cards[cardNum]

                    icon = cardInfo.icon ?? ''
                    cardName = cardInfo.name
                    groupName = groupInfo.name

                    color = groupInfo.color ?? ''
                    if (color === '') {
                        color = 'dimgray'
                    }
                }
            }
            icon = getImageUrl(cardsInfo.iconFolder + icon)

            const line = Math.floor(i / cardsInLine)
            const mnpl_special = isEvent ? 1 : null
            const mnpl_rotate = needsRotate ? 1 : null
            const backgroundColor = cardInfo?.ownerID ? gameState.players[cardInfo.ownerID].color : undefined

            arr[i] = (
                <div
                    id={card_id}
                    key={card_id}
                    className='cell'
                    mnpl-line={line}
                    mnpl-special={mnpl_special}
                    mnpl-rotate={mnpl_rotate}
                    style={{ gridArea: `l${i}`, backgroundColor: backgroundColor }}
                    onClick={isEvent || !cardInfo ? undefined : () => {
                        if (cardInfoDialog.current) {
                            const header = cardInfoDialog.current.getElementsByClassName('cardInfoHeader')[0] as HTMLElement
                            if (header) {
                                header.style.backgroundColor = color

                                const cName = header.getElementsByTagName('h1')[0]
                                const gName = header.getElementsByTagName('h2')[0]

                                if (cName) cName.textContent = cardName
                                if (gName) gName.textContent = groupName
                            }

                            const body = cardInfoDialog.current.getElementsByClassName('cardInfoBody')[0] as HTMLElement
                            if (body) {

                                // description
                                setCardGroupDescription(
                                    cardsInfo.groupTypes
                                        .find(x => x.type === cardInfo.type)
                                        ?.description.replace('{groupName}', groupName) ?? ''
                                )

                                // group params
                                const groupParams: JSX.Element[] = []
                                const cardParams: JSX.Element[] = []

                                switch (cardInfo.type) {
                                    case 'upgrade':
                                        const rent = cardInfo.info.rent!

                                        groupParams.push(createParamsLineElement(
                                            cardsInfo.translation.rent_0,
                                            numberWithCommas(rent[0]),
                                            'money'
                                        ))

                                        for (let i = 1; i < rent.length; i++) {
                                            const stars = Array(i).fill(<StarIcon className='starIcon' />)
                                            groupParams.push(createParamsLineElement(
                                                <div>{stars}</div>,
                                                numberWithCommas(rent[i]),
                                                'money'
                                            ))
                                        }

                                        break;

                                    case 'count':
                                        if (!cardInfo.multipliers) return

                                        for (let i = 0; i < cardInfo.multipliers.length; i++) {
                                            groupParams.push(createParamsLineElement(
                                                getNumberedFieldName(i + 1),
                                                cardInfo.multipliers[i].toString(),
                                                'money'
                                            ))
                                        }

                                        break;

                                    case 'dice':
                                        if (!cardInfo.multipliers) return

                                        for (let i = 0; i < cardInfo.multipliers.length; i++) {
                                            groupParams.push(createParamsLineElement(
                                                getNumberedFieldName(i + 1),
                                                <div><DiceIcon className='diceIcon' /> {' x ' + cardInfo.multipliers[i]}</div>,
                                                'diceMultiplier'
                                            ))
                                        }

                                        break;

                                    default:
                                        break;
                                }

                                // card params
                                cardParams.push(createParamsLineElement(
                                    cardsInfo.translation.buyCost,
                                    numberWithCommas(cardInfo.info.buyCost),
                                    'money'
                                ))
                                cardParams.push(createParamsLineElement(
                                    cardsInfo.translation.sellCost,
                                    numberWithCommas(cardInfo.info.sellCost),
                                    'money'
                                ))
                                cardParams.push(createParamsLineElement(
                                    cardsInfo.translation.rebuyCost,
                                    numberWithCommas(cardInfo.info.rebuyCost),
                                    'money'
                                ))
                                if (cardInfo.info.upgradeCost) cardParams.push(createParamsLineElement(
                                    cardsInfo.translation.upgradeCost,
                                    numberWithCommas(cardInfo.info.upgradeCost),
                                    'money'
                                ))

                                // set params
                                setGroupInfoParams(groupParams)
                                setCardInfoParams(cardParams)
                            }
                        }
                        cardInfoDialog.current?.show()
                    }}
                >
                    {!isEvent && (
                        <div className='cell-label' style={{ backgroundColor: color }}>
                            <div>{numberWithCommas(cardInfo.cost)}</div>
                        </div>
                    )}
                    <div className='cell-body'>
                        <div className='cell-icon' style={{ backgroundImage: icon }}></div>
                    </div>
                    <div className='cell-level'></div>
                </div>
            )

            cardsInGroupCount[group_id] = cardNum + 1
        }
        return arr
    }

    async function showClickDialog(text: string, subText: string, yesButtonText: string, yesButtonAction: Function | false, noButtonText?: string, noButtonAction?: Function) {
        if (!clickDialog.current ||
            !clickDialogText.current ||
            !clickDialogSubText.current ||
            !clickDialogYesButton.current ||
            !clickDialogNoButton.current
        ) return

        await sleep(globalData.msToWait)

        clickDialogText.current.textContent = text
        clickDialogSubText.current.textContent = subText

        clickDialogYesButton.current.textContent = yesButtonText

        if (yesButtonAction) {
            clickDialogYesButton.current.disabled = false
            clickDialogYesButton.current.onclick = () => {
                yesButtonAction()
                clickDialog.current?.close()
            }
        }
        else {
            clickDialogYesButton.current.disabled = true
            clickDialogYesButton.current.onclick = () => { }
        }

        if (noButtonText) {
            clickDialogNoButton.current.hidden = false
            clickDialogNoButton.current.textContent = noButtonText
            clickDialogNoButton.current.onclick = () => {
                if (noButtonAction) noButtonAction()
                clickDialog.current?.close()
            }
        }
        else {
            clickDialogNoButton.current.hidden = true
        }

        clickDialog.current.show()
    }



    function getOffsetForCell_cqw(coordinate: number): number {
        const totalCellsInLine = monopolyMap.cardsInLine + 4
        const paddingCqw = 3
        const doublePaddingCqw = 2 * paddingCqw

        return (paddingCqw + (100 - doublePaddingCqw) / totalCellsInLine * coordinate)
    }

    function getCellCoordinates(cell: HTMLElement): { x: number, y: number, offsetSignX: number, offsetSignY: number } {
        const cellsInLine = monopolyMap.cardsInLine
        const totalCellsInLine = monopolyMap.cardsInLine + 4
        const corner = cell.getAttribute('mnpl-corner')

        if (corner) {
            let x, y
            switch (corner) {
                default:
                case '0':
                    x = 1
                    y = 1
                    break

                case '1':
                    x = totalCellsInLine - 1
                    y = 1
                    break

                case '2':
                    x = totalCellsInLine - 1
                    y = totalCellsInLine - 1
                    break

                case '3':
                    x = 1
                    y = totalCellsInLine - 1
                    break
            }

            return { x, y, offsetSignX: -0.75, offsetSignY: 1 }
        }

        if (!cell.getAttribute('mnpl-line')) return { x: 1, y: 1, offsetSignX: 1, offsetSignY: 1 }
        const num = Number(cell.style.gridArea.substring(1))

        if (num < cellsInLine) {
            return { x: num + 2, y: 1, offsetSignX: 1, offsetSignY: 1 }
        }

        if (num < cellsInLine * 2) {
            return { x: totalCellsInLine - 1, y: num - cellsInLine + 2, offsetSignX: -1, offsetSignY: -1 }
        }

        if (num < cellsInLine * 3) {
            return { x: totalCellsInLine - 3 - (num - cellsInLine * 2), y: totalCellsInLine - 1, offsetSignX: 1, offsetSignY: 1 }
        }

        return { x: 1, y: totalCellsInLine - 3 - (num - cellsInLine * 3), offsetSignX: -1, offsetSignY: -1 }
    }



    async function movePlayerDot_direct(playerID: string, cellID: string) {
        if (!playerDots.current) return
        const dotSizeCqw = 3

        await sleep(globalData.msToWait)

        let prison_id = undefined
        if (cellID.includes('prison')) {
            prison_id = cellID.substring(cellID.indexOf('_') + 1)
            cellID = 'prison'
        }

        const cell = document.getElementById(cellID)
        if (!cell) return

        const cords = getCellCoordinates(cell)

        const curPos = playerDotPositions?.get(playerID)
        if (curPos && cords.x === curPos.x && cords.y === curPos.y) return

        playerDotPositions?.set(playerID, { x: cords.x, y: cords.y })

        for (let i = 0; i < playerDots.current.children.length; i++) {
            const dot = playerDots.current.children.item(i) as HTMLElement
            if (!dot) continue

            const dotPlayerID = dot.id.substring(dot.id.indexOf('_') + 1)
            if (dotPlayerID === playerID) {

                dot.style.transition = `top: ${MOVE_TIME}ms, left: ${MOVE_TIME}ms`
                dot.style.width = dotSizeCqw + 'cqw'

                let offsetX = cords.offsetSignX * dotSizeCqw / (cords.offsetSignX === 1 ? 1.5 : 2)
                let offsetY = cords.offsetSignY * dotSizeCqw / (cords.offsetSignY === -1 ? 1.5 : 2)


                // if in prison
                if (prison_id) {
                    if (prison_id === '1') {
                        offsetX += dotSizeCqw
                        offsetY += dotSizeCqw
                    }
                    else if (prison_id === '2') {
                        offsetX -= dotSizeCqw
                        offsetY -= dotSizeCqw
                    }
                }

                // random offset
                let playersCountInCell = 0
                playerDotPositions?.forEach(pos => {
                    if (pos.x === cords.x && pos.y === cords.y)
                        playersCountInCell++
                })

                if (playersCountInCell > 1) {
                    const cellBounds = cell.getBoundingClientRect()

                    let randomSizeX
                    let randomSizeY
                    if (cellBounds.width < cellBounds.height) {
                        randomSizeX = dotSizeCqw / 2
                        randomSizeY = cellBounds.height / cellBounds.width * randomSizeX
                    }
                    else {
                        randomSizeY = dotSizeCqw / 2
                        randomSizeX = cellBounds.width / cellBounds.height * randomSizeY
                    }

                    offsetX += (Math.random() * 2 - 1) * randomSizeX
                    offsetY += (Math.random() * 2 - 1) * randomSizeY
                }


                dot.style.left = (getOffsetForCell_cqw(cords.x) + offsetX) + 'cqw'
                dot.style.top = (getOffsetForCell_cqw(cords.y) - offsetY) + 'cqw'

                await sleep(MOVE_TIME)
                break
            }
        }
    }

    // async function movePlayerDot_direct(playerID: string, cellID: string) {
    //     const cell = document.getElementById(cellID)
    //     if (!cell) return

    //     const cords = getCellCoordinates(cell)
    //     await movePlayerDot_direct_cords(playerID, cords)
    // }

    // async function movePlayerDot(playerID: string, cellID: string) {
    //     const totalCellsInLine = monopolyMap.cardsInLine + 4

    //     // const curCellID = gameState?.players[playerID].position
    //     // if (!curCellID) return

    //     if (movingPlayers.get(playerID)) return

    //     const curPos = playerDotPositions?.get(playerID) ?? { x: 0, y: 0 }
    //     if (!curPos) return


    //     // // if in corner
    //     // if ((curPos.x == 1 || curPos.x == totalCellsInLine - 1) &&
    //     //     (curPos.y == 1 || curPos.y == totalCellsInLine - 1)) {
    //     //     movePlayerDot_direct(playerID, cellID)
    //     // }

    //     const cell = document.getElementById(cellID)
    //     if (!cell) return

    //     const cords = getCellCoordinates(cell)

    //     console.log(curPos)
    //     console.log(cords)
    //     if (cords.x !== curPos.x && cords.y !== curPos.y) {

    //         console.log('inside')

    //         if (curPos.y === 1 && cords.y > 1) {
    //             console.log(1)
    //             await movePlayerDot_cords_direct(playerID, { x: totalCellsInLine - 1, y: 1, offsetSignX: -0.75, offsetSignY: 1 })
    //             await movePlayerDot(playerID, cellID)
    //             return
    //         }

    //         if (curPos.x === totalCellsInLine - 1 && cords.x < totalCellsInLine - 1 && curPos.y !== totalCellsInLine - 1) {
    //             console.log(2)
    //             await movePlayerDot_cords_direct(playerID, { x: totalCellsInLine - 1, y: totalCellsInLine - 1, offsetSignX: -0.75, offsetSignY: 1 })
    //             await movePlayerDot(playerID, cellID)
    //             return
    //         }

    //         if (curPos.y === totalCellsInLine - 1 && cords.x === 1) {
    //             console.log(3)
    //             await movePlayerDot_cords_direct(playerID, { x: 1, y: totalCellsInLine - 1, offsetSignX: -0.75, offsetSignY: 1 })
    //             await movePlayerDot(playerID, cellID)
    //             return
    //         }

    //         if (curPos.x === 1 && cords.x > 1) {
    //             console.log(4)
    //             await movePlayerDot_cords_direct(playerID, { x: 1, y: 1, offsetSignX: -0.75, offsetSignY: 1 })
    //             await movePlayerDot(playerID, cellID)
    //             return
    //         }

    //         console.log('direct')
    //         await movePlayerDot_direct(playerID, cellID)
    //     }
    //     else {
    //         console.log('else')
    //         await movePlayerDot_direct(playerID, cellID)
    //     }
    // }




    const playersElements = [
        <div key={'player1'} className='playerCard'>first</div>,
        <div key={'player2'} className='playerCard'>second</div>,
        <div key={'player3'} className='playerCard'>third</div>,
        <div key={'player4'} className='playerCard'>fourth</div>,
        <div key={'player5'} className='playerCard'>fifth</div>
    ]

    const playerDotElements = !gameState ? [] : Object.keys(gameState?.players).map((playerID, i) => {
        const info = gameState.players[playerID]
        return (
            <div
                id={'dot_' + playerID}
                className='playerDot'
                key={i}
                style={{ backgroundColor: info.color }}
            ></div>
        )
    })



    // if (loading || reloading) return <Loading />
    // if (error) return error

    return (
        <div className='monopolyContainer'>
            {/* <dialog></dialog> */}

            <div className='playersContainer'>
                {playersElements}
            </div>

            <div className='boardContainer'>
                <div className='boardWrapper'>

                    <div className='diceCubeContainer'>
                        <DiceCube ref={diceCube1}></DiceCube>
                        <DiceCube ref={diceCube2}></DiceCube>
                    </div>

                    <dialog ref={clickDialog} id='clickDialog'>
                        <div>
                            <h1 ref={clickDialogText}>Заголовок!</h1>
                            <p ref={clickDialogSubText}>Какой-то очень длинный текст снизу, жесть длинный, вообще офигеть.</p>
                            <div className='buttons'>
                                <button ref={clickDialogYesButton}>Да</button>
                                <button ref={clickDialogNoButton}>Нет</button>
                            </div>
                        </div>
                    </dialog>

                    <dialog ref={cardInfoDialog} id='cardInfoDialog' onBlur={e => e.target.close()}>
                        <div className='cardInfoHeader'>
                            <h1>Card name</h1>
                            <h2>Group name</h2>
                        </div>
                        <div className='cardInfoBody'>
                            <div className='groupDescription'>{cardGroupDescription}</div>
                            <div className='groupParams'>{groupInfoParams}</div>
                            <div className='cardParams'>{cardInfoParams}</div>
                        </div>
                    </dialog>



                    <div ref={playerDots}>
                        {playerDotElements}
                    </div>


                    <div className='board' style={{ gridTemplateAreas: gridTemplateAreas }}>
                        {monopolyMap && (
                            <>
                                {getCornerCardsElements()}
                                {getLineCardsElements()}
                            </>
                        )}

                        {/* <div className='monopolyChat' style={{ gridArea: "x", backgroundColor: "black" }}>
                    </div> */}
                    </div>
                </div>
            </div>
        </div>
    )
}



// ========= functions =========
function getFirstLine(cellsInLine: number) {
    let str = '"c0 c0 '

    for (let i = 0; i < cellsInLine; i++) {
        str += `l${i} `
    }

    str += 'c1 c1"'
    return str
}

function getMidLines(cellsInLine: number) {
    const maxLineNum = cellsInLine * 4
    let str = ''

    for (let i = 0; i < cellsInLine; i++) {
        const curRightLine = cellsInLine + i
        const l_left = `l${maxLineNum - i - 1} l${maxLineNum - i - 1}`
        const l_right = `l${curRightLine} l${curRightLine}`
        str += `"${l_left} ${'x '.repeat(cellsInLine)}${l_right}"\n`
    }
    return str
}

function getLastLine(cellsInLine: number) {
    const end = cellsInLine * 3 - 1
    let str = '"c3 c3 '

    for (let i = 0; i < cellsInLine; i++) {
        str += `l${end - i} `
    }

    str += 'c2 c2"'
    return str
}

function createParamsLineElement(name: string | JSX.Element, value: string | JSX.Element, valueType: 'money' | 'diceMultiplier') {
    return (
        <div className='groupParamsLine'>
            <div>{name}</div>
            <div className={valueType + 'Value'}>{value}</div>
        </div>
    )
}



async function rollDice(dice: React.RefObject<HTMLElement>, value: number) {
    const cube = dice.current
    if (!cube || value < 1 || value > 6) return

    cube.style.transition = 'transform 0s'
    cube.style.transform = 'rotateX(0deg) rotateY(0deg)'
    cube.hidden = false
    await sleep(1)

    cube.style.transition = `transform ${DICE_TIME}ms`

    let moveX = 0
    let moveY = 0

    switch (value) {
        case 1:
            moveX = 0
            moveY = 0
            break
        case 2:
            moveX = 0
            moveY = 2
            break
        case 3:
            moveX = 0
            moveY = 3
            break
        case 4:
            moveX = 0
            moveY = 1
            break
        case 5:
            moveX = 3
            moveY = 0
            break
        case 6:
            moveX = 1
            moveY = 0
            break
        default: break
    }

    const xDeg = 90 * (4 + moveX)
    const yDeg = 90 * (4 + moveY)

    cube.style.webkitTransform = 'rotateX(' + xDeg + 'deg) rotateY(' + yDeg + 'deg)'
    cube.style.transform = 'rotateX(' + xDeg + 'deg) rotateY(' + yDeg + 'deg)'

    dice.current.ontransitionend = async () => {
        await sleep(AFTER_DICE_WAIT_TIME)
        cube.hidden = true
    }
    const waitTime = DICE_TIME + AFTER_DICE_WAIT_TIME

    globalData.msToWait = waitTime
    await sleep(waitTime)
    globalData.msToWait = 0
}



function getImageUrl(publicPath?: string) {
    return `url(${publicPath})`
}

function getNumberedFieldName(num: number) {
    const lastDigit = num % 10

    if (lastDigit === 1) return num + ' поле'
    if (lastDigit === 0 || lastDigit > 4) return num + ' полей'

    return num + ' поля'
}

async function getUserInfoByID(userID: string): Promise<PlayerInfo> {
    try {
        const response = await axios.get(ENDPOINTS.Users.GET_USER_INFO_BY_ID_URL + userID)
        return response.data as PlayerInfo

    } catch (error) {
        console.log('Can not get user info!')

        return {
            publicID: userID,
            name: 'unknown',
        }
    }
}
