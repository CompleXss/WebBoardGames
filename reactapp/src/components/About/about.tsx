import { useEffect } from 'react'
import './about.css'

export default function About() {
    useEffect(() => {
        document.title = 'О создателе'
    }, [])

    return <div className='aboutContainer'>
        <h1> Это приложение — дипломная работа студента 4 курса под ником </h1>
        <h1 className='nick'>CompleX</h1>
        <hr />
        <a href='https://github.com/CompleXss'>Мой Github</a>
        <a href='https://github.com/CompleXss/WebBoardGames'>Репозиторий этого приложения на Github</a>
    </div>
}