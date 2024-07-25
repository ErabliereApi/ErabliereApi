export class PostDegresJoursRepportRequest {
    id: any;
    idCapteur: any;
    dateDebut: any;
    dateFin: any;
    seuil: any;
    utiliserTrioDonnees: boolean = false;
}

export class ResponseRapportDegreeJours {
    requetes?: PostDegresJoursRepportRequest;
    rapports?: RapportDegreeJours[];
}

export class RapportDegreeJours {
    date: any;
    temperature: any;
    degresJours: any;
}