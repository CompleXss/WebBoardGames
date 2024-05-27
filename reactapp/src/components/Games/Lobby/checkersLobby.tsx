import { useEffect } from "react"
import { GameNames } from "src/utilities/GameNames"
import { useLobby } from "./useLobby"

export default function CheckersLobby() {
    const { element } = useLobby(GameNames.checkers, 'Лобби (Шашки)', '/images/checkers-bg.jpg')

    useEffect(() => {
        document.title = 'Шашки (лобби)'
    }, [])

    return element
}
