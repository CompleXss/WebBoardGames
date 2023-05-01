import { NavLink } from 'react-router-dom'
import './home.css'

export default function Home() {
    return (
        <div className="home">
            <h1>Во что сыграем сегодня?</h1>

            <div className="gamesContainer">
                <NavLink className="game checkers" to={'/'}>
                    <p>Шашки</p>
                </NavLink>

                <NavLink className="game chess" to={'/'}>
                    <p>Шахматы</p>
                </NavLink>

                <NavLink className="game monopoly" to={'/'}>
                    <p>Монополия</p>
                </NavLink>
            </div>

            <br />
            <h1>Статистика?</h1>

            <p>11111111111</p>
            <p>22222222222</p>
            <p>33333333333</p>
            <p>44444444444</p>
            <p>55555555555</p>
            <p>66666666666</p>
            <p>77777777775</p>
            <p>asdadasdasd</p>
            <p>asdadasdasd</p>
            <p>asdadasdasd</p>
            <p>asdadasdasd</p>
            <p>asdadasdasd</p>
            <p>asdadasdasd</p>
            <p>asdadasdasd</p>
            <p>last</p>
        </div>
    )
}
