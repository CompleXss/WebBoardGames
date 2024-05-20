import axios from "axios"
import { useQuery } from "react-query"
import { Navigate, useLocation } from "react-router-dom"
import ENDPOINTS from "../../utilities/Api_Endpoints"
import Loading from "../Loading/loading"

type Props = {
    gameName: string
    onOk: JSX.Element
    redirect?: string
    inverse?: boolean
    cascadeRedirect?: boolean
}

export default function RequireActiveGame({ gameName, onOk, redirect, inverse, cascadeRedirect }: Props) {
    const state = useLocation().state
    if (!redirect) redirect = '/login'

    const { data, isLoading, isFetching } = useQuery(['isInGame ' + gameName, redirect], () => isInGame(gameName), {
        retry: false,
        enabled: !(state?.doNotRedirect)
    })

    if (state?.doNotRedirect) return onOk
    if (isLoading || isFetching) return <Loading />

    const isOk = inverse ? !data : data
    const nextState = cascadeRedirect ? null : { doNotRedirect: true }

    return isOk
        ? onOk
        : <Navigate to={redirect} state={nextState} />
}

async function isInGame(gameName: string) {
    try {
        var response = await axios.get(ENDPOINTS.GET_IS_IN_GAME + gameName)
        return Boolean(response?.data)

    } catch (error) {
        return false
    }
}