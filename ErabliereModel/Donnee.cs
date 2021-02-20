using ErabliereApi.Donnees.Action.Post;
using System;
using System.Diagnostics.CodeAnalysis;

namespace ErabliereApi.Donnees
{
    public class Donnee : IIdentifiable<int?, Donnee>
    {
        /// <summary>
        /// L'id de l'occurence
        /// </summary>
        public int? Id { get; set; }

        /// <summary>
        /// Date de la transaction
        /// </summary>
        public DateTime? D { get; set; }

        /// <summary>
        /// Temperature en dixi�me de celcius
        /// </summary>
        public short? T { get; set; }

        /// <summary>
        /// Niveau bassin en pourcentage
        /// </summary>
        public short? NB { get; set; }

        /// <summary>
        /// Vaccium en dixi�me de HG
        /// </summary>
        public short? V { get; set; }

        /// <summary>
        /// Id de dl'�rabli�re relier a cette donn�e
        /// </summary>
        public int? IdErabliere { get; set; }

        /// <summary>
        /// Interval de date des donn�es aliment�. Utiliser pour optimiser le nombre de donn�es enregistrer
        /// 
        /// Plus grand interval d'alimentation de cette donn�e, en seconde
        /// </summary>
        public int? PI { get; set; }

        /// <summary>
        /// Nombre d'occurence enrgistrer de cette donn�e
        /// </summary>
        public int Nboc { get; set; }

        /// <summary>
        /// Id donn�e pr�c�dente
        /// </summary>
        public int? Iddp { get; set; }

        /// <inheritdoc />
        public int CompareTo([AllowNull] Donnee other)
        {
            if (other == default)
            {
                return 1;
            }

            if (D.HasValue == false)
            {
                return other.D.HasValue ? -1 : 0;
            }

            return D.Value.CompareTo(other.D);
        }

        /// <summary>
        /// Indique si la donn�e en param�tre est dans le future et poss�de les m�me valeur pour
        /// le niveau bassin, la temp�rature et le vaccium
        /// </summary>
        /// <param name="donnee">Une donn�e</param>
        public bool IdentiqueMemeLigneDeTemps(PostDonnee donnee) => donnee.NB == NB &&
                                                                    donnee.T == T &&
                                                                    donnee.V == V &&
                                                                    D.HasValue && donnee.D.HasValue &&
                                                                    D.Value < donnee.D.Value;
    }
}
