import axios from 'axios'
import React, { useEffect, useRef, useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { REDIRECT_QUERY_PARAM_NAME } from '../RequireComponents/RequireAuth'
import ENDPOINTS from '../../utilities/Api_Endpoints'
import './login.css'

// TODO: add request timeout

const MIN_LOGIN_LENGTH = 3
const MIN_PASSWORD_LENGTH = 3

export default function Login() {
    const loginBtn = useRef<HTMLButtonElement>(null)
    const registerBtn = useRef<HTMLButtonElement>(null)
    const loginHeader = useRef<HTMLHeadingElement>(null)
    const registerHeader = useRef<HTMLHeadingElement>(null)
    const loginWarningText = useRef<HTMLParagraphElement>(null)

    const loginInput = useRef<HTMLInputElement>(null)
    const nameInput = useRef<HTMLInputElement>(null)
    const passwordInput = useRef<HTMLInputElement>(null)
    const repeatPasswordInput = useRef<HTMLInputElement>(null)

    const [switchModeText, setSwitchModeText] = useState('Нет аккаунта? Регистрируйся')
    const [isLoginMode, setIsLoginMode] = useState(true)
    const navigate = useNavigate()

    const [searchParams] = useSearchParams()
    const redirectAfterLogin = searchParams.get(REDIRECT_QUERY_PARAM_NAME) ?? '/'

    useEffect(() => {
        document.title = 'Авторизация'
    }, [])

    function switchMode() {
        if (!loginBtn.current || !registerBtn.current || !loginHeader.current || !registerHeader.current || !nameInput.current || !repeatPasswordInput.current) {
            console.error('Не найдены нужные элементы на странице логина!')
            return
        }
        showWarningText(false)

        showBtn(loginBtn.current, !isLoginMode)
        showBtn(registerBtn.current, isLoginMode)
        showHeader(loginHeader.current, !isLoginMode)
        showHeader(registerHeader.current, isLoginMode)
        showInput(nameInput.current, isLoginMode)
        showInput(repeatPasswordInput.current, isLoginMode)
        setSwitchModeText(isLoginMode ? 'Всё-таки есть аккаунт?' : 'Нет аккаунта? Регистрируйся')

        setIsLoginMode(!isLoginMode)
    }

    function getErrorMessage(e: any) {
        return (e?.response?.data?.message || e?.message || 'Server Error')
    }

    function login(login: string, password: string) {
        console.log('trying to login as: ' + login)

        axios.post(ENDPOINTS.Auth.POST_LOGIN_URL, {
            login: login,
            password: password,
        }).then(() => {
            console.log('logged in as: ' + login)
            showWarningText('Вход успешен!', 'green')
            setTimeout(() => navigate(redirectAfterLogin), 1000)
        }).catch(e => {
            console.log(e?.response?.data || e?.message || e)
            showWarningText(getErrorMessage(e))
        })
    }

    function register(login: string, name: string, password: string) {
        console.log('trying to register ' + login)

        axios.post(ENDPOINTS.Auth.POST_REGISTER_URL, {
            login: login,
            name: name,
            password: password,
        }).then(() => {
            console.log('registration complete for: ' + login)
            showWarningText('Регистрация успешна!', 'green')
            setTimeout(() => navigate(redirectAfterLogin), 1000)
        }).catch(e => {
            console.log(e?.response?.data || e?.message || e)
            showWarningText(getErrorMessage(e))
        })
    }

    function loginInputOnKeyDown(e: React.KeyboardEvent<HTMLInputElement>) {
        if (e.key !== 'Enter') return
        nameInput.current?.focus()
    }

    function nameInputOnKeyDown(e: React.KeyboardEvent<HTMLInputElement>) {
        if (e.key !== 'Enter') return
        passwordInput.current?.focus()
    }

    function passInputOnKeyDown(e: React.KeyboardEvent<HTMLInputElement>) {
        if (e.key !== 'Enter') return

        if (isLoginMode) getInputAndLogin()
        else getInputAndRegister()
    }



    function getInputAndLogin() {
        if (!loginInput.current || !passwordInput.current) return

        const loginValue = loginInput.current.value.trim()
        const passwordValue = passwordInput.current.value

        if (isLoginInputValid(loginValue, passwordValue)) {
            login(loginValue, passwordValue)
        }
    }

    function getInputAndRegister() {
        if (!loginInput.current || !passwordInput.current || !nameInput.current || !repeatPasswordInput.current) return

        const loginValue = loginInput.current.value.trim()
        const nameValue = nameInput.current.value.trim()
        const passwordValue = passwordInput.current.value
        const repeatPasswordValue = repeatPasswordInput.current.value

        if (isRegisterInputValid(loginValue, nameValue, passwordValue, repeatPasswordValue)) {
            register(loginValue, nameValue, passwordValue)
        }
    }

    function isLoginInputValid(login: string, password: string): boolean {
        if (!login || !password || login === '' || password === '') {
            showWarningText('Заполните все поля')
            return false
        }

        if (login.length < MIN_LOGIN_LENGTH) {
            showWarningText('Длина логина должна быть больше ' + (MIN_LOGIN_LENGTH - 1))
            return false
        }

        if (login.includes(' ')) {
            showWarningText('Логин не должен содержать пробелов')
            return false
        }

        if (password.length < MIN_PASSWORD_LENGTH) {
            showWarningText('Длина пароля должна быть больше ' + (MIN_PASSWORD_LENGTH - 1))
            return false
        }

        showWarningText(false)
        return true
    }

    function isRegisterInputValid(login: string, name: string, password: string, repeatPassword: string): boolean {
        if (!isLoginInputValid(login, password)) {
            return false
        }

        if (!name || !password || !repeatPassword || name === '' || password === '' || repeatPassword === '') {
            showWarningText('Заполните все поля')
            return false
        }

        if (password !== repeatPassword) {
            showWarningText('Пароли не совпадают')
            return false
        }

        showWarningText(false)
        return true
    }

    function showWarningText(show: string | false, color?: string) {
        if (!loginWarningText.current) {
            console.log('Не могу найти элемент loginWarningText')
            return
        }

        if (show) {
            loginWarningText.current.textContent = show
        }
        loginWarningText.current.style.opacity = show ? '1' : '0';
        loginWarningText.current.style.color = color ?? 'red'
    }



    return <div id='loginContainer'>
        <div id='loginWrapper'>
            <div id='loginHeaderWrapper'>
                <h1 id='dummyHeader'>dummy</h1>
                <h1 id='loginHeader' ref={loginHeader}>Авторизация</h1>
                <h1 id='registerHeader' ref={registerHeader}>Регистрация</h1>
            </div>
            <div className='inputContainer'>

                <input ref={loginInput} name='login' type='text' placeholder='Логин' maxLength={32} onKeyDown={loginInputOnKeyDown} />
                <div className='expandable'>
                    <input ref={nameInput} name='name' type='text' placeholder='Отображаемое имя' maxLength={32} onKeyDown={nameInputOnKeyDown} />
                </div>
                <input ref={passwordInput} name='password' type='password' placeholder='Пароль' maxLength={64} onKeyDown={passInputOnKeyDown} />
                <div className='expandable'>
                    <input ref={repeatPasswordInput} name='password' type='password' placeholder='Повторите пароль' maxLength={64} onKeyDown={passInputOnKeyDown} />
                </div>
                <p id='loginWarningText' ref={loginWarningText}>Введите имя и пароль</p>

                <div className='btnContainer'>
                    <button id='dummyBtn'>dummy</button>
                    <button id='loginBtn' ref={loginBtn} onClick={getInputAndLogin}>Войти</button>
                    <button id='registerBtn' ref={registerBtn} onClick={getInputAndRegister}>Зарегистрироваться</button>
                </div>
            </div>

            <p id='loginSwitchMode' onClick={switchMode}>{switchModeText}</p>
        </div>
    </div>
}



function showBtn(button: HTMLElement, show: boolean) {
    button.style.opacity = show ? '1' : '0'
    // button.style.marginRight = show ? '16px' : '60%'
    button.style.zIndex = show ? '0' : '-1'
}

function showHeader(header: HTMLElement, show: boolean) {
    header.style.opacity = show ? '1' : '0'
    // header.style.marginRight = show ? '0' : '30%'
    header.style.zIndex = show ? '0' : '-1'
}

function showInput(input: HTMLElement, show: boolean) {
    if (!input.parentElement || !input.parentElement.className.includes('expandable')) return

    input.parentElement.className = show ? 'expandable open' : 'expandable'
    input.parentElement.style.opacity = show ? '1' : '0'
}
