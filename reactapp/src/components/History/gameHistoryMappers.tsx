import { GameHistoryData } from "../../utilities/Api_DataTypes";
import { getDatesDiff_string } from "../../utilities/DateHelper";

export function mapCheckers(myID: string, history: GameHistoryData[]) {
    if (history.length === 0) return null

    return <table>
        <caption>Шашки</caption>
        <thead>
            <tr>
                <td style={{ width: '38%' }}>Противник</td>
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

export function mapMonopoly(myID: string, history: GameHistoryData[]) {
    if (history.length === 0) return null

    return <table>
        <caption>Монополия</caption>
        <thead>
            <tr>
                <td style={{ width: '38%' }}>Противники</td>
                <td>Победа / Поражение</td>
                <td>Время игры</td>
                <td>Дата начала</td>
            </tr>
        </thead>

        <tbody>
            {history.map((data, index) => {
                const winner = data.winners[0]

                const isWin = myID === winner.publicID
                const enemyNames = data.winners
                    .concat(data.loosers)
                    .filter(x => x.publicID !== myID)
                    .map(x => x.name)
                    .reduce((acc, x) => acc += ', ' + x)

                return <tr key={index}>
                    <td>{enemyNames}</td>
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
