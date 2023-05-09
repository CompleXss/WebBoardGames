const BASE_URL_DEVELOPMENT = 'http://localhost:5042'
const BASE_URL_PRODUCTION = 'http://localhost:5042'

const BASE_URL =
    process.env.NODE_ENV === 'development'
        ? BASE_URL_DEVELOPMENT
        : BASE_URL_PRODUCTION

const ENDPOINTS = {
    POST_REGISTER_URL: BASE_URL + '/auth/register',
    POST_LOGIN_URL: BASE_URL + '/auth/login',
    POST_LOGOUT_URL: BASE_URL + '/auth/logout',
    GET_REFRESH_TOKEN_URL: BASE_URL + '/auth/refresh',
    
    GET_IS_AUTHORIZED: BASE_URL + '/auth/isAuthorized',
    GET_USER_DEVICE_COUNT_URL: BASE_URL + '/auth/deviceCount',
    DELETE_ANOTHER_DEVICES_REFRESH_TOKENS_URL: BASE_URL + '/auth/anotherDevices-logout',

    GET_USER_INFO_URL: BASE_URL + '/user',
    GET_HISTORY_URL: BASE_URL + '/history/33',
    GET_CHECKERS_GAME_URL: BASE_URL + '/',
    GET_ACTIVE_GAME_URL: BASE_URL + '/',
}

export default ENDPOINTS