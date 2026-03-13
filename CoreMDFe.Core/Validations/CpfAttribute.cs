using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace CoreMDFe.Core.Validations
{
    public class CpfAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var cpf = value as string;

            if (string.IsNullOrWhiteSpace(cpf))
                return ValidationResult.Success; // Se for vazio, a tag [Required] é quem deve barrar, não esta.

            // Remove formatação caso exista
            cpf = new string(cpf.Where(char.IsDigit).ToArray());

            if (cpf.Length != 11)
                return new ValidationResult(ErrorMessage ?? "O CPF deve ter 11 dígitos.");

            // Rejeita CPFs com todos os números iguais (ex: 111.111.111-11) - O cálculo matemático passa neles, mas são inválidos.
            if (cpf.Distinct().Count() == 1)
                return new ValidationResult(ErrorMessage ?? "CPF inválido.");

            // Cálculo do 1º Dígito Verificador
            int[] multiplicador1 = new int[9] { 10, 9, 8, 7, 6, 5, 4, 3, 2 };
            string tempCpf = cpf.Substring(0, 9);
            int soma = 0;

            for (int i = 0; i < 9; i++)
                soma += int.Parse(tempCpf[i].ToString()) * multiplicador1[i];

            int resto = soma % 11;
            resto = resto < 2 ? 0 : 11 - resto;

            string digito = resto.ToString();
            tempCpf += digito;

            // Cálculo do 2º Dígito Verificador
            int[] multiplicador2 = new int[10] { 11, 10, 9, 8, 7, 6, 5, 4, 3, 2 };
            soma = 0;

            for (int i = 0; i < 10; i++)
                soma += int.Parse(tempCpf[i].ToString()) * multiplicador2[i];

            resto = soma % 11;
            resto = resto < 2 ? 0 : 11 - resto;
            digito += resto.ToString();

            // Verifica se os dígitos calculados batem com os digitados
            if (!cpf.EndsWith(digito))
                return new ValidationResult(ErrorMessage ?? "O CPF informado é inválido.");

            return ValidationResult.Success;
        }
    }
}