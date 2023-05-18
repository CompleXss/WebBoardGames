import axios from 'axios'
import { useQuery } from 'react-query'
import { GameHistory } from './gameHistoryTypes'
import { mapCheckers } from './gameHistoryMappers'
import Loading from '../Loading/loading'
import ENDPOINTS from '../../utilities/Api_Endpoints'
import './history.css'

export default function History() {
    const { data, isLoading, isError } = useQuery('history', fetchData)
    const history = data as GameHistory

    const games = !history ? [] : Object.keys(history)
        .filter(name => history[name].length !== 0).map((Name, gameIndex) => {
            switch (Name) {
                case 'checkers':
                    return <div className='table_wrapper' key={'Game ' + gameIndex}>
                        {mapCheckers(history[Name])}
                    </div>

                default:
                    return <h1 style={{ color: 'red' }} key={'Game ' + gameIndex}>Unknown game</h1>
            }
        })

    if (isLoading) return <Loading />
    if (isError) return <h1>Произошла ошибка!</h1>

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
}