import { CheckersLeaderboardData } from "./leaderboardTypes"

export function mapCheckers(leaderboard: CheckersLeaderboardData[]) {
    if (leaderboard.length === 0) return null

    return <table>
        <caption>Шашки</caption>
        <thead>
            <tr>
                <td>Игрок</td>
                <td>Кол-во игр</td>
                <td>Кол-во побед</td>
            </tr>
        </thead>

        <tbody>
            {leaderboard.map((data, index) =>
                <tr key={index}>
                    <td>{data.user.name}</td>
                    <td>{data.playCount}</td>
                    <td>{data.winCount}</td>
                </tr>
            )}
        </tbody>
    </table>
}