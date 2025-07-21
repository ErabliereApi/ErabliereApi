import { ApiKey } from "./apikey";
import {CustomerAccess} from "./customerAccess";

export class Customer {
    id?: any
    name?: string
    uniqueName?: string
    email?: string
    secondaryEmail?: string
    accountType?: string
    stripeId?: string
    externalAccountUrl?: string
    creationTime?: string
    lastAccessTime?: string
    customerErablieres?: Array<CustomerAccess>
    apiKeys?: Array<ApiKey>
}
