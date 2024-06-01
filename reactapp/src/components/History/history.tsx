import axios from 'axios'
import { useEffect, useState } from 'react'
import { useQuery } from 'react-query'
import { mapCheckers, mapMonopoly } from './gameHistoryMappers'
import { Games, GameHistory, GameNamesRu } from '../../utilities/Api_DataTypes'
import Loading from '../Loading/loading'
import ENDPOINTS from '../../utilities/Api_Endpoints'
import './history.css'

export default function History() {
    const [gameName, setGameName] = useState<Games>(Object.values(Games)[0] ?? Games.checkers)
    const { data, isLoading, isError } = useQuery('history', fetchData)
    const d = data as { userID: string, history: GameHistory }
    const myID = d ? d.userID : null
    const history = d ? d.history : null

    useEffect(() => {
        document.title = 'История игр'
    }, [])

    const games = !history || !myID ? [] : Object.keys(history)
        .filter(name => name === gameName)
        .filter(name => history[name].length !== 0).map((name, index) => {
            switch (name) {
                case Games.checkers:
                    return (
                        <div className='table_wrapper' key={'Game ' + index}>
                            {mapCheckers(myID, history[name])}
                        </div>
                    )

                case Games.monopoly:
                    return (
                        <div className='table_wrapper' key={'Game ' + index}>
                            {mapMonopoly(myID, history[name])}
                        </div>
                    )

                default:
                    return <h1 style={{ color: 'red' }} key={'Game ' + index}> Неизвестная игра </h1>
            }
        })

    if (isLoading) return <Loading />
    if (isError) return <h1> Произошла ошибка! </h1>

    if (games.length === 0) return <h1> У тебя еще нет истории игр. <br /> Самое время поиграть :) </h1>

    const gameNames: JSX.Element[] = []
    let i = 0
    GameNamesRu.forEach((displayName, codeName) => {
        gameNames.push((
            <option value={codeName} key={i++}>{displayName}</option>
        ))
    })

    return (
        <div className='history'>
            <h1>Твоя история игр</h1>
            <select value={gameName} onChange={x => {
                const value = (x.currentTarget.value) as Games
                if (value in Games) {
                    setGameName(value)
                }
            }}>
                {gameNames}
            </select>
            <br />
            {games}
        </div>
    )
}



async function fetchData() {
    return axios.get(ENDPOINTS.GET_HISTORY_URL)
        .then(response => response.data)
        .catch(e => console.log(e))
}