import { NavLink } from "react-router-dom";
import AccountIcon from '../../SVGs/account'
import './navPanel.css'

// TODO: can tab to hidden buttons in mobile mode

export default function NavPanel() {
    return (
        <nav id='nav'>
            <NavLink to={'/'} onClick={hideBurger}> Главная </NavLink>
            <NavLink to={'/history'} onClick={hideBurger}> История </NavLink>
            <NavLink to={'/leaderboard'} onClick={hideBurger}> Рекорды </NavLink>
            <NavLink to={'/about'} onClick={hideBurger}> О нас </NavLink>
            <NavLink to={'/profile'}>
                <AccountIcon className='accountIcon' />
            </NavLink>
            <button className='burger' onClick={toggleBurger}></button>
        </nav>
    )
}



function toggleBurger() {
    const nav = document.getElementById('nav')
    if (nav === null) return

    if (nav.classList.contains('open'))
        nav.classList.remove('open')
    else
        nav.classList.add('open')
}

function hideBurger() {
    const nav = document.getElementById('nav')
    if (nav === null) return

    nav.classList.remove('open')
}