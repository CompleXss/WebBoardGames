import axios, { ResponseType } from "axios"
import { HttpClient, HttpRequest, HttpResponse, HubConnectionBuilder, IHttpConnectionOptions, LogLevel } from "@microsoft/signalr"

class AxiosHttpClient extends HttpClient {
    async get(url: string) {
        const response = await axios.get(url)
        return {
            statusCode: response.status,
            statusText: response.statusText,
            content: response.data,
        }
    }

    async post(url: string) {
        const response = await axios.post(url)
        return {
            statusCode: response.status,
            statusText: response.statusText,
            content: response.data,
        }
    }

    async delete(url: string) {
        const response = await axios.delete(url)
        return {
            statusCode: response.status,
            statusText: response.statusText,
            content: response.data,
        }
    }

    async send(request: HttpRequest): Promise<HttpResponse> {
        const response = await axios.request({
            url: request.url,
            method: request.method,
            responseType: request.responseType?.toString() as ResponseType ?? 'text',
            headers: request.headers,
            timeout: request.timeout,
            withCredentials: request.withCredentials,
            signal: request.abortSignal,
        })
        return {
            statusCode: response.status,
            statusText: response.statusText,
            content: response.data,
        }
    }
}



export const httpConnectionOptions: IHttpConnectionOptions = {
    withCredentials: true,
    httpClient: new AxiosHttpClient()
}

export function getConnectionTo(url: string) {
    return new HubConnectionBuilder()
        .withUrl(url, httpConnectionOptions)
        .withAutomaticReconnect()
        .configureLogging(process.env.NODE_ENV === 'development' ? LogLevel.Warning : LogLevel.None)
        .build();
}
