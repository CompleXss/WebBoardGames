import axios from 'axios'
import { useQuery, useQueryClient } from 'react-query'
import { useNavigate } from 'react-router-dom'
import ENDPOINTS from '../../utilities/Api_Endpoints'
import './profile.css'

export default function Profile() {
    const navigate = useNavigate()
    const queryClient = useQueryClient()
    const { data: user, isLoading: loadingUsername, error: errorUsername } = useQuery('profileUsername', fetchUsername)
    const { data: deviceCountResp, isLoading: loadingDeviceCount, error: errorDeviceCount, refetch: refetchDeviceCount } = useQuery('profileDeviceCount', fetchDeviceCount, {
        enabled: !loadingUsername
    })
    const username = user ? user.name : 'loading'
    const deviceCount = deviceCountResp ?? '?'

    async function logout() {
        axios.post(ENDPOINTS.POST_LOGOUT_URL)
            .then(() => {
                queryClient.removeQueries('profileUsername')
                queryClient.removeQueries('profileDeviceCount')
                navigate('/login')
            })
            .catch(e => console.log(e))
    }

    function logoutFromAnotherDevices() {
        axios.delete(ENDPOINTS.DELETE_ANOTHER_DEVICES_REFRESH_TOKENS_URL)
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

    function getProfileInfo() {
        if (loadingUsername) return <p>Загружаю...</p>
        if (errorUsername) return <p>Произошла ошибка!</p>

        return <div>
            <p>Никнейм (он же логин):</p>
            <p className='username'>{username}</p>
        </div>
    }

    function getLoginDeviceInfo() {
        if (loadingDeviceCount) return <p>Загружаю...</p>
        if (errorDeviceCount) return <p>Произошла ошибка!</p>

        return <div>
            <p id='loginCount'>{deviceCount}</p>
            <p id='deviceCountWarningText'>Сообщения еще нет...</p>
            <button onClick={logoutFromAnotherDevices}>Выйти со всех других устройств</button>
        </div>
    }

    if (loadingUsername) return <div>Loading...</div>

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
        <button id='deleteAccountBtn'>Удалить аккаунт</button>
    </div>
}



async function fetchUsername() {
    return axios.get(ENDPOINTS.GET_USER_INFO_URL)
        .then(response => response.data)
}

async function fetchDeviceCount() {
    return axios.get(ENDPOINTS.GET_USER_DEVICE_COUNT_URL)
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