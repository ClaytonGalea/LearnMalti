using System.Security.Cryptography;
using System.Text;

namespace LearnMalti.Services
{
    public class PlayerCodeGenerator
    {
        //Prefix that will always be addedat the begenning of the generated ID
        private const string Prefix = "P-";
        //The allowed characters in the generated ID
        private const string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        public static string Generate(int length = 8)
        {
            //Creates a byte array with the required size
            //These bytes will be used to pick the characters
            byte[] randomBytes = RandomNumberGenerator.GetBytes(length);

            //Prepare a string builder to assemble the result
            var result = new StringBuilder(length);

            //Convert each byte to a character from the allowed set
            foreach (var b in randomBytes)
            {
                int index = b % Chars.Length;
                result.Append(Chars[index]);
            }

            return Prefix + result.ToString();
        }
    }
}
