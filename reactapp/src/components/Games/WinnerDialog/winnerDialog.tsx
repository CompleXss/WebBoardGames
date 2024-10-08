import axios from 'axios'
import { useState, useRef } from 'react'
import { useNavigate } from 'react-router-dom'
import LoadingContent from 'src/components/LoadingContent/loadingContent'
import ENDPOINTS from 'src/utilities/Api_Endpoints'
import './winnerDialog.css'

const CLOSE_AFTER_SECONDS = 5

export function useWinnerDialog() {
    const navigate = useNavigate()
    const winnerBanner = useRef<HTMLDialogElement>(null)
    const returnButton = useRef<HTMLButtonElement>(null)
    const [loadingWinnerName, setLoadingWinnerName] = useState(true)
    const [winnerName, setWinnerName] = useState('?')
    const [closingIn, setClosingIn] = useState(CLOSE_AFTER_SECONDS)

    function showWinner(winnerID: string) {
        if (!winnerID) {
            navigate('/')
            return
        }

        if (!returnButton.current) {
            console.error('Не могу найти returnButton (winnerDialog)')
            return
        }

        if (!winnerBanner.current) {
            console.error('Не могу найти winnerBanner (winnerDialog)')
            return
        }

        winnerBanner.current.showModal()

        let counter = CLOSE_AFTER_SECONDS
        const timer = setInterval(() => {
            counter--
            setClosingIn(counter)

            if (counter === 0) {
                clearInterval(timer)
                navigate('/')
            }
        }, 1000)

        returnButton.current.onclick = () => {
            clearInterval(timer)
            navigate('/')
        }
        getWinnerName(winnerID)
    }

    function getWinnerName(winnerID: string) {
        setLoadingWinnerName(true)
        axios.get(ENDPOINTS.Users.GET_USER_INFO_BY_ID_URL + winnerID)
            .then(response => {
                setWinnerName(response.data.name)
                setLoadingWinnerName(false)
            })
            .catch(e => console.log(e))
    }



    return {
        showWinner,
        element: (
            <dialog ref={winnerBanner} className='winnerDialog' onClose={() => navigate('/')}>
                <h1>Победитель</h1>
                <LoadingContent loading={loadingWinnerName} content={
                    <p className='winnerName'>{winnerName}</p>
                } />
                <button ref={returnButton}>На главную</button>
                <p>Игра закроется через {closingIn}...</p>
            </dialog>
        )
    }
}