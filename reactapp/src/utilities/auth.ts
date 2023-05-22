import axios from "axios";
import ENDPOINTS from "./Api_Endpoints";
import { NavigateFunction } from "react-router-dom";

var refreshing: boolean = false
var navigateFunc: NavigateFunction | null = null

axios.defaults.withCredentials = true
axios.interceptors.request.clear()
axios.interceptors.response.clear()



// error => try refresh token pair if 401
axios.interceptors.response.use(response => response, async error => {
    if (refreshing || !error.response || error.response.status !== 401) {
        return Promise.reject(error)
    }
    const config = error.config

    if (!config._retry) {
        config._retry = true
        console.log('refreshing tokens...')

        refreshing = true
        let response = await refreshTokenPair(config);
        refreshing = false
        if (response) return response
    }

    // there was a retry already
    if (!config.doNotRedirect) navigate('/login')
    return Promise.reject(error)
})



async function refreshTokenPair(config: any) {
    try {
        const response = await axios.post(ENDPOINTS.Auth.POST_REFRESH_TOKEN_URL, config)

        if (response.status === 200) {
            return axios(config)
        }

    } catch (e) {
        //const response = (e as any).response
        //console.log(response ?? e)
    }
}

export function setNavigateFunc(navigate: NavigateFunction) {
    navigateFunc = navigate;
}

export async function isAuthorized(): Promise<boolean> {
    const config: any = { doNotRedirect: true }
    return axios.get(ENDPOINTS.Auth.GET_IS_AUTHORIZED, config)
        .then(_ => true)
        .catch(_ => false)
}

function navigate(path: string) {
    if (navigateFunc === null) {
        console.error('Can not access navigate function in http interceptor!')
        return
    }
    navigateFunc(path);
}