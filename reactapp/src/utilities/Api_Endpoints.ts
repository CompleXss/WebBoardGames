const BASE_URL_DEVELOPMENT = 'http://localhost:5042'
const BASE_URL_PRODUCTION = 'http://localhost:5042'

const BASE_URL =
    process.env.NODE_ENV === 'development'
        ? BASE_URL_DEVELOPMENT
        : BASE_URL_PRODUCTION

const ENDPOINTS = {
    Auth: {
        // register & login & refresh
        POST_REGISTER_URL: BASE_URL + '/auth/register',
        POST_LOGIN_URL: BASE_URL + '/auth/login',
        POST_REFRESH_TOKEN_URL: BASE_URL + '/auth/refresh',

        // logout
        POST_LOGOUT_URL: BASE_URL + '/auth/logout',
        POST_LOGOUT_FROM_ALL_DEVICES_URL: BASE_URL + '/auth/logout-from-all-devices',
        POST_LOGOUT_FROM_ANOTHER_DEVICES_URL: BASE_URL + '/auth/logout-from-another-devices',

        // other
        GET_IS_AUTHORIZED: BASE_URL + '/auth/isAuthorized',
        GET_USER_DEVICE_COUNT_URL: BASE_URL + '/auth/deviceCount',
    },

    Users: {
        GET_USER_INFO_URL: BASE_URL + '/user',
        GET_USER_INFO_BY_ID_URL: BASE_URL + '/user/',
        DELETE_USER_URL: BASE_URL + '/user',
    },

    GET_HISTORY_URL: BASE_URL + '/history',
    GET_LEADERBOARD: BASE_URL + '/leaderboard',
    GET_IS_IN_GAME: BASE_URL + '/isInGame/checkers',
    
    Hubs: {
        CHECKERS_LOBBY: BASE_URL + '/lobby/checkers',
        CHECKERS_GAME: BASE_URL + '/play/checkers',
    },
}

export default ENDPOINTS
