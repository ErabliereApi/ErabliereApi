import { MapPage } from "../pages/map.page";

describe("Map page", { testIsolation: false }, () => {
    const mapPage = new MapPage();

    it("should be able to navigate to map page", () => {
        mapPage.visit();

        cy.url().should('include', '/map');
    });

    it("should not display whitescreen", () => {
        // Wait for the delay before the #whitescreen can appear
        cy.wait(9000);
        cy.get('#whitescreen').should('have.css', 'display', 'none');
    });
});