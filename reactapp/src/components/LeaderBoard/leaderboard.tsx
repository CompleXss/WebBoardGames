import { useEffect } from 'react'
import './leaderboard.css'

export default function Leaderboard() {
    useEffect(() => {
        document.title = 'Таблица рекордов'
    }, [])

    return (
        <h1>
            Leaderboard text here
        </h1>
    )
}