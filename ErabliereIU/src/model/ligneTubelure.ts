export const TYPES_LIGNE = ['principale', 'secondaire', 'laterale'] as const;

export type TypeLigne = typeof TYPES_LIGNE[number];

export class LigneTubelure {
    id?: string
    idErabliere?: string
    nom?: string
    typeLigne?: string
    coordonneesJson?: string
    dc?: string
    dm?: string
}
