import { Component } from "@angular/core";

@Component({
    selector: 'app-admin-connected-platform',
    templateUrl: './admin-connected-platform.component.html'
})
export class AdminConnectedPlatformComponent {
    constructor() { }
    platforms =
        [
            {
                name: 'EntraID',
                connected: true,
                description: `La plateforme EntraId est utilisée pour l'authentification des utilisateurs et la gestion des
                            accès. Elle permet aux administrateurs de gérer les utilisateurs, les rôles et les
                            permissions au sein de l'application.`,
                url: 'https://entra.microsoft.com/',
                logo: 'assets/images/entraid.ico'
            },
            {
                name: 'Hologram',
                connected: true,
                description: `Hologram est une plateforme de connectivité IoT qui fournit des cartes SIM et des services de
                            données pour les appareils connectés. Elle permet une gestion centralisée des appareils et
                            une connectivité mondiale.`,
                url: 'https://hologram.io/',
                logo: 'assets/images/hologram.jpg'
            },
            {
                name: 'Twilio',
                connected: true,
                description: `Twilio est une plateforme de communication cloud qui permet aux développeurs d'intégrer des fonctionnalités de
                            communication telles que la voix, la vidéo et les SMS dans leurs applications.`,
                url: 'https://www.twilio.com/',
                logo: 'assets/images/twilio.png'
            },
            {
                name: 'Stripe',
                connected: true,
                description: `Stripe est une plateforme de paiement en ligne qui permet aux entreprises d'accepter des paiements
                            par carte de crédit et d'autres méthodes de paiement. Elle offre des outils pour la gestion des
                            abonnements, la facturation et la prévention de la fraude.`,
                url: 'https://stripe.com/',
                logo: 'assets/images/stripe.png'
            },
            {
                name: 'Mapbox',
                connected: true,
                description: `Mapbox est une plateforme de cartographie et de localisation qui permet aux développeurs d'intégrer des cartes
                            personnalisées et des fonctionnalités de géolocalisation dans leurs applications.`,
                url: 'https://www.mapbox.com/',
                logo: 'assets/images/mapbox.png'
            },
            {
                name: 'AccuWeather',
                connected: true,
                description: `AccuWeather est une plateforme de prévisions météorologiques qui fournit des données en temps réel
                            sur la météo, y compris les alertes et les prévisions pour les entreprises et les développeurs.`,
                url: 'https://developer.accuweather.com/home',
                logo: 'assets/images/accuweather.ico'
            },
            {
                name: 'Senscap',
                connected: true,
                description: `Senscap est une plateforme de connectivité IoT qui permet aux entreprises de déployer et de gérer des
                            appareils IoT à grande échelle. Elle offre des solutions pour la collecte de données, l'analyse et
                            la visualisation des données en temps réel.`,
                url: 'https://sensecap.seeed.cc/portal',
                logo: 'assets/images/senscap.jpg'
            },
            {
                name: 'IpInfo',
                connected: true,
                description: `IpInfo est une plateforme qui fournit des informations sur les adresses IP, y compris la géolocalisation,
                            les données sur l'ISP et d'autres informations pertinentes.`,
                url: 'https://ipinfo.io/',
                logo: 'assets/images/ipinfo.svg'
            },
            {
                name: 'IBM Quantum',
                connected: true,
                description: `IBM Quantum est une plateforme qui permet aux développeurs d'accéder à des ordinateurs quantiques
                            via le cloud. Elle offre des outils pour le développement d'algorithmes quantiques et l'exécution
                            de simulations.`,
                url: 'https://www.ibm.com/quantum-computing/',
                logo: 'assets/images/ibm-quantum.png'
            },
            {
                name: 'Azure OpenAI',
                connected: true,
                description: `Azure OpenAI est une plateforme qui permet aux développeurs d'accéder à des modèles d'IA avancés
                            via le cloud. Elle offre des outils pour le développement d'applications basées sur l'IA et l'intégration
                            de modèles de langage dans les applications.`,
                url: 'https://azure.microsoft.com/en-us/services/cognitive-services/openai-service/',
                logo: 'assets/images/azure-openai.png'
            }
        ];
}