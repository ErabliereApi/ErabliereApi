import { HomePage } from "cypress/pages/home.page";
import { NotesPage } from "cypress/pages/notes.page";

describe("Notes page", { testIsolation: false }, () => {
    const homePage = new HomePage();
    let notesPage: NotesPage | null = null;

    it("should be able to navigate to note page", () => {
        homePage.visit();

        notesPage = homePage.clickOnNotesButtonNavMenu();

        notesPage.getPageTitle().should('have.text', 'Notes');
    });

    it("should add note with specific date", () => {
        if (notesPage == null) {
            throw new Error("Notes page is not initialized");
        }

        // generate random title and content
        const title = `Note title ${Math.floor(Math.random() * 1000)}`;
        const content = `Note content ${Math.floor(Math.random() * 1000)}`;
        const date = new Date();

        const dstring = `${date.getFullYear()}-${pad2(date.getMonth() + 1)}-${pad2(date.getDate())}`;

        notesPage.addNote(title, content, dstring);

        cy.wait(1000);

        // validate that note is added
        notesPage.getNoteTitle().should('have.text', title);
        notesPage.getNoteDescription().should('have.text', `${content}\n`);
        notesPage.getNoteDate().should('have.text', `Date de Création: ${pad2(date.getDate())}-${pad2(date.getMonth() + 1)}-${date.getFullYear()}`);
    });

    it("should click on cancel button", () => {
        if (notesPage == null) {
            throw new Error("Notes page is not initialized");
        }

        notesPage.getCancelButton().click();
    });

    it("should add note without specifing date", () => {
        if (notesPage == null) {
            throw new Error("Notes page is not initialized");
        }

        // generate random title and content
        const title = `Note title ${Math.floor(Math.random() * 1000)}`;
        const content = `Note content ${Math.floor(Math.random() * 1000)}`;

        notesPage.addNote(title, content);

        cy.wait(1000);

        // validate that note is added
        notesPage.getNoteTitle().should('have.text', title);
        notesPage.getNoteDescription().should('have.text', `${content}\n`);
    });

    function pad2(number: number) {
        return (number < 10 ? '0' : '') + number;
    }
});