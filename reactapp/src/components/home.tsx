import { Component, ReactNode } from "react";
import './css/home.css'

export class Home extends Component {

    render(): ReactNode {
        return (
            <div className="home">
                <h1>Some cool game text</h1>

                <div className="gamesContainer">
                    <div className="game">
                        <p>Some 1 game text</p>
                    </div>

                    <div className="game">
                        <p>Some 2 game text</p>
                    </div>

                    <div className="game">
                        <p>Some 3 game text</p>
                    </div>
                </div>

                <h1>Some text again</h1>

                <p>11111111111</p>
                <p>22222222222</p>
                <p>33333333333</p>
                <p>44444444444</p>
                <p>55555555555</p>
                <p>66666666666</p>
                <p>77777777775</p>
                <p>asdadasdasd</p>
                <p>asdadasdasd</p>
                <p>asdadasdasd</p>
                <p>asdadasdasd</p>
                <p>asdadasdasd</p>
                <p>asdadasdasd</p>
                <p>asdadasdasd</p>
                <p>last</p>
            </div>
        )
    }
}
