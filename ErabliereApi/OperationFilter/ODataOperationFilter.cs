using Microsoft.AspNetCore.OData.Query;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text.Json.Nodes;

namespace ErabliereApi.OperationFilter;

/// <summary>
/// Operation filter permettant d'ajouter les paramètres de OData à la page swagger
/// </summary>
public class ODataOperationFilter : IOperationFilter
{
    /// <summary>
    /// Nom de l'exemple vide
    /// </summary>
    public const string EMPTY_EXAMPLE_NAME = "empty";

    /// <inheritdoc />
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var mInfo = context.MethodInfo;

        var enableQueryAttributes = mInfo.GetCustomAttributes(true).OfType<EnableQueryAttribute>();

        var hasODataAttribute = mInfo.DeclaringType?.GetCustomAttributes(true).OfType<ODataOperationFilter>().Any() == true ||
                                enableQueryAttributes.Any();

        if (hasODataAttribute)
        {
            AddODataParameter("$select", format: "string", type: JsonSchemaType.String, examples: GetSelectExamples());
            AddODataParameter("$filter", examples: GetFilterExamples());
            AddODataParameter("$top", format: "int32", type: JsonSchemaType.Integer, examples: GetTopExamples(), defaultValue: "10");
            AddODataParameter("$skip", format: "int32", type: JsonSchemaType.Integer, examples: GetSkipExamples());
            AddODataParameter("$count", format: "boolean", type: JsonSchemaType.Boolean, defaultValue: "false");

            if (ExpandEnabled(enableQueryAttributes))
            {
                AddODataParameter("$expand", examples: GetExpandExamples());
            }

            AddODataParameter("$orderby", format: "string", type: JsonSchemaType.String, GetOrderByExamples());
        }

        void AddODataParameter(string name, string format = "expression", JsonSchemaType type = JsonSchemaType.String, IDictionary<string, IOpenApiExample>? examples = default, string? defaultValue = null)
        {
            if (operation.Parameters == null)
            {
                operation.Parameters = new List<IOpenApiParameter>();
            }

            if (operation.Parameters.Any(p => p.Name == name))
            {
                return;
            }

            operation.Parameters.Add(new OpenApiParameter
            {
                Name = name,
                In = ParameterLocation.Query,
                Schema = new OpenApiSchema
                {
                    Format = format,
                    Type = type
                },
                Examples = examples,
                Example = defaultValue != null ? JsonNode.Parse(defaultValue) : null
            });
        }
    }

    private IDictionary<string, IOpenApiExample>? GetOrderByExamples()
    {
        var examples = new Dictionary<string, IOpenApiExample>
            {
                { EMPTY_EXAMPLE_NAME, new OpenApiExample { Value = JsonNode.Parse("\"\"") } },
                { "asc", new OpenApiExample { Value = JsonNode.Parse("\"propertyName\"") } },
                { "desc", new OpenApiExample { Value = JsonNode.Parse("\"propertyName desc\"") } },
                { "complexe sort", new OpenApiExample { Value = JsonNode.Parse("\"propertyOne asc, propertyTwo desc\"") } }
            };

        return examples;
    }

    private IDictionary<string, IOpenApiExample>? GetExpandExamples()
    {
        var examples = new Dictionary<string, IOpenApiExample>
            {
                { EMPTY_EXAMPLE_NAME, new OpenApiExample { Value = JsonNode.Parse("\"\"") } },
                { "expand one property", new OpenApiExample { Value = JsonNode.Parse("\"propertyName\"") } },
                { "expand multiple property", new OpenApiExample { Value = JsonNode.Parse("\"propertyOne,propertyTwo\"") } },
                { "expand nested property", new OpenApiExample { Value = JsonNode.Parse("\"modelSourceProperty/childOneProperty/childTwoProperty\"") } }
            };

        return examples;
    }

    private IDictionary<string, IOpenApiExample>? GetSkipExamples()
    {
        var examples = new Dictionary<string, IOpenApiExample>
            {
                { EMPTY_EXAMPLE_NAME, new OpenApiExample { Value = JsonNode.Parse("\"\"") } },
                { "skip 10", new OpenApiExample { Value = JsonNode.Parse("10") } }
            };

        return examples;
    }

    private IDictionary<string, IOpenApiExample>? GetTopExamples()
    {
        var examples = new Dictionary<string, IOpenApiExample>
            {
                { EMPTY_EXAMPLE_NAME, new OpenApiExample { Value = JsonNode.Parse("\"\"") } },
                { "take 10", new OpenApiExample { Value = JsonNode.Parse("10") } }
            };

        return examples;
    }

    private IDictionary<string, IOpenApiExample>? GetFilterExamples()
    {
        var examples = new Dictionary<string, IOpenApiExample>
            {
                { EMPTY_EXAMPLE_NAME, new OpenApiExample { Value = JsonNode.Parse("\"\"") } },
                { "equal", new OpenApiExample { Value = JsonNode.Parse("\"propertyName eq 'Some value'\"") } },
                { "and", new OpenApiExample { Value = JsonNode.Parse("\"propertyOne eq 'Some value' and propertyTwo eq 'other Value'\"") } },
                { "or", new OpenApiExample { Value = JsonNode.Parse("\"propertyOne eq 'Some value' or propertyTwo eq 'other Value'\"") } },
                { "less than", new OpenApiExample { Value = JsonNode.Parse("\"propertyName lt 610\"") } },
                { "greater than", new OpenApiExample { Value = JsonNode.Parse("\"propertyName gt 610\"") } },
                { "less or equal than", new OpenApiExample { Value = JsonNode.Parse("\"propertyName le 610\"") } },
                { "greater or equal than", new OpenApiExample { Value = JsonNode.Parse("\"propertyName ge 610\"") } },
                { "not equal", new OpenApiExample { Value = JsonNode.Parse("\"propertyName ne 610\"") } },
                { "endswith", new OpenApiExample { Value = JsonNode.Parse("\"endwith(propertyName, 'value') eq true\"") } },
                { "startswith", new OpenApiExample { Value = JsonNode.Parse("\"startswith(propertyName, 'value') eq true\"") } },
                { "contains", new OpenApiExample { Value = JsonNode.Parse("\"contains(propertyName, 'value') eq true\"") } },
                { "indexof", new OpenApiExample { Value = JsonNode.Parse("\"indexof(propertyName, 'VALUE') eq 0\"") } },
                { "replace", new OpenApiExample { Value = JsonNode.Parse("\"replace(propertyName, 'VALUE', 'VALUEREPLACED') eq 'OTHER VALUE'\"") } },
                { "substring", new OpenApiExample { Value = JsonNode.Parse("\"substring(propertyName, 'OTHERVALUE') eq 'VALUE'\"") } },
                { "tolower", new OpenApiExample { Value = JsonNode.Parse("\"tolower(propertyName) eq 'VALUE'\"") } },
                { "toupper", new OpenApiExample { Value = JsonNode.Parse("\"toupper(propertyName) eq 'VALUE'\"") } },
                { "trim", new OpenApiExample { Value = JsonNode.Parse("\"trim(propertyName) eq 'VALUE'\"") } },
                { "concat", new OpenApiExample { Value = JsonNode.Parse("\"concat(concat(PropertyText1, ', '), PropertyText2) eq 'value1, value2'\"") } },
                { "floor", new OpenApiExample { Value = JsonNode.Parse("\"floor(propertyDecimal) eq 1\"") } },
                { "ceiling", new OpenApiExample { Value = JsonNode.Parse("\"ceiling(propertyDecimal) eq 1\"") } },
            };

        return examples;
    }

    private IDictionary<string, IOpenApiExample>? GetSelectExamples()
    {
        var examples = new Dictionary<string, IOpenApiExample>
            {
                { EMPTY_EXAMPLE_NAME, new OpenApiExample { Value = JsonNode.Parse("\"\"") } },
                { "select one property", new OpenApiExample { Value = JsonNode.Parse("\"propertyName\"") } },
                { "select multiple property", new OpenApiExample { Value = JsonNode.Parse("\"propertyOne,propertyTwo\"") } }
            };

        return examples;
    }

    private bool ExpandEnabled(IEnumerable<EnableQueryAttribute> enableQueryAttributes)
    {
        var enableQueryAttribute = enableQueryAttributes.Single();

        return enableQueryAttribute.MaxExpansionDepth > 0;
    }
}
