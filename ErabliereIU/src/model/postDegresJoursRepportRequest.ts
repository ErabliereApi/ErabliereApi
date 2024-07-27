export class PostDegresJoursRepportRequest {
    id: any;
    idErabliere: any;
    idCapteur: any;
    dateDebut: Date = new Date(2024, 0, 1, 0, 0, 0);
    dateFin: Date = new Date(Date.now() - 1 * 24 * 60 * 60 * 1000);
    seuilTemperature: number = 0;
    utiliserTemperatureTrioDonnee: boolean = false;
}

export class ResponseRapportDegreeJours {
    requetes?: PostDegresJoursRepportRequest;
    rapport?: RapportDegreeJours[];
}

export class RapportDegreeJours {
    date: any;
    temperature: any;
    degreJour: any;
}