import axios from 'axios'
import { useEffect } from 'react'
import { useQuery } from 'react-query'
import { mapCheckers, mapMonopoly } from './gameHistoryMappers'
import { Games, GameHistory } from '../../utilities/Api_DataTypes'
import Loading from '../Loading/loading'
import ENDPOINTS from '../../utilities/Api_Endpoints'

export default function History() {
    const { data, isLoading, isError } = useQuery('history', fetchData)
    const d = data as { userID: string, history: GameHistory }
    const myID = d ? d.userID : null
    const history = d ? d.history : null

    useEffect(() => {
        document.title = 'История игр'
    }, [])

    const games = !history || !myID ? [] : Object.keys(history)
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

    return (
        <div>
            <h1>Твоя история игр</h1>
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