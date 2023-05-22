import axios from "axios"
import { useQuery } from "react-query"
import { Navigate, useLocation } from "react-router-dom"
import ENDPOINTS from "../../utilities/Api_Endpoints"
import Loading from "../Loading/loading"

export default function RequireActiveGame({ onOk, redirect, inverse, cascadeRedirect }: { onOk: JSX.Element, redirect?: string, inverse?: boolean, cascadeRedirect?: boolean }) {
    const state = useLocation().state
    if (!redirect) redirect = '/login'

    const { data, isLoading, isFetching } = useQuery(['isInGame', redirect], isInGame, {
        retry: false,
        enabled: !(state?.doNotRedirect)
    })

    if (state?.doNotRedirect) return onOk
    if (isLoading || isFetching) return <Loading />

    const isOk = inverse ? !data : data
    // console.log('fetching... redirect: ', redirect)
    // console.log(!isOk)

    const nextState = cascadeRedirect ? null : { doNotRedirect: true }

    return isOk
        ? onOk
        : <Navigate to={redirect} state={nextState} />
}

async function isInGame() {
    try {
        var response = await axios.get(ENDPOINTS.GET_IS_IN_GAME)
        return Boolean(response?.data)

    } catch (error) {
        return false
    }
}