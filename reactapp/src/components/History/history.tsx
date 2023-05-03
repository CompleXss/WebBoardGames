import { useEffect, useState } from 'react'
import { GameHistory } from './gameHistoryTypes'
import { mapCheckers } from './gameHistoryMappers'
import ENDPOINTS from '../../utilities/Api_Endpoints'
import './history.css'

export default function History() {

    const [history, setHistory] = useState<GameHistory>({})
    const [loading, setLoading] = useState(false)
    const [error, setError] = useState(null)

    useEffect(() => {
        const fetchData = () => {
            fetch(ENDPOINTS.GET_HISTORY_URL)
                .then(response => response.json())
                .then(data => {
                    setHistory(data)
                })
                .catch((err) => {
                    setHistory({})
                    setError(err?.message)
                    console.log(err)
                })
                .finally(() => setLoading(false))
        }

        setLoading(true)
        setError(null)
        fetchData()
    }, [])

    let games = Object.keys(history).map((Name, gameIndex) => {
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
            {loading && <h1>Loading... Please wait.</h1>}
            {error && <h1>Error: {error}</h1>}
            {!loading && !error &&
                <div>
                    {games.length > 0
                        ? games
                        : <h1> Your History is empty. <br /> Go play some games :) </h1>
                    }
                </div>
            }
        </div >
    )
}
