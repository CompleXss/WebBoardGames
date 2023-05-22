import { useEffect } from 'react'
import { NavLink } from 'react-router-dom'
import './home.css'

export default function Home() {
    useEffect(() => {
        document.title = 'Главная'
    }, [])

    return <div className="home">
        <h1>Во что сыграем сегодня?</h1>

        <div className="gamesContainer">
            <NavLink className="game checkers" to={'/lobby/checkers'}>
                <p>Шашки</p>
            </NavLink>

            <NavLink className="game chess" to={'/'}>
                <p>Шахматы</p>
            </NavLink>

            <NavLink className="game monopoly" to={'/'}>
                <p>Монополия</p>
            </NavLink>
        </div>
    </div>
}