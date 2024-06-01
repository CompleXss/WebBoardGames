import axios from 'axios'
import { useEffect, useState } from 'react'
import { useQuery } from 'react-query'
import { GameNamesRu, Games, LeaderboardData } from '../../utilities/Api_DataTypes'
import { mapDefault } from './leaderboardMappers'
import ENDPOINTS from '../../utilities/Api_Endpoints'
import Loading from '../Loading/loading'
import './leaderboard.css'

export default function Leaderboard() {
    const [gameName, setGameName] = useState<Games | '*'>('*')
    const { data, isLoading, isError } = useQuery('leaderboard', fetchData)
    const leaderboard = data as LeaderboardData

    useEffect(() => {
        document.title = 'Таблицы лидеров'
    }, [])

    const games = !leaderboard ? [] : Object.keys(leaderboard)
        .filter(name => gameName === '*' || name === gameName)
        .filter(name => leaderboard[name].length !== 0).map((name, index) => {
            switch (name) {
                case Games.checkers:
                    return (
                        <div className='table_wrapper' key={'Game ' + index}>
                            {mapDefault(leaderboard[name], 'Шашки')}
                        </div>
                    )

                case Games.monopoly:
                    return (
                        <div className='table_wrapper' key={'Game ' + index}>
                            {mapDefault(leaderboard[name], 'Монополия')}
                        </div>
                    )

                default:
                    return <h1 style={{ color: 'red' }} key={'Game ' + index}> Неизвестная игра </h1>
            }
        })

    if (isLoading) return <Loading />
    if (isError) return <h1> Произошла ошибка! </h1>

    if (games.length === 0) return <h1> Лидерборда ещё нет. </h1>

    const gameNames: JSX.Element[] = []
    let i = 1
    GameNamesRu.forEach((displayName, codeName) => {
        gameNames.push((
            <option value={codeName} key={i++}>{displayName}</option>
        ))
    })

    return (
        <div className='leaderboard'>
            <h1>Список лидеров</h1>
            <select value={gameName} onChange={x => {
                const value = (x.currentTarget.value) as (Games | '*')
                if ((value in Games) || value === '*') {
                    setGameName(value)
                }
            }}>
                <option value={'*'} key={0}>== Все игры ==</option>
                {gameNames}
            </select>
            <br />
            {games}
        </div>
    )
}

async function fetchData() {
    return axios.get(ENDPOINTS.GET_LEADERBOARD)
        .then(response => response.data)
        .catch(e => console.log(e))
}
