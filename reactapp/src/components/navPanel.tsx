import { NavLink } from "react-router-dom";
import './css/navPanel.css'

export default function NavPanel() {
    return (
        <nav>
            <NavLink to={'/'}> Home </NavLink>
            <NavLink to={'/history'}> History </NavLink>
            <NavLink to={'/leaderboard'}> Leaderboard </NavLink>
            <NavLink to={'/about'}> About </NavLink>
        </nav>
    )
}