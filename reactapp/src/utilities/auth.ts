import axios, { AxiosRequestConfig } from "axios";
import ENDPOINTS from "./Api_Endpoints";
import { NavigateFunction } from "react-router-dom";

var navigateFunc: NavigateFunction | null = null

axios.defaults.withCredentials = true
axios.interceptors.request.clear()
axios.interceptors.response.clear()

// error => try refresh token pair if 401
axios.interceptors.response.use(response => response, async error => {
    if (!error.response || error.response.status !== 401) {
        return Promise.reject(error)
    }
    const config = error.config

    if (!config._retry) {
        config._retry = true
        console.log('refreshing tokens...')
        return (await refreshTokenPair(config)) ?? Promise.reject(error)
    }

    // there was a retry already
    if (config.url === ENDPOINTS.GET_REFRESH_TOKEN_URL) {
        navigate('/login')
    }

    return Promise.reject(error)
})



async function refreshTokenPair(config: any) {
    try {
        const response = await axios.get(ENDPOINTS.GET_REFRESH_TOKEN_URL, config)
        
        if (response.status === 200) {
            return axios(config)
        }

    } catch (e) {
    }
}

export function setNavigateFunc(navigate: NavigateFunction) {
    navigateFunc = navigate;
}

function navigate(path: string) {
    if (navigateFunc === null) {
        console.error('Can not access navigate function in http interceptor!')
        return
    }
    navigateFunc(path);
}