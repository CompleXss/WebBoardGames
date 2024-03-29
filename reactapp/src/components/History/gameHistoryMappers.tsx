import { CheckersHistoryData } from "../../utilities/Api_DataTypes";
import { getDatesDiff_string } from "../../utilities/DateHelper";

export function mapCheckers(myID: string, history: CheckersHistoryData[]) {
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
                const winner = data.winners[0]
                const looser = data.loosers[0]

                const isWin = myID === winner.publicID
                const enemyName = isWin ? looser.name : winner.name

                return <tr key={index}>
                    <td>{enemyName}</td>
                    <td style={{ background: isWin ? 'var(--win-color)' : 'var(--loose-color)' }}>
                        {isWin ? 'Победа' : 'Поражение'}
                    </td>
                    <td>{getDatesDiff_string(data.dateTimeStart, data.dateTimeEnd)}</td>
                    <td>{new Date(data.dateTimeStart).toLocaleString()}</td>
                </tr>
            })}
        </tbody>
    </table>
}