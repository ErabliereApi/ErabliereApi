export class Abonnement {
    id?: string
    customerId?: string
    plan?: string
    dateDebut?: string
    dateFin?: string
    statut?: string
    stripeSubscriptionId?: string
    dc?: string
    dm?: string
}

export class PostAbonnement {
    plan?: string
    dateDebut?: string
    dateFin?: string
}

export class PutAbonnement {
    id?: string
    plan?: string
    dateDebut?: string
    dateFin?: string
}

export class PostAbonnementResponse {
    id?: string
    checkoutUrl?: string
}
