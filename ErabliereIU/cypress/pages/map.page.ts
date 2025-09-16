export class MapPage {
    visit(): this {
        cy.login();
        cy.visit('/map');
        return this;
    }
}