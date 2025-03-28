export class Rapport {
    id?: string;
    idErabliere?: string;
    dateDebut?: Date;
    dateFin?: Date;
    type?: string;
    utiliserTemperatureTrioDonnee?: boolean;
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