import { useEffect, useState } from "react"
import ENDPOINTS from "../../../utilities/Api_Endpoints"
import { useNavigate } from "react-router-dom"

export default function CheckersLobby() {
    const [loading, setLoading] = useState(true)
    const navigate = useNavigate()

    useEffect(() => {
        const tryRedirect = () => {
            fetch(ENDPOINTS.GET_CHECKERS_GAME_URL)
                .then(response => {
                    if (response.ok)
                        navigate('/play/checkers')
                })
                .catch((e) => {
                    console.log(e)
                })
                .finally(() => setLoading(false))
        }

        setLoading(true)
        tryRedirect()
    }, [navigate])

    return loading ? <div></div> :
        <div>Hay</div>
}