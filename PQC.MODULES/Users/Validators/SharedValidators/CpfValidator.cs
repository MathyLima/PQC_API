using System.Text.RegularExpressions;

namespace PQC.MODULES.Users.Validators.SharedValidators
{
    public class CpfValidator
    {
        public static bool IsValidCpf(string cpf)
        {
            if (string.IsNullOrWhiteSpace(cpf))
                return false;

            // Remove caracteres não numéricos
            cpf = Regex.Replace(cpf, @"\D", "");

            // Verifica se tem 11 dígitos
            if (cpf.Length != 11)
                return false;

            // Elimina CPFs com todos os dígitos iguais
            if (cpf.Distinct().Count() == 1)
                return false;

            // Converte para array de inteiros uma única vez
            ReadOnlySpan<int> digits = cpf.Select(c => c - '0').ToArray();

            // Calcula o primeiro dígito verificador
            int firstDigit = CalculateDigit(digits[..9], 10);
            if (digits[9] != firstDigit)
                return false;

            // Calcula o segundo dígito verificador
            int secondDigit = CalculateDigit(digits[..10], 11);
            return digits[10] == secondDigit;
        }

        private static int CalculateDigit(ReadOnlySpan<int> digits, int startWeight)
        {
            int sum = 0;
            for (int i = 0; i < digits.Length; i++)
            {
                sum += digits[i] * (startWeight - i);
            }

            int remainder = (sum * 10) % 11;
            return remainder == 10 ? 0 : remainder;
        }
    }
}
