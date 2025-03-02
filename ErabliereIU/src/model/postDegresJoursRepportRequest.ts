export class PostDegresJoursRepportRequest {
    id: any;
    idErabliere: any;
    idCapteur: any;
    dateDebut: Date = new Date(2024, 0, 1, 0, 0, 0);
    dateFin: Date = new Date(Date.now() - 1 * 24 * 60 * 60 * 1000);
    seuilTemperature: number = 0;
    utiliserTemperatureTrioDonnee: boolean = false;
    afficherDansDashboard: boolean = false;
}

export class ResponseRapportDegreeJours {
    id: any;
    idErabliere: any;
    idCapteur: any;
    dateDebut?: Date;
    dateFin?: Date;
    seuilTemperature: number = 0;
    utiliserTemperatureTrioDonnee: boolean = false;
    donnees?: RapportDegreeJours[];
    moyenne: any;
    somme: any;
    min: any;
    max: any;
}

export class RapportDegreeJours {
    date: any;
    moyenne: any;
    somme: any;
    min: any;
    max: any;
}