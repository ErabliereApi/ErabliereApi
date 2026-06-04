using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Query.Validator;
using Microsoft.OData;

namespace ErabliereApi.Validators;

/// <summary>
/// Custom validator to avoid returning exception in HTTP response
/// </summary>
public class CustomODataQueryValidator : ODataQueryValidator
{
    /// <inheritdoc />
    public override void Validate(ODataQueryOptions options, ODataValidationSettings validationSettings)
    {
        try
        {
            // This runs the built-in parser AND underlying validators
            base.Validate(options, validationSettings);
        }
        catch (ODataException ex)
        {
            // Throw an exception that escapes the OData layer and triggers your GlobalExceptionHandler
            throw new BadHttpRequestException($"OData query validation failed: {ex.Message}", ex);
        }
    }
}