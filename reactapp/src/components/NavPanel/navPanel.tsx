import { NavLink } from "react-router-dom";
import './navPanel.css'

export default function NavPanel() {
    function openBurger() {
        let nav = document.getElementById('nav');
        if (nav === null) return;

        nav.className = nav.className === 'closed'
            ? 'open'
            : 'closed'
    }

    function hideBurger() {
        let nav = document.getElementById('nav');
        if (nav === null) return;

        nav.className = 'closed'
    }

    return (
        <nav id="nav" className="closed">
            <NavLink to={'/'} onClick={hideBurger}> Home </NavLink>
            <NavLink to={'/history'} onClick={hideBurger}> History </NavLink>
            <NavLink to={'/leaderboard'} onClick={hideBurger}> Leaderboard </NavLink>
            <NavLink to={'/about'} onClick={hideBurger}> About </NavLink>
            <button className="burger" onClick={openBurger}></button>
        </nav>
    )
}