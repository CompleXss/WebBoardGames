import { useEffect, useState } from 'react';
import ENDPOINTS from '../../../utilities/Api_Endpoints';
import './checkersGame.css'
import { useNavigate } from 'react-router-dom';

interface Point {
    x: number,
    y: number,
}

interface GameData {
    allyPositions: Point[],
    enemyPositions: Point[],
    lastMove: { from: Point, to: Point }[],
}

export default function CheckersGame() {
    const [loading, setLoading] = useState(false) // set to true
    const [gameData, setGameData] = useState<GameData | null>(null)
    const navigate = useNavigate()

    useEffect(() => {
        const fetchData = () => {
            fetch(ENDPOINTS.GET_CHECKERS_GAME_URL)
                .then(response => response.ok ? response.json() : null)
                .then(json => {
                    if (json === null) {
                        navigate('/lobby/checkers')
                        return
                    }
                    let data = json as GameData;
                    setGameData(data)
                })
                .catch((err) => {
                    console.log(err)
                })
                .finally(() => setLoading(false))
        }

        console.log(1)
        setLoading(true)
        fetchData()
    }, [navigate])



    return loading ? <div></div> :
        <div className="checkersContainer">
            <div className='enemyZone'>BDAFHDGADGNAGDAD</div>
            <div className='boardContainer'>
                <div className="board">
                    {spawnCells()}
                </div>
            </div>
            <div className='myZone'>BDAFHDGADGNAGDAD</div>
        </div>
}

function spawnCells() {
    const cells: JSX.Element[][] = new Array(8);

    for (let i = 0; i < 8; i++) {
        cells[i] = new Array(8);

        for (let j = 0; j < 8; j++) {
            let className = (i & 1) === (j & 1)
                ? 'cell white'
                : 'cell black'

            let id = `${i}${j}`;
            cells[i][j] = <div id={id} key={id} className={className}></div>
        }
    }

    return cells;
}