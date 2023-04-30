import { useEffect, useState } from 'react'
import './history.css'
import { getDatesDiff_string } from '../../DateHelper'
import { CheckersData } from './historyGameTypes'

interface GameHistory {
    [Name: string]: CheckersData[] // | OtherGameData[]
}

export default function History() {

    const [loading, setLoading] = useState(false)
    const [history, setHistory] = useState<GameHistory>({})

    useEffect(() => {
        const dataFetch = async () => {
            const data = await (
                await fetch("http://localhost:5042/history/33")
            ).json()

            setLoading(false)
            setHistory(data)
        }

        setLoading(true)
        dataFetch()
    }, [])

    let games: JSX.Element[] = [];
    Object.keys(history).forEach(Name =>
        games.push(...history[Name].map(data =>
            <tr>
                <td>{Name === 'checkers' ? 'Шашки' : Name}</td>
                <td style={{ background: data.isWin ? 'var(--win-color)' : 'var(--loose-color)' }}>
                    {data.isWin ? 'Победа' : 'Поражение'}
                </td>
                <td>{getDatesDiff_string(data.dateTimeStart, data.dateTimeEnd)}</td>
                <td>{data.dateTimeStart.toLocaleString()}</td>
            </tr>
        ))
    )



    return (
        <div className="container">
            <h1>Твоя история игр</h1>
            <h1>{loading ? 'Гружу' : 'Отдыхаю'}</h1>
            <br />
            {games.length > 0 ?
                <table>
                    <thead>
                        <tr>
                            <td>Игра</td>
                            <td>Победа / Поражение</td>
                            <td>Время игры</td>
                            <td>Дата начала</td>
                        </tr>
                    </thead>

                    <tbody>
                        {/* <tr>
                        <td>Шашки</td>
                        <td>Победа</td>
                        <td>9:10</td>
                        <td>17.12.1999 00:00</td>
                    </tr>

                    <tr>
                        <td>Шашки</td>
                        <td>Поражение</td>
                        <td>20:00</td>
                        <td>20.10.2018 00:00</td>
                    </tr>

                    <tr>
                        <td>Шашки</td>
                        <td>Поражение</td>
                        <td>20:00</td>
                        <td>20.10.2018 00:00</td>
                    </tr>

                    <tr>
                        <td>Шашки</td>
                        <td>Победа</td>
                        <td>20:00</td>
                        <td>20.10.2018 00:00</td>
                    </tr> */}
                    </tbody>
                </table>
                : <h1> No data</h1>
            }
        </div >
    )
}
