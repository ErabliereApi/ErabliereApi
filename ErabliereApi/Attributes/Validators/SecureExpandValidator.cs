
using ErabliereApi.Donnees;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Query.Validator;

namespace ErabliereApi.Attributes.Validators;
/// <summary>
/// Validator qui vérifie que la requête Expand ne contient pas Aletes, Documentations ou Notes.
/// </summary>
public class SecureExpandValidator : SelectExpandQueryValidator
{
    private static readonly string[] _forbiddenExpands = [
        nameof(Erabliere.Alertes),
        nameof(Erabliere.Documentations),
        nameof(Erabliere.Notes),
        nameof(Erabliere.CustomerErablieres)
    ];

    /// <inheritdoc />
    public override void Validate(SelectExpandQueryOption selectExpandQueryOption, ODataValidationSettings validationSettings)
    {
        string expand = selectExpandQueryOption.RawExpand;

        if (expand != null) 
        {
            for (int i = 0; i < _forbiddenExpands.Length; i++)
            {
                string forbiddenExpand = _forbiddenExpands[i];
                
                if (expand.Contains(forbiddenExpand, StringComparison.OrdinalIgnoreCase))
                {
                    throw new Microsoft.OData.ODataException($"La requête sur {forbiddenExpand} n'est pas permise");
                }
            }
        }

        base.Validate(selectExpandQueryOption, validationSettings);
    }
}