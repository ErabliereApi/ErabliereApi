export class Appareil {
    id: any;
    name?: string;
    description?: string;
    statut?: AppareilStatut;
    adresses: Array<AdresseAppareil> = [];
    ports: Array<PortAppareil> = [];
    nomsHost: Array<NomHost> = [];
}

export class AppareilStatut {
    id: any;
    etat?: string;
    raison?: string;
    raisonTTL?: string;
}

export class AdresseAppareil {
    id: any;
    addr?: string;
    addrtype?: string;
    vendeur?: string;
}

export class PortAppareil {
    id: any;
    port?: number;
    protocole?: string;
    etat?: PortEtat;
    portService?: PortService;
}

export class PortEtat {
    id: any;
    etat?: string;
    raison?: string;
    raisonTTL?: string;
    idPortAppareil?: any;
}

export class PortService {
    id: any;
    name: string = "";
    produit?: string;
    extraInfo?: string;
    methode?: string;
    cpEs?: string[];
}

export class NomHost {
    id: any;
    name?: string;
    type?: string;
}