import { HomePage } from "../pages/home.page";

describe('Graph pannel test', { testIsolation: false }, () => {
    const homePage = new HomePage();
    const tauxSucreId = "010e708b-a7d0-449e-77e2-08d9d37ca582";
    let baseValue = "2.3";

    it('Check the base value of "Taux de sucre" pannel', () => {
        const erabliereName = "Érablière A";
        homePage.visit()
                .searchErabliere(erabliereName)
                .clickOnLink(erabliereName)
                .getGraphPannel(tauxSucreId)
                .find('h3')
                .should($t => {
                    const text = $t.text()
                
                    expect(text).to.match(/Taux de sucre/);
                    
                    if (text.indexOf(baseValue) > -1) {
                        baseValue = "2.4";
                    }
                });
    });

    it('Add data in "Taux de sucre" pannel', () => {
        let pannelCompoenent = homePage.getGraphPannel(tauxSucreId);
        let addButton = pannelCompoenent.getAddButton();

        addButton.should('exist');
        addButton.click();

        pannelCompoenent.enterValue(baseValue.toString())
        pannelCompoenent.enterDate();
        
        // There is now a new button 'Ajouter' that appears when the first click happend.
        addButton = pannelCompoenent.getAddButton();

        addButton.should('exist');
        addButton.click();
    });

    it('Should make the form disappear', () => {
        // Find the button 'Annuler'
        let graphPannel = homePage.getGraphPannel(tauxSucreId);
        let cancelButton = graphPannel.getCancelButton();
        
        cancelButton.should('exist');
        cancelButton.click();

        // The form should not be displayed anymore
        graphPannel.find('input[name="valeur"]').should('not.exist');
        graphPannel.find('input[name="date"]').should('not.exist');
    });

    it('Should see that the data is added', () => {
        let graphPannel = homePage.getGraphPannel(tauxSucreId);

        cy.wait(5000);

        let title = graphPannel.find('h3');
        title.should('exist');
        title.should('contain', baseValue);
    });
});