import { Customer } from "./customer";

export class ApiKey
{
    id?: string;
    name?: string;
    key?: string;
    creationTime?: Date;
    revocationTime?: Date;
    deletionTime?: Date;
    customerId?: string;
    customer?: Customer;
    lastUsage?: Date;
    authorizeUris?: string;
    authorizeVerbs?: string;
}

export class PostApiKey {
    name?: string;
    customerId?: string;
    authorizeUris?: string;
    authorizeVerbs?: string;
}

export class PutApiKeyRestriction {
    authorizeUris?: string;
    authorizeVerbs?: string;
}