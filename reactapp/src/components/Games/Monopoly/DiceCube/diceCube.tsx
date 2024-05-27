import { forwardRef } from 'react'
import './diceCube.css'

interface Props {
    hidden?: boolean
}

export const DiceCube = forwardRef<HTMLDivElement, Props>((props, ref) => {
    return (
        <div className='diceContainer'>
            <div ref={ref} hidden={props.hidden} className="dice">
                <div className="front">
                    <span className="dot dot1"></span>
                </div>
                <div className="back">
                    <span className="dot dot1"></span>
                    <span className="dot dot2"></span>
                </div>
                <div className="right">
                    <span className="dot dot1"></span>
                    <span className="dot dot2"></span>
                    <span className="dot dot3"></span>
                </div>
                <div className="left">
                    <span className="dot dot1"></span>
                    <span className="dot dot2"></span>
                    <span className="dot dot3"></span>
                    <span className="dot dot4"></span>
                </div>
                <div className="top">
                    <span className="dot dot1"></span>
                    <span className="dot dot2"></span>
                    <span className="dot dot3"></span>
                    <span className="dot dot4"></span>
                    <span className="dot dot5"></span>
                </div>
                <div className="bottom">
                    <span className="dot dot1"></span>
                    <span className="dot dot2"></span>
                    <span className="dot dot3"></span>
                    <span className="dot dot4"></span>
                    <span className="dot dot5"></span>
                    <span className="dot dot6"></span>
                </div>
            </div>
        </div>
    )
})