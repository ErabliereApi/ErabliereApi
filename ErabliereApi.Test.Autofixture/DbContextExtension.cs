using AutoFixture;
using ErabliereApi.Depot.Sql;
using ErabliereApi.Donnees;

namespace ErabliereApi.Test.Autofixture;

internal static class DbContextExtension
{
    /// <summary>
    /// Permet d'ajouter des données des une instance de <see cref="ErabliereDbContext"/>. 
    /// Une fois les données ajouter, la même instance est retourner par la méthode.
    /// </summary>
    public static ErabliereDbContext PopulatesDbSets(this ErabliereDbContext context, IFixture fixture)
    {
        context.Erabliere.AddRange(fixture.CreateMany<Erabliere>());

        context.SaveChanges();

        foreach (var erabliere in context.Erabliere)
        {
            var capteurs = fixture.CreateMany<Capteur>().ToList();
            foreach (var capteur in capteurs)
            {
                capteur.Erabliere = erabliere;
                capteur.IdErabliere = erabliere.Id;
                context.Add(capteur);
            }
            context.Capteurs.AddRange(capteurs);
            context.SaveChanges();

            foreach (var capteur in capteurs)
            {
                var donnees = fixture.CreateMany<DonneeCapteur>().ToList();
                foreach (var donnee in donnees)
                {
                    donnee.Capteur = capteur;
                    donnee.IdCapteur = capteur.Id;
                    context.Add(donnee);
                }
                context.DonneesCapteur.AddRange(donnees);
            }

            context.SaveChanges();

            var donnewsTrio = fixture.CreateMany<Donnee>().ToList();
            foreach (var donneeTrio in donnewsTrio)
            {
                donneeTrio.Erabliere = erabliere;
                donneeTrio.IdErabliere = erabliere.Id;
                context.Add(donneeTrio);
            }
        }

        return context;
    }
}
