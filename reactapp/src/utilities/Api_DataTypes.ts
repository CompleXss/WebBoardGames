export enum Games {
    checkers = 'checkers',
    monopoly = 'monopoly',
}

export const GameNamesRu = new Map<Games, string>([
    [Games.checkers, 'Шашки'],
    [Games.monopoly, 'Монополия']
])



export type User = {
    publicID: string
    name: string
}

export type UserGameStats = {
    user: User
    winCount: number
    playCount: number
}

export type LeaderboardData = {
    [gameName: string]: UserGameStats[]
}

export type GameHistory = {
    [gameName: string]: GameHistoryData[]
}

export type GameHistoryData = {
    winners: User[]
    loosers: User[]
    dateTimeStart: Date
    dateTimeEnd: Date
}
