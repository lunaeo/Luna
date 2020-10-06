using System;
using System.IO;
using System.Linq;

namespace LunaServer.Utilities
{
    public static class UniqueNameGenerator
    {
        private static readonly string[] Adjectives;
        private static readonly string[] Animals;
        private static readonly SecureRandomNumberGenerator CSPRNG;

        static UniqueNameGenerator()
        {
            CSPRNG = new SecureRandomNumberGenerator();
            Adjectives = File.ReadAllLines(Path.Combine("data", "meta", "adjectives.txt"));
            Animals = File.ReadAllLines(Path.Combine("data", "meta", "animals.txt"));
        }

        public static string Generate()
        {
            return CSPRNG.Next(0, 1) == 1 ? Animals[CSPRNG.Next(0, Animals.Length)] :
                Adjectives[CSPRNG.Next(0, Adjectives.Length)].FirstCharToUpper() +
                Animals[CSPRNG.Next(0, Animals.Length)].FirstCharToUpper();
        }
    }

    public static class StringExtensions
    {
        public static string FirstCharToUpper(this string input)
        {
            if (input is null)
                throw new ArgumentNullException(nameof(input));

            if (string.IsNullOrWhiteSpace(input))
                throw new ArgumentNullException($"{nameof(input)} cannot be empty");

            return input.First().ToString().ToUpper() + input.Substring(1);
        }
    }
}