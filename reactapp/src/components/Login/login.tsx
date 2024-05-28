import axios from 'axios'
import React, { useEffect, useRef, useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { REDIRECT_QUERY_PARAM_NAME } from '../RequireComponents/RequireAuth'
import { MIN_LOGIN_LENGTH, MIN_PASSWORD_LENGTH } from 'src/utilities/auth'
import ENDPOINTS from '../../utilities/Api_Endpoints'
import './login.css'

// TODO: add request timeout
// TODO: disable button during request

export default function Login() {
    const loginBtn = useRef<HTMLButtonElement>(null)
    const registerBtn = useRef<HTMLButtonElement>(null)
    const loginHeader = useRef<HTMLHeadingElement>(null)
    const registerHeader = useRef<HTMLHeadingElement>(null)
    const loginWarningText = useRef<HTMLParagraphElement>(null)

    const loginInput = useRef<HTMLInputElement>(null)
    const nameInput = useRef<HTMLInputElement>(null)

    // IMPORTANT: passwordInput and repeatPasswordInput elements don't use refs
    // because React can't update them properly when changing autoComplete option

    const [switchModeText, setSwitchModeText] = useState('Нет аккаунта? Регистрируйся')
    const [isLoginMode, setIsLoginMode] = useState(true)
    const navigate = useNavigate()

    const [searchParams] = useSearchParams()
    const redirectAfterLogin = searchParams.get(REDIRECT_QUERY_PARAM_NAME) ?? '/'

    useEffect(() => {
        document.title = 'Авторизация'
    }, [])

    useEffect(() => {
        const passElement = getPasswordInputElement()
        const repeatPassElement = getRepeatPasswordInputElement()

        if (!loginBtn.current || !registerBtn.current || !loginHeader.current || !registerHeader.current || !nameInput.current || !passElement || !repeatPassElement) {
            console.error('Не найдены нужные элементы на странице логина!')
            return
        }
        showWarningText(false)

        showBtn(loginBtn.current, isLoginMode)
        showBtn(registerBtn.current, !isLoginMode)
        showHeader(loginHeader.current, isLoginMode)
        showHeader(registerHeader.current, !isLoginMode)
        showInput(nameInput.current, !isLoginMode)
        showInput(repeatPassElement, !isLoginMode)
        setSwitchModeText(isLoginMode ? 'Нет аккаунта? Регистрируйся' : 'Всё-таки есть аккаунт?')


        // change password inputs' autoComplete option
        passElement.autocomplete = isLoginMode ? 'current-password' : 'new-password'
        const newElement = passElement.cloneNode(false) as HTMLInputElement // create new element because browser keeps using old value

        const passOnKeyDown = executeOnEnterPress(focusElementAfterPasswordInput)
        newElement.onkeydown = passOnKeyDown as any // bruh

        passElement.replaceWith(newElement)

        // prevent repeatPassElement.autocomplete overriding passElement.autocomplete
        repeatPassElement.autocomplete = isLoginMode ? 'off' : 'new-password'

        // eslint-disable-next-line
    }, [isLoginMode])

    function switchLoginMode() {
        setIsLoginMode(!isLoginMode)
    }

    function getErrorMessage(e: any) {
        if (e?.response?.status === 404) {
            return 'Неверный логин или пароль'
        }
        return 'Ошибка входа'
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



    function focusElementAfterLoginInput() {
        const nextInput = isLoginMode ? getPasswordInputElement() : nameInput.current
        nextInput?.focus()
    }

    function focusElementAfterNameInput() {
        const nextInput = getPasswordInputElement()
        nextInput?.focus()
    }

    function focusElementAfterPasswordInput() {
        if (isLoginMode) {
            getInputAndLogin()
        }
        else {
            getRepeatPasswordInputElement()?.focus()
        }
    }



    function getInputAndLogin() {
        const passwordInput = getPasswordInputElement()
        if (!loginInput.current || !passwordInput) return

        const loginValue = loginInput.current.value.trim()
        const passwordValue = passwordInput.value

        if (isLoginInputValid(loginValue, passwordValue)) {
            login(loginValue, passwordValue)
        }
    }

    function getInputAndRegister() {
        const passwordInput = getPasswordInputElement()
        const repeatPasswordInput = getRepeatPasswordInputElement()
        if (!loginInput.current || !passwordInput || !nameInput.current || !repeatPasswordInput) return

        const loginValue = loginInput.current.value.trim()
        const nameValue = nameInput.current.value.trim()
        const passwordValue = passwordInput.value
        const repeatPasswordValue = repeatPasswordInput.value

        if (isRegisterInputValid(loginValue, nameValue, passwordValue, repeatPasswordValue)) {
            register(loginValue, nameValue, passwordValue)
        }
    }

    function isLoginInputValid(login: string, password: string): boolean {
        if (!login || !password || login.trim() === '' || password === '') {
            showWarningText('Заполните все поля')
            return false
        }

        login = login.trim()

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

        if (login.includes(' ')) {
            showWarningText('В логине не должно быть пробелов')
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

                <input ref={loginInput} name='username' type='text' placeholder='Логин' maxLength={32} onKeyDown={executeOnEnterPress(focusElementAfterLoginInput)} />
                <div className='expandable'>
                    <input ref={nameInput} name='name' type='text' placeholder='Отображаемое имя' maxLength={32} onKeyDown={executeOnEnterPress(focusElementAfterNameInput)} />
                </div>
                <input id='passwordInput' name='password' type='password' placeholder='Пароль' maxLength={64} autoComplete={isLoginMode ? 'current-password' : 'new-password'} onKeyDown={executeOnEnterPress(focusElementAfterPasswordInput)} />
                <div className='expandable'>
                    <input id='repeatPasswordInput' name='password' type='password' placeholder='Повторите пароль' maxLength={64} autoComplete={'new-password'} onKeyDown={executeOnEnterPress(getInputAndRegister)} />
                </div>
                <p id='loginWarningText' ref={loginWarningText}>Введите имя и пароль</p>

                <div className='btnContainer'>
                    <button id='dummyBtn'>dummy</button>
                    <button id='loginBtn' ref={loginBtn} onClick={getInputAndLogin}>Войти</button>
                    <button id='registerBtn' ref={registerBtn} onClick={getInputAndRegister}>Зарегистрироваться</button>
                </div>
            </div>

            <p id='loginSwitchMode' onClick={switchLoginMode}>{switchModeText}</p>
        </div>
    </div>
}



function getPasswordInputElement(): HTMLInputElement | null {
    return document.getElementById('passwordInput') as HTMLInputElement
}

function getRepeatPasswordInputElement(): HTMLInputElement | null {
    return document.getElementById('repeatPasswordInput') as HTMLInputElement
}

function executeOnEnterPress(action: Function) {
    return (e: React.KeyboardEvent<HTMLInputElement>) => {
        if (e.key !== 'Enter') return
        console.log('enter')
        action()
    }
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
