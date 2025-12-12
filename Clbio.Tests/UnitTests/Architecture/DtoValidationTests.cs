using Clbio.Application.DTOs.V1.Base; // RequestDtoBase'in olduğu yer
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Clbio.Tests.UnitTests.Architecture
{
    public class DtoValidationTests
    {
        [Fact]
        public void All_String_Properties_In_Input_Dtos_Should_Have_Length_Constraint()
        {
            // 1. get assembly at application layer
            var assembly = typeof(RequestDtoBase).Assembly;

            // 2. find classes that end with "Dto"
            var inputDtos = assembly.GetTypes()
                .Where(t => t.Name.EndsWith("Dto") && t.IsClass && !t.IsAbstract)
                // 1. ignore those starting with 'Read'
                .Where(t => !t.Name.StartsWith("Read"))
                // 2. ignore those ending with 'Response'
                .Where(t => !t.Name.EndsWith("ResponseDto"))
                .Where(t => !t.Name.Equals("ExternalUserInfoDto"))
                //.Where(t => typeof(RequestDtoBase).IsAssignableFrom(t)) 
                .ToList();

            var failingProperties = new List<string>();

            foreach (var dto in inputDtos)
            {
                // 3. find string public props
                var stringProperties = dto.GetProperties()
                    .Where(p => p.PropertyType == typeof(string) && p.CanWrite);

                foreach (var prop in stringProperties)
                {
                    // 4. check if it has MaxLength or StringLength attributes
                    var hasMaxLength = prop.GetCustomAttribute<MaxLengthAttribute>() != null;
                    var hasStringLength = prop.GetCustomAttribute<StringLengthAttribute>() != null;

                    if (!hasMaxLength && !hasStringLength)
                    {
                        failingProperties.Add($"{dto.Name}.{prop.Name}");
                    }
                }
            }

            if (failingProperties.Count != 0)
            {
                throw new Xunit.Sdk.XunitException(
                    $"Security Risk: The following DTO string properties are missing [MaxLength] or [StringLength] attributes:\n" +
                    string.Join("\n", failingProperties));
            }
        }
    }
}