import { useState } from 'react'
import './login.css'

export default function Login() {
    let [switchModeText, setswitchModeText] = useState('Нет аккаунта? Регистрируйся')
    let [isLogin, setIsLogin] = useState(true)

    function switchMode() {
        let loginBtn = document.getElementById('loginBtn')
        let registerBtn = document.getElementById('registerBtn')
        let loginHeader = document.getElementById('loginHeader')
        let registerHeader = document.getElementById('registerHeader')

        if (!loginBtn || !registerBtn || !loginHeader || !registerHeader) {
            console.error('Не найдены нужные элементы на странице логина!')
            return
        }
        showWarningText(false)

        if (isLogin) {
            showBtn(loginBtn, false)
            showBtn(registerBtn, true)

            showHeader(loginHeader, false)
            showHeader(registerHeader, true)

            setswitchModeText('Всё-таки есть аккаунт?')
        }
        else {
            showBtn(loginBtn, true)
            showBtn(registerBtn, false)

            showHeader(loginHeader, true)
            showHeader(registerHeader, false)

            setswitchModeText('Нет аккаунта? Регистрируйся')
        }

        setIsLogin(!isLogin)
    }

    return <div id='loginContainer'>
        <div id='loginWrapper'>
            <div id='loginHeaderWrapper'>
                <h1 id='dummyHeader'>dummy</h1>
                <h1 id='loginHeader'>Авторизация</h1>
                <h1 id='registerHeader'>Регистрация</h1>
            </div>
            <div className='inputContainer'>
                <input id='loginName' name='name' type='text' placeholder='Имя пользователя' maxLength={32} />
                <input id='loginPassword' name='password' type='password' placeholder='Пароль' maxLength={64} />
                <p id='loginWarningText' hidden>Введите имя и пароль</p>
                <div className='btnContainer'>
                    <button id='dummyBtn'>dummy</button>
                    <button id='loginBtn' onClick={() => getInputAnd(login)}>Войти</button>
                    <button id='registerBtn' onClick={() => getInputAnd(register)}>Зарегистрироваться</button>
                </div>
            </div>

            <p id='loginSwitchMode' onClick={switchMode}>{switchModeText}</p>
        </div>
    </div>
}

function getInputAnd(func: Function) {
    let loginName = document.getElementById('loginName') as HTMLInputElement
    let loginPassword = document.getElementById('loginPassword') as HTMLInputElement
    if (loginName === null || loginPassword === null) return

    let login = loginName.value.trim()
    let password = loginPassword.value.trim()

    if (login === '' || password === '') {
        showWarningText(true)
        return
    }
    else showWarningText(false)

    func(login, password);
}

function login(login: string, password: string) {
    console.log('login ' + login)
}

function register(login: string, password: string) {
    console.log('register ' + login)
}



function showBtn(button: HTMLElement, show: boolean) {
    button.style.opacity = show ? '1' : '0'
    button.style.marginRight = show ? '16px' : '60%'
    button.style.zIndex = show ? '0' : '-1'
}

function showHeader(header: HTMLElement, show: boolean) {
    header.style.opacity = show ? '1' : '0'
    header.style.marginRight = show ? '0' : '30%'
    header.style.zIndex = show ? '0' : '-1'
}

function showWarningText(show: boolean) {
    let text = document.getElementById('loginWarningText')
    if (!text) {
        console.log('Не могу найти элемент loginWarningText')
        return
    }

    text.hidden = !show;
}