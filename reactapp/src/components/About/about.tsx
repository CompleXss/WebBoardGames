import { useEffect } from 'react'
import './about.css'

export default function About() {
    useEffect(() => {
        document.title = 'О создателе'
    }, [])

    return <div className='aboutContainer'>
        <h1> Этот сайт — курсовая работа студента 3 курса под ником </h1>
        <h1 className='nick'>CompleX</h1>
        <hr />
        <a href='https://github.com/CompleXss'>Github</a>
        <a href='https://github.com/CompleXss/WebBoardGames'>Этот сайт на том же Github</a>
    </div>
}