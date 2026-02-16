using Application.ContentTypes;
using Domain.Fields;

namespace Integration.Tests.Helpers;

public class TestDataHelper
{
    public static CreateContentTypeCommand CreateValidContentTypeCommand(
        string name = "TestType"
    ) =>
        new CreateContentTypeCommand(
            name,
            new List<CreateFieldDto>
            {
                new CreateFieldDto(
                    Name: "Title",
                    Type: FieldTypes.Text,
                    IsRequired: true,
                    ValidationRules: new List<CreateValidationRuleDto>
                    {
                        new CreateValidationRuleDto(
                            Type: "MaximumLengthValidationRule",
                            Parameters: new Dictionary<string, object> { { "max-length", 256 } }
                        ),
                    }
                ),
                new CreateFieldDto(
                    Name: "Body",
                    Type: FieldTypes.Text,
                    IsRequired: false,
                    ValidationRules: new List<CreateValidationRuleDto>
                    {
                        new CreateValidationRuleDto(
                            Type: "MaximumLengthValidationRule",
                            Parameters: new Dictionary<string, object> { { "max-length", 256 } }
                        ),
                    }
                ),
                new CreateFieldDto(
                    Name: "Description",
                    Type: FieldTypes.Text,
                    IsRequired: false,
                    ValidationRules: null
                ),
            }
        );

    public static CreateContentTypeCommand CreateInvalidContentTypeCommand() =>
        new CreateContentTypeCommand(
            "", // Empty name - invalid
            new List<CreateFieldDto>() // Empty fields - invalid
        );
}
