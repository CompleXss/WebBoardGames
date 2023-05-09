import { NavLink } from "react-router-dom";
import AccountIcon from '../../SVGs/account'
import './navPanel.css'

// TODO: you can spam /profile button (server requests spam)

export default function NavPanel() {
    return (
        <nav id="nav" className="closed">
            <NavLink to={'/'} onClick={hideBurger}> Home </NavLink>
            <NavLink to={'/history'} onClick={hideBurger}> History </NavLink>
            <NavLink to={'/leaderboard'} onClick={hideBurger}> Leaderboard </NavLink>
            <NavLink to={'/about'} onClick={hideBurger}> About </NavLink>
            <NavLink id='profileLink' to={'/profile'}>
                <AccountIcon className='accountIcon' />
            </NavLink>
            <button className='burger' onClick={openBurger}></button>
        </nav>
    )
}



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