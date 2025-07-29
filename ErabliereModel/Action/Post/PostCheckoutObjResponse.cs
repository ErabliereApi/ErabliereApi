namespace ErabliereApi.Donnees.Action.Post
{
    /// <summary>
    /// Représente la réponse d'un objet de checkout
    /// </summary>
    public class PostCheckoutObjResponse
    {
        /// <summary>
        /// L'URL de la session de checkout
        /// </summary>
        public string Url { get; set; } = string.Empty;
    }
}