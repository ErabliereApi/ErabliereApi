import { Capteur } from "./capteur";
import {CustomerAccess} from "./customerAccess";
import { Horaire } from "./horaire";

export class Erabliere {
    id?: any;
    nom?: string;
    description?: string;
    addresse?: string;
    regionAdministrative?: string;
    ipRule?: string;
    indiceOrdre?: number;
    codePostal?: string;
    isPublic?: boolean;
    afficherSectionBaril?: boolean;
    afficherTrioDonnees?: boolean;
    afficherSectionDompeux?: boolean;
    capteurs?: Array<Capteur>;
    customerErablieres?: Array<CustomerAccess>;
    afficherPredictionMeteoJour?: boolean;
    afficherPredictionMeteoHeure?: boolean;
    dimensionPanneauImage?: number;
    longitude?: number;
    latitude?: number;
    base?: number;
    sommet?: number;
    horaire?: Array<Horaire>;

    /* La date de création de l'érablière */
    dc?: string;
}
