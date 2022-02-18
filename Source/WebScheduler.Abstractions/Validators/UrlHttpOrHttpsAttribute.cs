namespace WebScheduler.Abstractions.Validators;
using System;
using System.ComponentModel.DataAnnotations;

/// <summary>
/// Valids if a value is HTTP or HTTPS url.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class UrlHttpOrHttpsAttribute : ValidationAttribute
{
    /// <summary>
    /// Default constructor.
    /// </summary>
    public UrlHttpOrHttpsAttribute()
    {
        this.ErrorMessage = "The Url is invalid. Only HTTP and HTTPS schems are allowed.";
        this.innerAttribute = new RequiredAttribute();
    }

    private readonly RequiredAttribute innerAttribute;

    /// <inheritdoc/>
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null)
        {
            return new ValidationResult(this.ErrorMessage);
        }
        if (this.innerAttribute.IsValid(value) && value is string valueAsString &&
            (valueAsString.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || valueAsString.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
        {
            return ValidationResult.Success;
        }
        return new ValidationResult(this.ErrorMessage);
    }
}
