import { useEffect, useState } from 'react'
import './css/history.css'

interface GameHistory {
    [Name: string]: CheckersData[]
}

interface CheckersData {
    isWin: number,
    dateTimeStart: Date,
    dateTimeEnd: Date
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

    const games = Object.keys(history).map(Name =>
        history[Name].map(data =>
            <tr>
                <td>{Name === 'checkers' ? 'Шашки' : Name}</td>
                <td>{data.isWin === 1 ? 'Победа' : 'Поражение'}</td>
                <td>{getDatesDiff_string(data.dateTimeStart, data.dateTimeEnd)}</td>
                <td>{data.dateTimeStart.toLocaleString()}</td>
            </tr>
        )
    )

    

    return (
        <div className="container">
            <h1>Твоя история игр</h1>
            <h1>{loading ? 'Гружу' : 'Отдыхаю'}</h1>
            <br />
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
                    {games}

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
        </div>
    )
}

function getDatesDiff_milliseconds(date1: Date, date2: Date): number {
    // wtf
    date1 = new Date(date1);
    date2 = new Date(date2);

    return date2.getTime() - date1.getTime();
}

function getDatesDiff_string(date1: Date, date2: Date): string {
    var mills = getDatesDiff_milliseconds(date1, date2);

    let days = 0;
    let hours = 0;
    let minutes = 0;
    let seconds = Math.round(mills / 1000)

    if (seconds > 60) {
        minutes = Math.round(seconds / 60);
        seconds -= minutes * 60;

        if (minutes > 60) {
            hours = Math.round(minutes / 60);
            minutes -= hours * 60;
    
            if (hours > 24) {
                days = Math.round(hours / 24);
                hours -= days * 24;
            }
        }
    }

    let daysPart = days > 0 ? days + ' дней, ' : ''
    let hoursPart = hours > 0 ? hours + ' часов, ' : ''
    let minutesPart = minutes > 0 ? minutes + ' мин, ' : ''
    let secondsPart = seconds > 0 ? seconds + ' сек, ' : ''

    let res = daysPart + hoursPart + minutesPart + secondsPart
    return res === '' ? mills + ' миллисекунд' : res.slice(0, -2)
}

// TODO: сделать красивое "дня, день, дней......."