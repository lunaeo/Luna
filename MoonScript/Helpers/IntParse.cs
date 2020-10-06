namespace MoonScript.Helpers
{
    internal partial class Helpers
    {
        internal static int IntParse(string value)
        {
            var result = 0;

            for (var i = 0; i < value.Length; i++)
                result = 10 * result + (value[i] - 48);

            return result;
        }
    }
}