export class IpInfo {
    id: any;
    ip: string = '';
    network: string = '';
    country: string = '';
    countryCode: string = '';
    continent: string = '';
    asn: string = '';
    aS_name: string = '';
    aS_domain: string = '';
    isAllowed: boolean = true;
    latitude: number = 0;
    longitude: number = 0;
    dc?: string;
    dm?: string;
}