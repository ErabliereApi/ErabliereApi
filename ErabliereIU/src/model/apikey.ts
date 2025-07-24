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
    customer?: Customer
    lastUsage?: Date;
}