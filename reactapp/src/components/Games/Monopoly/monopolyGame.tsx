import React, { useEffect, useRef, useState } from 'react';
import { useNavigate } from "react-router-dom";
import { HubConnection } from '@microsoft/signalr';
import { useWebsocketConnection } from 'src/utilities/useWebsocketHook';
import { GameNames } from 'src/utilities/GameNames';
import { StringMap, numberWithCommas, sleep } from 'src/utilities/utils';
import { User as PlayerInfo } from 'src/utilities/Api_DataTypes';
import { ReactComponent as DiceIcon } from 'src/svg/dice.svg'
import { ReactComponent as StarIcon } from 'src/svg/star.svg'
import { DiceCube } from './DiceCube/diceCube';
import { cumulativeOffset, getImageUrl } from 'src/utilities/frontend.utils';
import { useWinnerDialog } from '../WinnerDialog/winnerDialog';
import { getUserInfoByID } from 'src/utilities/EndpointHelpers';
import ENDPOINTS from 'src/utilities/Api_Endpoints';
import Loading from "src/components/Loading/loading"
import monopolyMap from './monopoly_map.json'
import cardsInfo from './monopoly_cards.json'
import reactStringReplace from 'react-string-replace'
import './monopolyGame.css'

interface GameState {
    myID: string
    isMyTurn: boolean
    isAbleToUpgrade: boolean
    actingPlayerID: string
    players: StringMap<PlayerState>
    cellStates: StringMap<CellState>
    chatMessages: string[]
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
    PayToPlayer,
    DiceToMove,
    DiceToExitPrison,
    BuyCell,
    UpgradeCell,
    DowngradeCell,
    CreateContract,
}



const MOVE_TIME_MS = 1000
const DICE_TIME_MS = 1000
const AFTER_DICE_WAIT_TIME_MS = 500

const globalData = {
    msToWait_dice: 0,
    msToWait_move: 0,
}

const gameName = GameNames.monopoly

export default function MonopolyGame() {
    const navigate = useNavigate()
    const [playerInfos, setPlayerInfos] = useState<Map<string, PlayerInfo>>(new Map())
    const [gameState, setGameState] = useState<GameState | undefined>()
    const [cardButtons, setCardButtons] = useState<JSX.Element[]>([])
    const [cardGroupDescription, setCardGroupDescription] = useState<string>('')
    const [groupInfoParams, setGroupInfoParams] = useState<JSX.Element[]>([])
    const [cardInfoParams, setCardInfoParams] = useState<JSX.Element[]>([])
    const [gridTemplateAreas, setGridTemplateAreas] = useState<string>()
    const [playerDotPositions, setPlayerDotPositions] = useState<Map<string, { x: number, y: number }>>(new Map())
    const [turnTimerSecondsLeft, setTurnTimerSecondsLeft] = useState<number>(99)
    const chatInput = useRef<HTMLInputElement>(null)
    const clickDialog = useRef<HTMLDialogElement>(null)
    const cardInfoDialog = useRef<HTMLDialogElement>(null)
    const clickDialogYesButton = useRef<HTMLButtonElement>(null)
    const clickDialogNoButton = useRef<HTMLButtonElement>(null)
    const clickDialogText = useRef<HTMLHeadingElement>(null)
    const clickDialogSubText = useRef<HTMLParagraphElement>(null)
    const diceCube1 = useRef<HTMLDivElement>(null)
    const diceCube2 = useRef<HTMLDivElement>(null)
    const playerDots = useRef<HTMLDivElement>(null)
    const surrenderDialog = useRef<HTMLDialogElement>(null)
    const { showWinner, element: winnerDialog, } = useWinnerDialog()

    useEffect(() => {
        document.title = 'Монополия'
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
            // react captures state values in eventHandler so we'll update them every time
            // bruh
            addEventHandlers(connection)
        }
        requestLastOffer()

        if (!gameState?.players) return

        loadPlayerInfos(gameState)

        const playerIDs = Object.keys(gameState.players)
        for (const playerID of playerIDs) {
            const info = gameState.players[playerID]
            if (info.isDead && playerID === gameState.myID) {
                clickDialog.current?.close()
            }

            movePlayerDot(playerID, info.position)
        }
    }, [gameState])

    async function loadPlayerInfos(gameState: GameState) {
        let addedNew = false

        for (const playerID of Object.keys(gameState.players)) {
            if (!playerInfos.has(playerID)) {
                const info = await getUserInfoByID(playerID)
                playerInfos.set(playerID, info)

                addedNew = true
            }
        }

        if (addedNew) {
            setPlayerInfos(new Map(playerInfos))
        }
    }



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
                    if (player) {
                        player.isOnline = false
                        return { ...state } as GameState
                    }
                }
                return state
            })
        })

        connectionOnExclusive(connection, 'UserReconnected', async userID => {
            setGameState(state => {
                if (state) {
                    const player = state.players[userID]
                    if (player) {
                        player.isOnline = true
                        return { ...state } as GameState
                    }
                }
                return state
            })
        });

        // remove all callbacks from following methods
        (connection as any)._reconnectingCallbacks = [] as any;
        (connection as any)._reconnectedCallbacks = [] as any;
        (connection as any)._closedCallbacks = [] as any;

        connection.onreconnecting(() => setReloading(true))
        connection.onreconnected(getGameState)
        connection.onclose(() => setReloading(true))

        connectionOnExclusive(connection, 'GameClosed', winnerID => {
            if (!winnerID) {
                navigate('/')
                return
            }

            showWinner(winnerID)
        })

        connectionOnExclusive(connection, 'TurnTimerTicked', turnTimerTicked)



        // actions
        connectionOnExclusive(connection, 'ShowDiceRoll', data => {
            rollDice(diceCube1, data.dice1)
            rollDice(diceCube2, data.dice2)
        })

        connectionOnExclusive(connection, 'ChatMessage', message => {
            if (!message) return
            gameState?.chatMessages.push(message)
            setGameState(state => {
                return { ...state } as GameState
            })
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
                enoughMoneyToPay ? () => makeMove(connection, ActionType.PayToPlayer) : false
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

        connectionOnExclusive(connection, 'OfferBuyCell', ({ cellID }) => {
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
            return payAmount <= gameState.players[gameState.myID].money
        }
    }

    function connectionOnExclusive(connection: HubConnection, methodName: string, newMethod: (...args: any[]) => any) {
        connection.off(methodName)
        connection.on(methodName, newMethod)
    }

    function turnTimerTicked(secondsLeft: number) {
        if (secondsLeft === undefined || secondsLeft === null) return
        setTurnTimerSecondsLeft(secondsLeft)
    }



    function makeMove(connection: HubConnection, actionType?: ActionType, cellID?: string) {
        connection.invoke('MakeMove', {
            actionType,
            cellID,
        })
            .then(x => {
                if (x.statusCode && x.statusCode !== 200) {
                    console.log(x.value)
                    requestLastOffer()
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



    function getGameState() {
        if (!connection) return

        connection.invoke('GetGameState')
            .then(response => {
                if (response.value) {
                    setGameState(response.value)
                    setReloading(false)
                }
                else setGameState(undefined)
            })
            .catch(e => { })
    }

    function getTextForPayReason(reason: string): { title: string, description: string } {
        switch (reason) {
            case 'ExitPrison':
                return {
                    title: 'Заплатите за освобождение.',
                    description: 'У вас кончились попытки на выбрасывание дубля. Теперь придётся заплатить, чтобый выйти из тюрьмы.'
                }

            case 'EventRandom':
                return {
                    title: 'Внезапные траты.',
                    description: 'На некоторых полях вы платите, потому что так решила судьба. Такое может случиться с каждым...'
                }

            case 'EventMoney':
                return {
                    title: 'Заплатите Банку.',
                    description: 'На некоторых полях вы платите Банку. Или он вам. Это как повезет.'
                }

            default:
                return {
                    title: 'Надо заплатить',
                    description: 'Кто ж знал, что все так обернется...'
                }
        }
    }

    function requestLastOffer() {
        requestGame('RepeatLastOffer')
    }

    function requestGame(request: string, data?: any) {
        connection?.invoke('Request', request, data)
            .catch(_ => { })
    }

    function sendMessageInChat_onKeyDown(e: React.KeyboardEvent<HTMLInputElement>) {
        if (e.key !== 'Enter') return
        sendMessageInChat()
    }

    function sendMessageInChat() {
        if (!chatInput.current) return

        const value = chatInput.current.value?.trim()
        if (!value || value === '') return

        if (value.length > 512) {
            alert('Сообщение слишком длинное!')
            return
        }

        requestGame('SendChatMessage', value)
        chatInput.current.value = ''
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
        if (!gameState?.cellStates) return

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
            const mnpl_rotate = group_id.includes('random')
                ? 180
                : group_id.includes('money') ? -90 : undefined

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
            const mnpl_special = isEvent ? 1 : undefined
            const backgroundColor = cardInfo?.ownerID ? gameState.players[cardInfo.ownerID].color : undefined

            const cellLevel = (!cardInfo ? '' : cardInfo.isSold
                // display it with lock icon
                ? cardInfo.movesLeftToLooseThisCell
                // show level stars
                : cardInfo.upgradeLevel > 4
                    ? <StarIcon key={0} className='starIcon big'></StarIcon>
                    : Array(cardInfo.upgradeLevel).fill(0).map((_, i) => (
                        <StarIcon key={i} className='starIcon'></StarIcon>
                    ))
            )

            arr[i] = (
                <div
                    id={card_id}
                    key={card_id}
                    className='cell'
                    mnpl-line={line}
                    mnpl-special={mnpl_special}
                    mnpl-rotate={mnpl_rotate}
                    style={{ gridArea: `l${i}` }}
                    onClick={isEvent || !cardInfo ? undefined : e => {
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

                                // buttons (upgrade-downgrade)
                                const cardButtons: JSX.Element[] = []

                                if (gameState.isMyTurn && cardInfo.ownerID && cardInfo.ownerID === gameState.myID) {
                                    let upBtn = false
                                    let downBtn = false
                                    let upgradeBtnName
                                    let downgradeBtnName

                                    if (cardInfo.isSold) {
                                        upgradeBtnName = 'Выкупить'
                                        upBtn = true
                                    }
                                    else if (cardInfo.type !== 'upgrade') {
                                        downgradeBtnName = 'Заложить'
                                        downBtn = true
                                    }
                                    else { // if not sold & 'upgrade'
                                        let totalOfTheSameGroupCount = 0
                                        const ownedOfTheSameGroupCells = Object.keys(gameState.cellStates).filter(cellID => {
                                            const groupID = cellID.substring(0, cellID.lastIndexOf('_'))
                                            const cell = gameState.cellStates[cellID]

                                            if (groupID !== group_id) return false
                                            totalOfTheSameGroupCount++

                                            return cell.ownerID && cell.ownerID === cardInfo.ownerID
                                        }).map(id => gameState.cellStates[id])

                                        if (ownedOfTheSameGroupCells.length !== totalOfTheSameGroupCount) {
                                            downgradeBtnName = 'Заложить'
                                            downBtn = true
                                        }
                                        else { // if owned === total
                                            if (ownedOfTheSameGroupCells.every(x => x.upgradeLevel <= cardInfo.upgradeLevel)) {
                                                downgradeBtnName = 'Заложить'
                                                downBtn = true
                                            }

                                            if (ownedOfTheSameGroupCells.every(x => x.upgradeLevel >= cardInfo.upgradeLevel && !x.isSold) &&
                                                gameState.isAbleToUpgrade
                                            ) {
                                                upgradeBtnName = 'Построить'
                                                upBtn = true
                                            }
                                        }
                                    }

                                    if (upBtn) cardButtons.push((
                                        <button key={0} className='upBtn' onClick={() => {
                                            if (connection) {
                                                makeMove(connection, ActionType.UpgradeCell, card_id)
                                            }
                                            cardInfoDialog.current?.close()
                                        }}>{upgradeBtnName}</button>
                                    ))
                                    if (downBtn) cardButtons.push((
                                        <button key={1} className='downBtn' onClick={() => {
                                            if (connection) {
                                                makeMove(connection, ActionType.DowngradeCell, card_id)
                                            }
                                            cardInfoDialog.current?.close()
                                        }}>{downgradeBtnName}</button>
                                    ))
                                }

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
                                            'money',
                                            0
                                        ))

                                        for (let i = 1; i < rent.length; i++) {
                                            const stars = i === 5
                                                ? <StarIcon key={0} className='starIcon big' />
                                                : Array(i).fill(0).map((_, i) => (
                                                    <StarIcon key={i} className='starIcon' />
                                                ))

                                            groupParams.push(createParamsLineElement(
                                                <div>{stars}</div>,
                                                numberWithCommas(rent[i]),
                                                'money',
                                                i
                                            ))
                                        }

                                        break;

                                    case 'count':
                                        if (!cardInfo.multipliers) return

                                        for (let i = 0; i < cardInfo.multipliers.length; i++) {
                                            groupParams.push(createParamsLineElement(
                                                getNumberedFieldName(i + 1),
                                                cardInfo.multipliers[i].toString(),
                                                'money',
                                                i
                                            ))
                                        }

                                        break;

                                    case 'dice':
                                        if (!cardInfo.multipliers) return

                                        for (let i = 0; i < cardInfo.multipliers.length; i++) {
                                            groupParams.push(createParamsLineElement(
                                                getNumberedFieldName(i + 1),
                                                <div><DiceIcon className='diceIcon' /> {' x ' + cardInfo.multipliers[i]}</div>,
                                                'diceMultiplier',
                                                i
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
                                    'money',
                                    0
                                ))
                                cardParams.push(createParamsLineElement(
                                    cardsInfo.translation.sellCost,
                                    numberWithCommas(cardInfo.info.sellCost),
                                    'money',
                                    1
                                ))
                                cardParams.push(createParamsLineElement(
                                    cardsInfo.translation.rebuyCost,
                                    numberWithCommas(cardInfo.info.rebuyCost),
                                    'money',
                                    2
                                ))
                                if (cardInfo.info.upgradeCost) cardParams.push(createParamsLineElement(
                                    cardsInfo.translation.upgradeCost,
                                    numberWithCommas(cardInfo.info.upgradeCost),
                                    'money',
                                    3
                                ))

                                // set params
                                setCardButtons(cardButtons)
                                setGroupInfoParams(groupParams)
                                setCardInfoParams(cardParams)
                            }
                        }

                        if (cardInfoDialog.current) {
                            cardInfoDialog.current.show()
                            cardInfoDialog.current.focus()
                            cardInfoDialog.current.style.opacity = '0'

                            const offset = cumulativeOffset(e.currentTarget)
                            const elementHeight = e.currentTarget.offsetHeight
                            const elementWidth = e.currentTarget.offsetWidth
                            let top = e.currentTarget.offsetTop
                            let left = offset.left - window.innerWidth / 2

                            // left is kinda magic but works almost perfectly

                            // cardInfoDialog.current.offsetHeight can be retrieved only after dialog.show()
                            // but e.* can be retrieved only BEFORE this timeout (that's why it's above)
                            setTimeout(() => {
                                if (!cardInfoDialog.current) return

                                // top
                                if (top < elementHeight) top += elementHeight
                                const dialogOutOfBoundsY = (offset.top + cardInfoDialog.current.offsetHeight + elementHeight * 1.3) - window.innerHeight
                                if (dialogOutOfBoundsY > 0) top -= dialogOutOfBoundsY

                                // left
                                if (left < elementWidth) left += elementWidth

                                cardInfoDialog.current.style.left = left + 'px'
                                cardInfoDialog.current.style.top = top + 'px'

                                cardInfoDialog.current.style.opacity = '1'
                            }, 1)
                        }
                    }}
                >
                    {!isEvent && (
                        <div className='cell-label' style={{ backgroundColor: color }}>
                            <div mnpl-x={cardInfo.type === 'dice' ? 1 : undefined}>
                                {numberWithCommas(cardInfo.cost)}
                            </div>
                        </div>
                    )}
                    <div className='cell-body' style={{ backgroundColor: backgroundColor, filter: cardInfo?.isSold ? 'brightness(80%)' : undefined }}>
                        <div className='cell-icon' style={{ backgroundImage: icon }}></div>
                    </div>
                    {!isEvent && (
                        <div className='cell-level' mnpl-sold={cardInfo.isSold ? 1 : undefined}>
                            {cellLevel}
                        </div>
                    )}
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

        await sleep(globalData.msToWait_dice)

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
        const totalCellsInLine = cellsInLine + 4
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

    function getCellIDByCords(cords: { x: number, y: number }): string | undefined {
        const totalCellsInLine = monopolyMap.cardsInLine + 4

        // corner 0
        if (cords.x === 1 && cords.y === 1) {
            return document.querySelector('[mnpl-corner="0"]')?.id
        }

        // corner 1
        if (cords.x === totalCellsInLine - 1 && cords.y === 1) {
            return document.querySelector('[mnpl-corner="1"]')?.id
        }

        // corner 2
        if (cords.x === totalCellsInLine - 1 && cords.y === totalCellsInLine - 1) {
            return document.querySelector('[mnpl-corner="2"]')?.id
        }

        // corner 3
        if (cords.x === 1 && cords.y === totalCellsInLine - 1) {
            return document.querySelector('[mnpl-corner="3"]')?.id
        }

        // line 0
        if (cords.y === 1) {
            const cells = [...document.querySelectorAll('[mnpl-line="0"]')]
            return cells[cords.x - 2]?.id
        }

        // line 1
        if (cords.x === totalCellsInLine - 1) {
            const cells = [...document.querySelectorAll('[mnpl-line="1"]')]
            return cells[cords.y - 2]?.id
        }

        // line 2
        if (cords.y === totalCellsInLine - 1) {
            const cells = [...document.querySelectorAll('[mnpl-line="2"]')]
            return cells[totalCellsInLine - cords.x - 3]?.id
        }

        // line 3
        if (cords.x === 1) {
            const cells = [...document.querySelectorAll('[mnpl-line="3"]')]
            return cells[totalCellsInLine - cords.y - 3]?.id
        }

        return undefined
    }

    async function movePlayerDot(playerID: string, cellID: string) {
        if (!playerDots.current) return

        await sleep(globalData.msToWait_dice)
        await sleep(globalData.msToWait_move)

        const moveQueue = getMovePlayerQueue(playerID, cellID)

        // move
        for (const cellID of moveQueue) {
            await movePlayerDot_direct(playerID, cellID)
        }
    }

    function getMovePlayerQueue(playerID: string, cellID: string): string[] {
        const totalCellsInLine = monopolyMap.cardsInLine + 4
        const maxLoopIterations = totalCellsInLine * 4 + 1

        const cell = getCellByID(cellID)
        if (!cell) return []

        const destination = getCellCoordinates(cell)
        let curPos = playerDotPositions?.get(playerID)

        if (!curPos || (curPos.x === destination.x && curPos.y === destination.y)) {
            if (cellID.startsWith('prison_2_')) {
                return ['prison_2']
            }
            return [cellID]
        }

        if (cellID.startsWith('prison_2_')) {
            const lastCellID = cellID.replace('prison_2_', '')
            return [...getMovePlayerQueue(playerID, lastCellID), 'prison_2']
        }

        curPos = { ...curPos } // make a copy

        const moveQueue: string[] = []
        let iterationCounter = 0
        let xInc
        let yInc

        while (curPos.x !== destination.x || curPos.y !== destination.y) {
            if (curPos.y === 1 && curPos.x !== totalCellsInLine - 1) {
                // right
                xInc = 1
                yInc = 0
            }
            else if (curPos.x === totalCellsInLine - 1 && curPos.y !== totalCellsInLine - 1) {
                // down
                xInc = 0
                yInc = 1
            }
            else if (curPos.y === totalCellsInLine - 1 && curPos.x !== 1) {
                // left
                xInc = -1
                yInc = 0
            }
            else {
                // up
                xInc = 0
                yInc = -1
            }

            curPos.x += xInc
            curPos.y += yInc

            if (curPos.x === destination.x && curPos.y === destination.y) {
                break
            }

            // if in corner
            if ((curPos.x === 1 || curPos.x === totalCellsInLine - 1) &&
                (curPos.y === 1 || curPos.y === totalCellsInLine - 1)) {

                let cell_id = getCellIDByCords(curPos)

                if (cell_id === 'prisonEnter' && cellID === 'prison_2') {
                    moveQueue.push(cell_id)
                    break // move to prison straight from prisonEnter
                }

                if (cell_id === 'prison') cell_id += '_1'
                if (cell_id) moveQueue.push(cell_id)
            }

            iterationCounter++
            if (iterationCounter > maxLoopIterations) {
                console.log('getMovePlayerQueue loop overflow')
                break
            }
        }

        moveQueue.push(cellID)
        return moveQueue
    }

    async function movePlayerDot_direct(playerID: string, cellID: string) {
        if (!playerDots.current) return
        const dotSizeCqw = 3

        const prison_id = getPrisonIDFromCellID(cellID)
        const cell = getCellByID(cellID)
        if (!cell) return

        const cords = getCellCoordinates(cell)
        const curPos = playerDotPositions?.get(playerID)
        let moveTime = MOVE_TIME_MS

        if (curPos && cords.x === curPos.x && cords.y === curPos.y) {
            moveTime = 0
        }

        setPlayerDotPositions(pos => {
            pos.set(playerID, { x: cords.x, y: cords.y })
            return new Map(pos)
        })

        for (let i = 0; i < playerDots.current.children.length; i++) {
            const dot = playerDots.current.children.item(i) as HTMLElement
            if (!dot) continue

            const dotPlayerID = dot.id.substring(dot.id.indexOf('_') + 1)
            if (dotPlayerID === playerID) {

                dot.hidden = false
                dot.style.transition = `top: ${moveTime}ms, left: ${moveTime}ms`
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

                globalData.msToWait_move = moveTime
                await sleep(moveTime)
                globalData.msToWait_move = 0
                break
            }
        }
    }

    function getCellByID(cellID: string) {
        if (cellID.includes('prison_')) {
            cellID = 'prison'
        }

        return document.getElementById(cellID)
    }

    function getPrisonIDFromCellID(cellID: string) {
        let prison_id = undefined
        if (cellID.includes('prison_')) {
            prison_id = cellID.substring(cellID.indexOf('_') + 1)
        }

        return prison_id
    }



    const playersElements = (!gameState?.players || !gameState.myID || !gameState.actingPlayerID) ? [] : Object.keys(gameState.players).map((playerID, i) => {
        const player = gameState.players[playerID]
        const name = playerInfos.get(playerID)?.name ?? '???'
        const secondsLeft = (gameState.actingPlayerID === playerID && turnTimerSecondsLeft < 30
            ? turnTimerSecondsLeft.toString()
            : ''
        )

        return (
            <div className='playerCardDropDown' key={i} onClick={e => {
                const element = e.currentTarget.querySelector('.playerCardButtons')
                element?.classList.toggle('show')

                const closest = (e.target as Element)?.closest('.playerCardDropDown')

                function close(e: MouseEvent) {
                    const target = e.target as Element
                    if (!target) return

                    if (target.closest('.playerCardDropDown') !== closest) {
                        element?.classList.toggle('show')
                    }

                    if (!element?.classList.contains('show')) {
                        window.removeEventListener('click', close)
                    }
                }

                window.addEventListener('click', close)
            }}>
                <div className='playerCard' mnpl-dead={player.isDead ? 1 : undefined}>
                    <div className='cardLine'>
                        <p>{name}</p>
                        <div className={'onlineIndicator ' + (player.isOnline ? 'on' : 'off')}></div>
                    </div>
                    <div className='cardLine'>
                        <p className='money'>
                            {player.isDead ? '💀' : numberWithCommas(player.money)}
                        </p>
                        <p className='secondsLeft'>{secondsLeft}</p>
                    </div>
                    <div className='line' style={{ backgroundColor: player.color }}></div>
                </div>
                <div className='playerCardButtons'>
                    {/* {!player.isDead && playerID !== gameState.myID && (
                        <button>Договор 💸</button>
                    )} */}
                    {playerID === gameState.myID && (
                        <button onClick={showSurrenderDialog}>Сдаться 💀</button>
                    )}
                </div>
            </div>
        )
    })



    const playerDotElements = !gameState?.players ? [] : Object.keys(gameState.players).map((playerID, i) => {
        const info = gameState.players[playerID]
        if (info.isDead) return undefined

        return (
            <div
                id={'dot_' + playerID}
                className='playerDot'
                key={i}
                style={{ backgroundColor: info.color }}
                hidden
            ></div>
        )
    })

    const chatMessages = !gameState?.players ? [] : gameState.chatMessages.map((line, index) => {

        // replace {playerID:xxx} with JSX
        let elements = reactStringReplace(line, /{playerID:([^}]+)}/g, (playerID, i) => {
            const playerName = playerInfos.get(playerID)?.name
            const playerColor = gameState.players[playerID].color
            return <span key={i} style={{ color: playerColor }}>
                {playerName ?? '???'}
            </span>
        })

        // replace {mesPlayerID:xxx} with JSX
        elements = reactStringReplace(elements, /{mesPlayerID:([^}]+)}/g, (playerID, i) => {
            const playerName = playerInfos.get(playerID)?.name
            const playerColor = gameState.players[playerID].color
            return <span key={i} className='chatPlayerName' style={{ backgroundColor: playerColor }}>
                {playerName ?? '???'}
            </span>
        })

        // replace {cellID:xxx} with JSX
        elements = reactStringReplace(elements, /{cellID:([^}]+)}/g, (cellID, i) => {
            const last_index = cellID.lastIndexOf('_')
            const cellGroupID = cellID.substring(0, last_index)
            const cellNum = Number(cellID.substring(last_index + 1))

            const cellname = cellNum > -1
                ? cardsInfo.cardGroups.find(x => x.id === cellGroupID)?.cards[cellNum].name
                : cellID

            return cellname
        })

        return <p key={index}>{elements}</p>
    }).reverse()



    if (loading || reloading) return <Loading />
    if (error) return error

    return (
        <div className='monopolyContainer'>
            {winnerDialog}

            <div className='playersContainer'>
                {playersElements}
            </div>

            <div className='boardContainer'>
                <div className='boardWrapper'>

                    <div className='diceCubeContainer'>
                        <DiceCube hidden ref={diceCube1}></DiceCube>
                        <DiceCube hidden ref={diceCube2}></DiceCube>
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

                    <dialog ref={cardInfoDialog} id='cardInfoDialog' onBlur={closeCardInfoDialog}>
                        <div className='cardInfoHeader'>
                            <h1>Card name</h1>
                            <h2>Group name</h2>
                        </div>
                        <div className='cardInfoBody'>
                            <div className='cardButtons'>{cardButtons}</div>
                            <div className='groupDescription'>{cardGroupDescription}</div>
                            <div className='groupParams'>{groupInfoParams}</div>
                            <div className='cardParams'>{cardInfoParams}</div>
                        </div>
                    </dialog>


                    <div ref={playerDots}>
                        {playerDotElements}
                    </div>


                    <div className='board' style={{ gridTemplateAreas: gridTemplateAreas }}>
                        {monopolyMap && gameState && (
                            <>
                                {getCornerCardsElements()}
                                {getLineCardsElements()}
                            </>
                        )}

                        <div className='monopolyChat' style={{ gridArea: 'x' }}>
                            <div className='chatInput' >
                                <input ref={chatInput} placeholder='Введите сообщение' type='text' maxLength={512} onKeyDown={sendMessageInChat_onKeyDown} />
                                <button onClick={sendMessageInChat}>
                                    <div className='icon'></div>
                                </button>
                            </div>
                            <div className='chatMessages'>
                                {chatMessages}
                            </div>
                        </div>
                    </div>
                </div>
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

function createParamsLineElement(name: string | JSX.Element, value: string | JSX.Element, valueType: 'money' | 'diceMultiplier', key: number | string) {
    return (
        <div className='groupParamsLine' key={key}>
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

    cube.style.transition = `transform ${DICE_TIME_MS}ms`

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

    cube.style.transform = 'rotateX(' + xDeg + 'deg) rotateY(' + yDeg + 'deg)'


    // dice.current.ontransitionend = async () => {
    //     await sleep(AFTER_DICE_WAIT_TIME_MS)

    // }
    const waitTime = DICE_TIME_MS + AFTER_DICE_WAIT_TIME_MS

    globalData.msToWait_dice = waitTime
    await sleep(waitTime)
    cube.hidden = true
    globalData.msToWait_dice = 0
}

function getNumberedFieldName(num: number) {
    const lastDigit = num % 10

    if (lastDigit === 1) return num + ' поле'
    if (lastDigit === 0 || lastDigit > 4) return num + ' полей'

    return num + ' поля'
}

function closeCardInfoDialog(e: React.FocusEvent<HTMLDialogElement, Element>) {
    if (e.target.id === 'cardInfoDialog' &&
        e.relatedTarget?.parentElement?.className !== 'cardButtons'
    ) {
        e.target.close()
    }
}
