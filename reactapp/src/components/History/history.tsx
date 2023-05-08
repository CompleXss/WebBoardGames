import axios from 'axios'
import { useQuery } from 'react-query'
import { GameHistory } from './gameHistoryTypes'
import { mapCheckers } from './gameHistoryMappers'
import ENDPOINTS from '../../utilities/Api_Endpoints'
import './history.css'

export default function History() {
    const { data, isLoading, error: err } = useQuery('history', fetchData)
    const history = data as GameHistory
    const error = (err as any)?.message

    let games = !history ? [] : Object.keys(history).map((Name, gameIndex) => {
        switch (Name) {
            case 'checkers':
                return <div className='table_wrapper' key={'Game ' + gameIndex}>
                    {mapCheckers(history[Name])}
                </div>;

            default:
                return <h1 style={{ color: 'red' }} key={gameIndex}>Unknown game</h1>
        }
    })

    return (
        <div>
            <h1>Твоя история игр</h1>
            <br />
            {isLoading && <h1>Loading... Please wait.</h1>}
            {error && <h1>Error: {error}</h1>}
            {!isLoading && !error &&
                <div>
                    {games.length > 0
                        ? games
                        : <h1> Your History is empty. <br /> Go play some games :) </h1>
                    }
                </div>
            }
        </div>
    )
}



async function fetchData() {
    return axios.get(ENDPOINTS.GET_HISTORY_URL)
        .then(response => response.data)
}