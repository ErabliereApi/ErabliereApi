export class Kit {
    id?: string
    nom?: string
    type?: string
    idCapteurAlimentation?: string
    idCapteurCharge?: string
    idCapteurAutonomie?: string
    dc?: string
    dateRevocation?: string
    derniereUtilisation?: string
}

export class PostKitPanne {
    nom?: string
    envoyerA?: string
    texterA?: string
    delaiSilenceMinutes?: number
    url?: string
}

export class PostKitPanneResponse {
    idKit?: string
    nomFichierConfiguration?: string
    fichierConfiguration?: string
    nomFichierAuthentification?: string
    fichierAuthentification?: string
}
