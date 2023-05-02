import './checkersGame.css'

export default function CheckersGame() {
    const cells: JSX.Element[][] = [];

    for (let i = 0; i < 8; i++) {
        cells[i] = [];

        for (let j = 0; j < 8; j++) {
            let className = (i & 1) === (j & 1)
                ? 'cell white'
                : 'cell black'

            cells[i][j] = <div id={`${i}${j}`} className={className}></div>
        }
    }

    return <div className="checkersContainer">
        <div className='enemyZone'>BDAFHDGADGNAGDAD</div>
        <div className='boardContainer'>
            <div className="board">
                {cells}
            </div>
        </div>
        <div className='myZone'>BDAFHDGADGNAGDAD</div>
    </div>
}
