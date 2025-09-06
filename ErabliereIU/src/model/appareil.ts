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
    protocol?: string;
}

export class NomHost {
    id: any;
    name?: string;
    type?: string;
}