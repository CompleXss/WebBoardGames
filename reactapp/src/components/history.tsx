import { Component, ReactNode } from "react";
import './css/history.css'

export class History extends Component {

    render(): ReactNode {
        console.log('history')
        return (
            <div className="container">
                <h1>Твоя история игр</h1>
                <br/>
                <table>
                    <thead>
                        <tr>
                            <td>Игра</td>
                            <td>Победа / Поражение</td>
                            <td>Время игры</td>
                            <td>Дата начала</td>
                        </tr>
                    </thead>

                    <tbody>
                        <tr>
                            <td>Шашки</td>
                            <td>Победа</td>
                            <td>9:10</td>
                            <td>17.12.1999</td>
                        </tr>

                        <tr>
                            <td>Шашки</td>
                            <td>Поражение</td>
                            <td>20:00</td>
                            <td>20.10.2018</td>
                        </tr>

                        <tr>
                            <td>Шашки</td>
                            <td>Поражение</td>
                            <td>20:00</td>
                            <td>20.10.2018</td>
                        </tr>

                        <tr>
                            <td>Шашки</td>
                            <td>Победа</td>
                            <td>20:00</td>
                            <td>20.10.2018</td>
                        </tr>
                    </tbody>
                </table>
            </div>
        )
    }
}