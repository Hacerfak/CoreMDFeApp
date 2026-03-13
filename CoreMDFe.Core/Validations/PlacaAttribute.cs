using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace CoreMDFe.Core.Validations
{
    public class PlacaAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var placa = value as string;

            if (string.IsNullOrWhiteSpace(placa))
                return ValidationResult.Success;

            // Remove o traço, caso o utilizador tenha digitado (ex: ABC-1234) e espaços
            placa = placa.Replace("-", "").Replace(" ", "").Trim();

            // Expressão Regular que aceita o padrão Antigo (ABC1234) e o Mercosul (ABC1D23)
            var regex = new Regex(@"^[a-zA-Z]{3}[0-9][A-Za-z0-9][0-9]{2}$");

            if (!regex.IsMatch(placa))
            {
                return new ValidationResult(ErrorMessage ?? "Formato de placa inválido. Use ABC1234 ou ABC1D23.");
            }

            return ValidationResult.Success;
        }
    }
}