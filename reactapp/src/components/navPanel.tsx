import { Component, ReactNode } from "react";
import { NavLink } from "react-router-dom";
import './css/navPanel.css'

export class NavPanel extends Component {
    
    render(): ReactNode {
        return (
            <nav>
                <NavLink to={'/'}> Home </NavLink>
                <NavLink to={'/history'}> History </NavLink>
                <NavLink to={'/leaderboard'}> Leaderboard </NavLink>
                <NavLink to={'/about'}> About </NavLink>
            </nav>
        )
    }
}