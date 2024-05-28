import axios from 'axios'
import { useQuery, useQueryClient } from 'react-query'
import { useNavigate } from 'react-router-dom'
import { useEffect, useRef, useState } from 'react'
import { MIN_LOGIN_LENGTH, MIN_PASSWORD_LENGTH } from 'src/utilities/auth'
import LoadingContent from '../LoadingContent/loadingContent'
import ENDPOINTS from '../../utilities/Api_Endpoints'
import './profile.css'

export default function Profile() {
    const navigate = useNavigate()
    const queryClient = useQueryClient()
    const deleteAccountDialog = useRef<HTMLDialogElement>(null)
    const deleteAccountPassInput = useRef<HTMLInputElement>(null)
    const { data: user, isLoading: loadingUsername, error: errorUsername, refetch: refetchUserInfo } = useQuery('profileUsername', fetchUserInfo)
    const { data: deviceCountResp, isLoading: loadingDeviceCount, error: errorDeviceCount, refetch: refetchDeviceCount } = useQuery('profileDeviceCount', fetchDeviceCount)
    const username = user?.name ?? 'loading...'
    const deviceCount = deviceCountResp ?? '?'

    const changeValueDialog = useRef<HTMLDialogElement>(null)
    const changeValueDialogConfirmButton = useRef<HTMLButtonElement>(null)
    const changeValueDialogValueInput = useRef<HTMLInputElement>(null)
    const [changeValueDialogTitle, setChangeValueDialogTitle] = useState<string>('')
    const [changeValueDialogOldValue, setChangeValueDialogOldValue] = useState<string>('')

    const changePasswordDialog = useRef<HTMLDialogElement>(null)
    const oldPassword = useRef<HTMLInputElement>(null)
    const newPassword = useRef<HTMLInputElement>(null)
    const repeatNewPassword = useRef<HTMLInputElement>(null)

    useEffect(() => {
        document.title = 'Профиль'
    }, [])

    function logout() {
        axios.post(ENDPOINTS.Auth.POST_LOGOUT_URL)
            .then(() => {
                queryClient.removeQueries('profileUsername')
                queryClient.removeQueries('profileDeviceCount')
                navigate('/login', { state: { doNotRedirect: true } })
            })
            .catch(e => console.log(e))
    }

    function logoutFromAnotherDevices() {
        axios.post(ENDPOINTS.Auth.POST_LOGOUT_FROM_ANOTHER_DEVICES_URL)
            .then(response => {
                if (response.status === 200) {
                    showDeviceCountWarningText('Успех!', 'green')
                    refetchDeviceCount()
                }
            })
            .catch(e => {
                showDeviceCountWarningText('Произошла ошибка', 'red')
                console.log(e)
            })
    }



    function openDeleteDialog() {
        deleteAccountDialog.current?.showModal()
    }

    function closeDeleteDialog() {
        deleteAccountDialog.current?.close()
    }

    function deleteAccount() {
        if (!deleteAccountPassInput.current) return

        const password = deleteAccountPassInput.current.value

        if (!password || password.trim().length < 1) {
            showDeleteAccountWarningText('Вы не ввели пароль!')
            return
        }
        showDeleteAccountWarningText(false)
        deleteAccountPassInput.current.value = ''

        axios.post(ENDPOINTS.Users.DELETE_USER_URL, {
            password: password
        })
            .then(response => {
                showDeviceCountWarningText(false)
                console.log(response.data)
                navigate('/login', { state: { doNotRedirect: true } })
            })
            .catch(e => {
                console.log(e)
                showDeviceCountWarningText('Не удалось удалить аккаунт!', 'red')
            })
            .finally(() => {
                closeDeleteDialog()
            })
    }

    function changeName() {
        if (!changeValueDialog.current || !changeValueDialogConfirmButton.current) return

        setChangeValueDialogTitle('Изменение имени')
        setChangeValueDialogOldValue(username)
        if (changeValueDialogValueInput.current) {
            changeValueDialogValueInput.current.value = ''
        }

        changeValueDialogConfirmButton.current.onclick = () => {
            if (!changeValueDialogValueInput.current) return

            let value = changeValueDialogValueInput.current.value

            if (!value) return
            value = value.trim()

            if (value.length < 1) return

            axios.post(ENDPOINTS.Users.POST_EDIT_USER_NAME_URL + value)
                .then(() => {
                    showChangeValueWarningText(false)
                    refetchUserInfo()
                    changeValueDialog.current?.close()
                })
                .catch(_ => showChangeValueWarningText('Не получилось изменить имя'))
        }

        showChangeValueWarningText(false)
        changeValueDialog.current.showModal()
    }

    function changeLogin() {
        if (!changeValueDialog.current || !changeValueDialogConfirmButton.current) return

        setChangeValueDialogTitle('Изменение логина')
        setChangeValueDialogOldValue(user?.login ?? '')
        if (changeValueDialogValueInput.current) {
            changeValueDialogValueInput.current.value = ''
        }

        changeValueDialogConfirmButton.current.onclick = () => {
            if (!changeValueDialogValueInput.current) return

            let value = changeValueDialogValueInput.current.value

            if (!value) return
            value = value.trim()

            if (value.length < MIN_LOGIN_LENGTH) {
                showChangeValueWarningText('Длина логина должна быть больше ' + (MIN_LOGIN_LENGTH - 1))
                return
            }

            if (value.includes(' ')) {
                showChangeValueWarningText('Логин не должен содержать пробелов')
                return
            }

            axios.post(ENDPOINTS.Users.POST_EDIT_USER_LOGIN_URL + value)
                .then(() => {
                    showChangeValueWarningText(false)
                    refetchUserInfo()
                    changeValueDialog.current?.close()
                })
                .catch(_ => showChangeValueWarningText('Не получилось изменить логин'))
        }

        showChangeValueWarningText(false)
        changeValueDialog.current.showModal()
    }

    function changePassword() {
        if (!oldPassword.current || !newPassword.current || !repeatNewPassword.current) return

        const oldPass = oldPassword.current.value
        const newPass = newPassword.current.value
        const repeatPass = repeatNewPassword.current.value

        if (!isPasswordValid(oldPass) || !isPasswordValid(newPass)) {
            return
        }

        if (oldPass === newPass) {
            showPasswordWarningText('Старый пароль совпадает с новым')
            return
        }

        if (newPass !== repeatPass) {
            showPasswordWarningText('Новый пароль не совпадает с повторно введенным')
            return
        }

        axios.post(ENDPOINTS.Users.POST_EDIT_USER_PASSWORD_URL, {
            oldPassword: oldPass,
            newPassword: newPass
        })
            .then(() => {
                showPasswordWarningText(false)
                changePasswordDialog.current?.close()
            })
            .catch(_ => showPasswordWarningText('Не получилось изменить пароль'))
    }

    function isPasswordValid(password: string): boolean {
        if (!password || password === '') {
            showPasswordWarningText('Заполните все поля')
            return false
        }

        if (password.length < MIN_PASSWORD_LENGTH) {
            showPasswordWarningText('Длина пароля должна быть больше ' + (MIN_PASSWORD_LENGTH - 1))
            return false
        }

        return true
    }



    function getProfileInfo() {
        if (errorUsername) return <p>Произошла ошибка!</p>

        return <LoadingContent loading={loadingUsername} content={<div>
            <p>Никнейм (отображаемое имя):</p>
            <div style={{ display: 'inline-block' }}>
                <p className='value'>{username}</p>
                <button onClick={changeName} className='editBtn'>.</button>
            </div>
            <br />

            {user?.login && (<>
                <p>Логин (нужен для входа):</p>
                <div style={{ display: 'inline-block' }}>
                    <p className='value'>{user.login}</p>
                    <button onClick={changeLogin} className='editBtn'>.</button>
                </div>
                <br />
            </>)}

        </div>} />
    }

    function getLoginDeviceInfo() {
        if (errorDeviceCount) return <p>Произошла ошибка!</p>

        return <LoadingContent loading={loadingDeviceCount} content={<div>
            <p id='loginCount'>{deviceCount}</p>
            <p id='deviceCountWarningText'>Сообщения еще нет...</p>
            <button onClick={logoutFromAnotherDevices}>Выйти со всех других устройств</button>
        </div>} />
    }



    return <div className='profileContainer'>
        <h1>Профиль</h1>

        <div id='accountInfo'>
            <h2>Информация об аккаунте</h2>
            <hr />
            {getProfileInfo()}
            <br />
            <button className='changePasswordBtn' onClick={() => {
                if (!changePasswordDialog.current || !oldPassword.current || !newPassword.current || !repeatNewPassword.current) return

                oldPassword.current.value = ''
                newPassword.current.value = ''
                repeatNewPassword.current.value = ''
                showPasswordWarningText(false)
                changePasswordDialog.current.showModal()
            }}>Сменить пароль</button>
        </div>

        <div id='loginDeviceInfo'>
            <p>Кол-во устройств, на которых вы вошли в аккаунт: </p>
            {getLoginDeviceInfo()}
        </div>

        <button className='exitBtn' onClick={logout}>Выйти<span className='icon' /></button>
        <button id='deleteAccountBtn' onClick={openDeleteDialog}>Удалить аккаунт</button>



        <dialog className='changeValueDialog' ref={changeValueDialog}>
            <h1>{changeValueDialogTitle}</h1>
            <hr></hr>

            <p>Старое значение:</p>
            <input type='text' disabled value={changeValueDialogOldValue}></input>

            <p>Новое значение:</p>
            <input ref={changeValueDialogValueInput} type='text' maxLength={32} placeholder="Введите новое значение"></input>

            <p id='changeValueWarningText'></p>

            <div className='buttonsWrapper'>
                <button ref={changeValueDialogConfirmButton}>Подтвердить</button>

                <button onClick={() => {
                    changeValueDialog.current?.close()
                }}>Отмена</button>
            </div>
        </dialog>


        <dialog className='changeValueDialog' ref={changePasswordDialog}>
            <h1>Изменение пароля</h1>
            <hr></hr>

            <p>Старый пароль</p>
            <input ref={oldPassword} autoComplete='current-password' type='password' maxLength={64} placeholder='Введите текущий пароль'></input>

            <p>Новый пароль</p>
            <input ref={newPassword} autoComplete='new-password' type='password' maxLength={64} placeholder="Введите новый пароль"></input>
            <input ref={repeatNewPassword} autoComplete='new-password' type='password' maxLength={64} placeholder="Повторите новый пароль"></input>

            <p id='passwordWarningText' style={{ maxWidth: '100%' }}></p>

            <div className='buttonsWrapper'>
                <button onClick={changePassword}>Подтвердить</button>

                <button onClick={() => {
                    changePasswordDialog.current?.close()
                }}>Отмена</button>
            </div>
        </dialog>



        <dialog id='deleteAccountDialog' ref={deleteAccountDialog} onClose={closeDeleteDialog}>
            <p>Уверены, что хотите <span>удалить аккаунт</span> ?</p>
            <p>Для подтверждения введите пароль:</p>
            <input ref={deleteAccountPassInput} autoComplete='off' type='password' placeholder='Введите пароль'></input>
            <p id='deleteAccountWarningText'></p>
            <button onClick={deleteAccount}>Удалить</button>
            <button onClick={closeDeleteDialog}>Ладно, не буду...</button>
        </dialog>
    </div>
}



async function fetchUserInfo() {
    return axios.get(ENDPOINTS.Users.GET_USER_INFO_URL)
        .then(response => response.data)
}

async function fetchDeviceCount() {
    return axios.get(ENDPOINTS.Auth.GET_USER_DEVICE_COUNT_URL)
        .then(response => response.data)
}



function showDeviceCountWarningText(show: string | false, color?: string) {
    const text = document.getElementById('deviceCountWarningText')
    if (!text) {
        console.log('Не могу найти элемент deviceCountWarningText')
        return
    }

    if (show) {
        text.textContent = show
    }
    text.style.opacity = show ? '1' : '0';
    text.style.color = color ?? 'red'
}

function showChangeValueWarningText(show: string | false, color?: string) {
    const text = document.getElementById('changeValueWarningText')
    if (!text) {
        console.log('Не могу найти элемент changeValueWarningText')
        return
    }

    if (show) {
        text.textContent = show
    }
    text.style.opacity = show ? '1' : '0';
    text.style.color = color ?? 'red'
}

function showPasswordWarningText(show: string | false, color?: string) {
    const text = document.getElementById('passwordWarningText')
    if (!text) {
        console.log('Не могу найти элемент passwordWarningText')
        return
    }

    if (show) {
        text.textContent = show
    }
    text.style.opacity = show ? '1' : '0';
    text.style.color = color ?? 'red'
}

function showDeleteAccountWarningText(show: string | false) {
    const text = document.getElementById('deleteAccountWarningText')
    if (!text) {
        console.log('Не могу найти элемент deleteAccountWarningText')
        return
    }

    if (show) {
        text.textContent = show
    }
    text.style.opacity = show ? '1' : '0';
}
