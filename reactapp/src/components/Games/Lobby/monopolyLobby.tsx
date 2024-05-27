import { useEffect } from "react"
import { GameNames } from "src/utilities/GameNames"
import { useLobby } from "./useLobby"

export default function MonopolyLobby() {
    const { element } = useLobby(GameNames.monopoly, 'Лобби (Монополия)', '/images/monopoly-bg.jpg')

    useEffect(() => {
        document.title = 'Монополия (лобби)'
    }, [])

    return element
}
