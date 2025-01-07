import { DonneeCapteur } from "./donneeCapteur";

export class Capteur {
    id?: string;
    idErabliere?: string;
    nom?: string;
    symbole?: string;
    afficherCapteurDashboard?: boolean;
    ajouterDonneeDepuisInterface: boolean = false
    dc?: string;
    donnees?: DonneeCapteur[];
    indiceOrdre?: number;
    taille?: number;
    batteryLevel?: number;
    type?: string;
    externalId?: string;
    lastMessageTime?: string;
    online?: boolean;
    reportFrequency?: number;
    displayType?: string;
    displayTop?: number;
    displayMin?: number;
    displayMax?: number;
    capteurStyle?: CapteurStyle;
}

export class CapteurStyle {
    id: any;
    backgroundColor?: string;
    color?: string;
    borderColor?: string;
    fill?: boolean;
    pointBackgroundColor?: string;
    pointBorderColor?: string;
    tension?: number;
    dSetBorderColors?: string;
    useGradient?: boolean;
    g1Stop?: number;
    g1Color?: string;
    g2Stop?: number;
    g2Color?: string;
    g3Stop?: number;
    g3Color?: string;
}
