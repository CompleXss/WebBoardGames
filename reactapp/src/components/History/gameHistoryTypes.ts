export interface GameHistory {
    [Name: string]: CheckersData[] // | OtherGameData[]
}

export interface CheckersData {
    isWin: number,
    enemyName: string,
    dateTimeStart: Date,
    dateTimeEnd: Date
}
