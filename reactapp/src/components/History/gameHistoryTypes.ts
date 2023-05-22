import { User } from "../../utilities/Api_DataTypes"

export interface GameHistory {
    [Name: string]: CheckersHistoryData[] // | OtherGameData[]
}

export interface CheckersHistoryData {
    winner: User,
    looser: User,
    dateTimeStart: Date,
    dateTimeEnd: Date
}
