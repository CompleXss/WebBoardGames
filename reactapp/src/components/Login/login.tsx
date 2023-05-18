import axios from 'axios'
import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import ENDPOINTS from '../../utilities/Api_Endpoints'
import './login.css'

// TODO: add request timeout

export default function Login() {
    const [switchModeText, setswitchModeText] = useState('Нет аккаунта? Регистрируйся')
    const [isLogin, setIsLogin] = useState(true)
    const navigate = useNavigate()
    
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

    function login(login: string, password: string) {
        console.log('trying to login ' + login)

        axios.post(ENDPOINTS.Auth.POST_LOGIN_URL, {
            name: login,
            password: password,
        }).then(response => {
            if (response.status === 200) {
                console.log(login + ' logged in')
                showWarningText('Вход успешен!', 'green')
                setTimeout(() => navigate('/'), 1000)
            }
        }).catch(e => {
            console.log(e)
            showWarningText(e?.response?.data)
        })
    }

    function register(login: string, password: string) {
        console.log('trying to register ' + login)

        axios.post(ENDPOINTS.Auth.POST_REGISTER_URL, {
            name: login,
            password: password,
        }).then(response => {
            if (response.status === 201) {
                console.log(login + ' registered')
                showWarningText('Регистрация успешна!', 'green')
                setTimeout(() => navigate('/'), 1000)
            }
        }).catch(e => {
            console.log(e)
            showWarningText(e?.response?.data)
        })
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
                <p id='loginWarningText'>Введите имя и пароль</p>
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
        showWarningText('Введите имя и пароль')
        return
    }
    else showWarningText(false)

    func(login, password);
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

function showWarningText(show: string | false, color?: string) {
    let text = document.getElementById('loginWarningText')
    if (!text) {
        console.log('Не могу найти элемент loginWarningText')
        return
    }

    if (show) {
        text.textContent = show
    }
    text.style.opacity = show ? '1' : '0';
    text.style.color = color ?? 'red'
}