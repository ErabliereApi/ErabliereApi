export class PutCapteur {
    id?: number;
    idErabliere?: string;
    nom?: string;
    symbole?: string;
    afficherCapteurDashboard?: boolean;
    ajouterDonneeDepuisInterface: boolean = false
    dc?: string;
    taille?: number;
    batteryLevel?: number;
    type?: string;
    externalId?: string;
    lastMessageTime?: string;
    online?: boolean;
    reportFrequency?: number;
    displayType?: string;
}
