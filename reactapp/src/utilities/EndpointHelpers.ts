import axios from "axios"
import ENDPOINTS from "./Api_Endpoints"
import { User } from "./Api_DataTypes"

export async function getUserInfoByID(userID: string): Promise<User> {
    try {
        const response = await axios.get(ENDPOINTS.Users.GET_USER_INFO_BY_ID_URL + userID)
        return response.data as User

    } catch (error) {
        console.log('Can not get user info!')

        return {
            publicID: userID,
            name: 'unknown',
        }
    }
}
