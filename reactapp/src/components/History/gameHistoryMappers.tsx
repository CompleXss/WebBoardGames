import { CheckersHistoryData } from "./gameHistoryTypes";
import { getDatesDiff_string } from "../../utilities/DateHelper";

export function mapCheckers(myID: number, history: CheckersHistoryData[]) {
    if (history.length === 0) return null

    return <table>
        <caption>Шашки</caption>
        <thead>
            <tr>
                <td>Противник</td>
                <td>Победа / Поражение</td>
                <td>Время игры</td>
                <td>Дата начала</td>
            </tr>
        </thead>

        <tbody>
            {history.map((data, index) => {
                const isWin = myID === data.winner.id
                const enemyName = isWin ? data.looser.name : data.winner.name

                return <tr key={index}>
                    <td>{enemyName}</td>
                    <td style={{ background: isWin ? 'var(--win-color)' : 'var(--loose-color)' }}>
                        {isWin ? 'Победа' : 'Поражение'}
                    </td>
                    <td>{getDatesDiff_string(data.dateTimeStart, data.dateTimeEnd)}</td>
                    <td>{data.dateTimeStart.toLocaleString()}</td>
                </tr>
            })}
        </tbody>
    </table>
}