import { useEffect, useState } from "react"
import ENDPOINTS from "../../../utilities/Api_Endpoints"
import { useNavigate } from "react-router-dom"
import axios from "axios"
import './checkersLobby.css'

export default function CheckersLobby() {
    const [loading, setLoading] = useState(true)
    const navigate = useNavigate()

    // useEffect(() => {
    //     const tryRedirect = () => {
    //         axios.get(ENDPOINTS.GET_CHECKERS_GAME_URL)
    //             .then(response => {
    //                 if (response.status === 200)
    //                     navigate('/play/checkers')
    //             })
    //             .catch((e) => {
    //                 console.log(e)
    //             })
    //             .finally(() => setLoading(false))
    //     }

    //     setLoading(true)
    //     tryRedirect()
    // }, [navigate])

    return loading ? <div></div> :
        <div id="lobbyWrapper">
            <button>Создать игру</button>
            <button>Подключиться к другому игроку</button>
        </div>
}