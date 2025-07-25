import { FormUtil } from "cypress/util/formUtil"

export class AlertesPage {
    getPageTitle(): Cypress.Chainable<JQuery<HTMLElement>> {
        return cy.get('alerte-page > div > h3')
    }
    clickOnAddAlerteButton(): this {
        FormUtil.clickButton("alerte-page", "ajouter-alerte-btn");
        return this;
    }
    typeEmail(email: string): this {
        FormUtil.typeTextBaseOnName(email, "alerte-page", "destinataireCourriel");
        return this;
    }
    typeName(name: string): this {
        FormUtil.typeTextBaseOnFormControlName(name, "alerte-page", "nom");
        return this;
    }
    clickOnCreateButton(): this {
        FormUtil.clickButton("alerte-page", "creer-alerte-donnees-btn");
        return this;
    }
    getLastAlerteDonneesEmail(): Cypress.Chainable<JQuery<HTMLElement>> {
        return cy.get('alerte-page').then($obj => {
            // find the last row of the table and validate the email
            return $obj.find("table tbody tr td:nth-child(4)").last();
        });
    }
}