import { useQuery } from "react-query"
import { Navigate, useLocation } from "react-router-dom"
import { isAuthorized } from "../../utilities/auth"
import Loading from "../Loading/loading"

export default function RequireAuth({ onOk, redirect, inverse, cascadeRedirect }: { onOk: JSX.Element, redirect?: string, inverse?: boolean, cascadeRedirect?: boolean }) {
    const state = useLocation().state
    if (!redirect) redirect = '/login'

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