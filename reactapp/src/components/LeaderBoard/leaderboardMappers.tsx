import { UserGameStats } from "../../utilities/Api_DataTypes"

export function mapCheckers(leaderboard: UserGameStats[]) {
    if (leaderboard.length === 0) return null

    return <table>
        <caption>Шашки</caption>
        <thead>
            <tr>
                <td>Игрок</td>
                <td>Кол-во игр</td>
                <td>Кол-во побед</td>
                <td>Процент побед</td>
            </tr>
        </thead>

        <tbody>
            {leaderboard.map((data, index) =>
                <tr key={index}>
                    <td>{data.user.name}</td>
                    <td>{data.playCount}</td>
                    <td>{data.winCount}</td>
                    <td>{(data.winCount / data.playCount).toLocaleString(undefined, { style: 'percent', minimumFractionDigits: 0 })}</td>
                </tr>
            )}
        </tbody>
    </table>
}