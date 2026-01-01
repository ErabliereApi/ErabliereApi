export class Rapport {
    id?: string;
    idErabliere?: string;
    dateDebut?: Date;
    dateFin?: Date;
    dc?: Date;
    dateModification?: Date;
    type?: string;
    utiliserTemperatureTrioDonnee?: boolean;
    seuilTemperature?: number;
    afficherDansDashboard?: boolean;
    nom?: string;
    somme?: number;
    moyenne?: number;
    min?: number;
    max?: number;

    donnees: RapportDonnee[] = [];
}

export class RapportDonnee {
    date?: Date;
    moyenne?: number;
    somme?: number;
    min?: number;
    max?: number;
}

export class PutRapport {
    nom?: string;
    type?: string;
    dateDebut?: Date;
    dateFin?: Date;
    utiliserTemperatureTrioDonnee?: boolean;
    seuilTemperature?: number;
    afficherDansDashboard?: boolean;
}