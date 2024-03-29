import axios from 'axios'
import { useEffect } from 'react'
import { useQuery } from 'react-query'
import { Games, LeaderboardData } from '../../utilities/Api_DataTypes'
import { mapCheckers } from './leaderboardMappers'
import ENDPOINTS from '../../utilities/Api_Endpoints'
import Loading from '../Loading/loading'

export default function Leaderboard() {
    const { data, isLoading, isError } = useQuery('leaderboard', fetchData)
    const leaderboard = data as LeaderboardData

    useEffect(() => {
        document.title = 'Таблицы лидеров'
    }, [])

    const games = !leaderboard ? [] : Object.keys(leaderboard)
        .filter(name => leaderboard[name].length !== 0).map((name, index) => {
            switch (name) {
                case Games.checkers:
                    return <div className='table_wrapper' key={'Game ' + index}>
                        {mapCheckers(leaderboard[name])}
                    </div>

                default:
                    return <h1 style={{ color: 'red' }} key={'Game ' + index}> Неизвестная игра </h1>
            }
        })

    if (isLoading) return <Loading />
    if (isError) return <h1> Произошла ошибка! </h1>

    if (games.length === 0) return <h1> Лидерборда ещё нет. </h1>

    return (
        <div>
            <h1>Список лидеров</h1>
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
