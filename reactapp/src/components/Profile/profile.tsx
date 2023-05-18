import axios from 'axios'
import { useQuery, useQueryClient } from 'react-query'
import { useNavigate } from 'react-router-dom'
import { useRef } from 'react'
import LoadingContent from '../LoadingContent/loadingContent'
import ENDPOINTS from '../../utilities/Api_Endpoints'
import './profile.css'

export default function Profile() {
    const deleteAccountDialog = useRef<HTMLDialogElement>(null)
    const navigate = useNavigate()
    const queryClient = useQueryClient()
    const { data: user, isLoading: loadingUsername, error: errorUsername } = useQuery('profileUsername', fetchUsername)
    const { data: deviceCountResp, isLoading: loadingDeviceCount, error: errorDeviceCount, refetch: refetchDeviceCount } = useQuery('profileDeviceCount', fetchDeviceCount)
    const username = user ? user.name : 'loading'
    const deviceCount = deviceCountResp ?? '?'

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
                    showWarningText('Успех!', 'green')
                    refetchDeviceCount()
                }
            })
            .catch(e => {
                showWarningText('Произошла ошибка', 'red')
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
        axios.delete(ENDPOINTS.Users.DELETE_USER_URL)
            .then(response => {
                showWarningText(false)
                console.log(response.data)
                navigate('/login', { state: { doNotRedirect: true } })
            })
            .catch(e => {
                console.log(e)
                showWarningText('Не удалось удалить аккаунт!', 'red')
            })
            .finally(() => {
                closeDeleteDialog()
            })
    }



    function getProfileInfo() {
        if (errorUsername) return <p>Произошла ошибка!</p>

        return <LoadingContent loading={loadingUsername} content={<div>
            <p>Никнейм (он же логин):</p>
            <p className='username'>{username}</p>
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

    return <div id='profileContainer'>
        <h1>Профиль</h1>

        <div id='accountInfo'>
            <h2>Информация об аккаунте</h2>
            <hr />
            {getProfileInfo()}
        </div>

        <div id='loginDeviceInfo'>
            <p>Кол-во устройств, на которых вы вошли в аккаунт: </p>
            {getLoginDeviceInfo()}
        </div>

        <button id='exitBtn' onClick={logout}>Выйти<span className='icon' /></button>
        <button id='deleteAccountBtn' onClick={openDeleteDialog}>Удалить аккаунт</button>
        <dialog id='deleteAccountDialog' ref={deleteAccountDialog} className='myModal' onClose={closeDeleteDialog}>

            <p>Уверены, что хотите <span>удалить аккаунт</span> ?</p>
            <button onClick={deleteAccount}>Да</button>
            <button onClick={closeDeleteDialog}>Нет</button>
        </dialog>
    </div>
}



async function fetchUsername() {
    return axios.get(ENDPOINTS.Users.GET_USER_INFO_URL)
        .then(response => response.data)
}

async function fetchDeviceCount() {
    return axios.get(ENDPOINTS.Auth.GET_USER_DEVICE_COUNT_URL)
        .then(response => response.data)
}

function showWarningText(show: string | false, color?: string) {
    let text = document.getElementById('deviceCountWarningText')
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