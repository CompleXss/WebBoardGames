import { useEffect } from 'react'
import './about.css'

export default function About() {
    useEffect(() => {
        document.title = 'О создателе'
    }, [])

    return (
        <h1>
            About page text here!
        </h1>
    )
}