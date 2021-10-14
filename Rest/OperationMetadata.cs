using System.Linq;
using System.Text;
using Microsoft.OpenApi.Models;

namespace Reductech.EDR.Connectors.Rest
{

/// <summary>
/// Metadata for an API operation
/// </summary>
public sealed record OperationMetadata(
    string ServiceName,
    OpenApiDocument OpenApiDocument,
    string Path,
    string ServerUrl,
    OpenApiPathItem PathItem,
    OpenApiOperation Operation,
    OperationType OperationType)
{
    /// <inheritdoc />
    public override string ToString()
    {
        return Name;
    }

    /// <summary>
    /// Generated name for this operation
    /// </summary>
    public string Name
    {
        get
        {
            string baseString;

            if (!string.IsNullOrWhiteSpace(Operation.OperationId))
                baseString = ServiceName + '_' + Operation.OperationId;
            else
                baseString = ServiceName + '_' + Path + '_' + OperationType;

            var sb = new StringBuilder();

            var previousWasUnderscore = true;

            foreach (var c in baseString)
            {
                if (char.IsLetterOrDigit(c))
                {
                    sb.Append(c);
                    previousWasUnderscore = false;
                }
                else if (!previousWasUnderscore)
                {
                    sb.Append('_');
                    previousWasUnderscore = true;
                }
            }

            return sb.ToString();
        }
    }
}

}
