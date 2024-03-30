import { useQuery } from "react-query"
import { Navigate, useLocation } from "react-router-dom"
import { isAuthorized } from "../../utilities/auth"
import Loading from "../Loading/loading"

type Props = {
    onOk: JSX.Element
    redirect?: string
    inverse?: boolean
    cascadeRedirect?: boolean
}

export const REDIRECT_QUERY_PARAM_NAME = 'redirect'

export default function RequireAuth({ onOk, redirect, inverse, cascadeRedirect }: Props) {
    const location = useLocation()
    const state = location.state

    if (!redirect) {
        redirect = `/login?${REDIRECT_QUERY_PARAM_NAME}=${location.pathname}`
    }

    const { data, isLoading, isFetching } = useQuery(['isAuthorized', redirect], isAuthorized, {
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