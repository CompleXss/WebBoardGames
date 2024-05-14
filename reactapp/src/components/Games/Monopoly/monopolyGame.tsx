import axios from 'axios';
import { useEffect, useRef, useState } from 'react';
import { useNavigate } from "react-router-dom";
import { HubConnection } from '@microsoft/signalr';
import LoadingContent from 'src/components/LoadingContent/loadingContent';
import { useWebsocketConnection } from 'src/utilities/useWebsocketHook';
import ENDPOINTS from 'src/utilities/Api_Endpoints';
import { StringMap } from 'src/utilities/utils';
import Loading from "src/components/Loading/loading"
import monopolyMap from './monopoly_map.json'
import cardsInfo from './monopoly_cards.json'
import { ReactComponent as DiceIcon } from 'src/svg/dice.svg'
import { ReactComponent as StarIcon } from 'src/svg/star.svg'
import './monopolyGame.css'

export default function MonopolyGame() {
    const navigate = useNavigate()
    const cardInfoDialog = useRef<HTMLDialogElement>(null)
    const [cardGroupDescription, setCardGroupDescription] = useState<string>('')
    const [groupInfoParams, setGroupInfoParams] = useState<JSX.Element[]>([])
    const [cardInfoParams, setCardInfoParams] = useState<JSX.Element[]>([])
    const [gridTemplateAreas, setGridTemplateAreas] = useState<string>()

    // query ended
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

            const key = `card_${card.id}`

            arr[i] = (
                <div id={key} key={key} className='cell' mnpl-corner={i} style={{ gridArea: `c${i}`, backgroundImage: icon }}>
                </div>
            )
        }
        return arr
    }

    function getLineCardsElements() {
        if (monopolyMap.layout.length !== (4 * monopolyMap.cardsInLine)) {
            console.error('Invalid monopoly map json. Amount of line cards don\' match.')
        }

        const count = Math.min(monopolyMap.layout.length, 4 * monopolyMap.cardsInLine)
        const arr = new Array<JSX.Element>(count)
        const cardsInGroupCount: StringMap<number> = {}

        for (let i = 0; i < count; i++) {
            const group_id = monopolyMap.layout[i]
            const cardNum = cardsInGroupCount[group_id] ?? 0

            const group = monopolyMap.cardGroups.find(x => x.id === group_id)
            const isEvent = group_id.startsWith('event_')
            const needsRotate = group_id.includes('random')
            let icon = ''
            let color = ''
            let cardName = ''
            let groupName = ''

            if (group) {
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
            } else if (isEvent) {
                icon = cardsInfo.eventCards.find(x => x.id === group_id)?.icon ?? ''
            }
            icon = getImageUrl(cardsInfo.iconFolder + icon)

            const key = `card_${group_id}_${cardNum}`
            const line = Math.floor(i / monopolyMap.cardsInLine)
            const mnpl_special = isEvent ? 1 : null
            const mnpl_rotate = needsRotate ? 1 : null

            arr[i] = (
                <div
                    id={key}
                    key={key}
                    className='cell'
                    mnpl-line={line}
                    mnpl-special={mnpl_special}
                    mnpl-rotate={mnpl_rotate}
                    style={{ gridArea: `l${i}` }}
                    onClick={isEvent ? undefined : () => {
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
                            if (body && group) {

                                // description
                                setCardGroupDescription(
                                    cardsInfo.groupTypes
                                        .find(x => x.type === group.type)
                                        ?.description.replace('{groupName}', groupName) ?? ''
                                )

                                // group params
                                const groupParams: JSX.Element[] = []
                                const cardParams: JSX.Element[] = []

                                switch (group.type) {
                                    case 'upgrade':
                                        const rent = (group.cards[cardNum] as {
                                            rent: number[];
                                        }).rent;

                                        groupParams.push(createParamsLineElement(
                                            cardsInfo.translation.rent_0,
                                            rent[0].toString(),
                                            'money'
                                        ))

                                        for (let i = 1; i < rent.length; i++) {
                                            const stars = Array(i).fill(<StarIcon className='starIcon'/>)
                                            groupParams.push(createParamsLineElement(
                                                <div>{stars}</div>,
                                                rent[i].toString(),
                                                'money'
                                            ))
                                        }

                                        break;

                                    case 'count':
                                        if (!group.multipliers) return

                                        for (let i = 0; i < group.multipliers.length; i++) {
                                            groupParams.push(createParamsLineElement(
                                                getNumberedFieldName(i + 1),
                                                group.multipliers[i].toString(),
                                                'money'
                                            ))
                                        }

                                        break;

                                    case 'dice':
                                        if (!group.multipliers) return

                                        for (let i = 0; i < group.multipliers.length; i++) {
                                            groupParams.push(createParamsLineElement(
                                                getNumberedFieldName(i + 1),
                                                <div><DiceIcon className='diceIcon'/> {' x ' + group.multipliers[i]}</div>,
                                                'diceMultiplier'
                                            ))
                                        }

                                        break;

                                    default:
                                        break;
                                }

                                // card params
                                const card = group.cards[cardNum] as {
                                    buyCost: number;
                                    sellCost: number;
                                    rebuyCost: number;
                                    upgradeCost?: number;
                                }

                                cardParams.push(createParamsLineElement(
                                    cardsInfo.translation.buyCost,
                                    card.buyCost.toString(),
                                    'money'
                                ))
                                cardParams.push(createParamsLineElement(
                                    cardsInfo.translation.sellCost,
                                    card.sellCost.toString(),
                                    'money'
                                ))
                                cardParams.push(createParamsLineElement(
                                    cardsInfo.translation.rebuyCost,
                                    card.rebuyCost.toString(),
                                    'money'
                                ))
                                if (card.upgradeCost) cardParams.push(createParamsLineElement(
                                    cardsInfo.translation.upgradeCost,
                                    card.upgradeCost.toString(),
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
                    {group && (
                        <div className='cell-label' style={{ backgroundColor: color }}>
                            <div>200</div>
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

    function getNumberedFieldName(num: number) {
        const lastDigit = num % 10

        if (lastDigit === 1) return num + ' поле'
        if (lastDigit === 0 || lastDigit > 4) return num + ' полей'

        return num + ' поля'
    }

    function createParamsLineElement(name: string | JSX.Element, value: string | JSX.Element, valueType: 'money' | 'diceMultiplier') {
        return (
            <div className='groupParamsLine'>
                <p>{name}</p>
                <p className={valueType + 'Value'}>{value}</p>
            </div>
        )
    }

    function spawnCells() {
        return (
            <>
                {getCornerCardsElements()}
                {getLineCardsElements()}
            </>
        )
    }



    const players = [
        <div key={'player1'} className='playerCard'>first</div>,
        <div key={'player2'} className='playerCard'>second</div>,
        <div key={'player3'} className='playerCard'>third</div>,
        <div key={'player4'} className='playerCard'>fourth</div>
    ]

    return (
        <div className='monopolyContainer'>
            {/* <dialog></dialog> */}

            <div className='playersContainer'>
                {players}
            </div>

            <div className='boardContainer'>
                <div className='boardWrapper'>
                    <dialog ref={cardInfoDialog} id='cardInfoDialog' onBlur={e => e.target.close()}>
                        <div className='cardInfoHeader'>
                            <h1>Card name</h1>
                            <h2>Group name</h2>
                        </div>
                        <div className='cardInfoBody'>
                            <p className='groupDescription'>{cardGroupDescription}</p>
                            <p className='groupParams'>{groupInfoParams}</p>
                            <p className='cardParams'>{cardInfoParams}</p>
                        </div>
                    </dialog>

                    <div className='board' style={{ gridTemplateAreas: gridTemplateAreas }}>
                        {monopolyMap && spawnCells()}

                        {/* <div className='monopolyChat' style={{ gridArea: "x", backgroundColor: "black" }}>
                    </div> */}
                    </div>
                </div>
            </div>
        </div>
    )
}



function getFirstLine(num: number) {
    let str = '"c0 c0 '

    for (let i = 0; i < num; i++) {
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

function getImageUrl(publicPath?: string) {
    return `url(${publicPath})`
}