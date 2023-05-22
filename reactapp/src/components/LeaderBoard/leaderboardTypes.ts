import { User } from "../../utilities/Api_DataTypes"

export interface LeaderboardData {
    [Name: string]: CheckersLeaderboardData[] // || OtherType[]
}

export interface CheckersLeaderboardData {
    playCount: number
    winCount: number
    user: User
}