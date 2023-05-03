const BASE_URL_DEVELOPMENT = 'http://localhost:5042'
const BASE_URL_PRODUCTION = 'http://localhost:5042'

const BASE_URL =
    process.env.NODE_ENV === 'development'
        ? BASE_URL_DEVELOPMENT
        : BASE_URL_PRODUCTION

const ENDPOINTS = {
    GET_HISTORY_URL: BASE_URL + '/history/33',
    GET_CHECKERS_GAME_URL: BASE_URL + '/',
    GET_ACTIVE_GAME_URL: BASE_URL + '/',
}

export default ENDPOINTS;