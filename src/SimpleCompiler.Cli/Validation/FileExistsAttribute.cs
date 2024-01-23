using System.ComponentModel.DataAnnotations;

namespace SimpleCompiler.Cli.Validation;

public class FileExistsAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string str || !File.Exists(str))
        {
            return new ValidationResult("Provided file does not exist.");
        }

        return ValidationResult.Success;
    }
}
