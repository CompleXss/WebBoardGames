import axios from 'axios'
import { useEffect, useState } from 'react'
import { NavLink } from 'react-router-dom'
import ENDPOINTS from '../../utilities/Api_Endpoints'
import './home.css'

const CHECKERS_DEFAULT_URL = '/play/checkers'
const CHESS_DEFAULT_URL = '/'
const MONOPOLY_DEFAULT_URL = '/'

interface ActiveGame {
    GameType: string,
    ID: string
}

export default function Home() {
    const [checkersUrl, setCheckersUrl] = useState('/')
    const [chessUrl, setChessUrl] = useState('/')
    const [monopolyUrl, setMonopolyUrl] = useState('/')

    // useEffect(() => {
    //     const fetchData = () => {
    //         axios.get(ENDPOINTS.GET_ACTIVE_GAME_URL)
    //             .then(response => response.status === 200 ? response.data : null)
    //             .then(json => {
    //                 setDefaultLinks()

    //                 let activeGame = json as ActiveGame
    //                 if (activeGame === null) { // response is not ok
    //                     return
    //                 }

    //                 switch (activeGame.GameType) {
    //                     case 'checkers':
    //                         setCheckersIDLink(activeGame.ID)
    //                         break;
    //                     case 'chess':
    //                         setChessIDLink(activeGame.ID)
    //                         break;
    //                     case 'monopoly':
    //                         setMonopolyIDLink(activeGame.ID)
    //                         break;

    //                     default:
    //                         break;
    //                 }
    //             })
    //             .catch((err) => {
    //                 setDefaultLinks()
    //                 console.log(err)
    //             })
    //     }

    //     setEmptyLinks()
    //     fetchData()
    // }, [])

    return (
        <div className="home">
            <h1>Во что сыграем сегодня?</h1>

            <div className="gamesContainer">
                <NavLink className="game checkers" to={'/lobby/checkers'}>
                    <p>Шашки</p>
                </NavLink>

                <NavLink className="game chess" to={chessUrl}>
                    <p>Шахматы</p>
                </NavLink>

                <NavLink className="game monopoly" to={monopolyUrl}>
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

    function setEmptyLinks() {
        setCheckersUrl('/')
        setChessUrl('/')
        setMonopolyUrl('/')
    }

    function setDefaultLinks() {
        setCheckersUrl(CHECKERS_DEFAULT_URL)
        setChessUrl(CHESS_DEFAULT_URL)
        setMonopolyUrl(MONOPOLY_DEFAULT_URL)
    }

    function setCheckersIDLink(gameID: string) {
        setCheckersUrl(CHECKERS_DEFAULT_URL + '/' + gameID)
    }
    function setChessIDLink(gameID: string) {
        setChessUrl(CHESS_DEFAULT_URL + '/' + gameID)
    }
    function setMonopolyIDLink(gameID: string) {
        setMonopolyUrl(MONOPOLY_DEFAULT_URL + '/' + gameID)
    }
}