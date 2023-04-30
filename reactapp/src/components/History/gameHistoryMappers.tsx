import { CheckersData } from "./gameHistoryTypes";
import { getDatesDiff_string } from "../../DateHelper";

export function mapCheckers(history: CheckersData[]) {
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
            {history.map((data, index) =>
                <tr key={index}>
                    <td>{data.enemyName}</td>
                    <td style={{ background: data.isWin ? 'var(--win-color)' : 'var(--loose-color)' }}>
                        {data.isWin ? 'Победа' : 'Поражение'}
                    </td>
                    <td>{getDatesDiff_string(data.dateTimeStart, data.dateTimeEnd)}</td>
                    <td>{data.dateTimeStart.toLocaleString()}</td>
                </tr>
            )}
        </tbody>
    </table>
}
