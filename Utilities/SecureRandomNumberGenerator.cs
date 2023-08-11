using System;
using System.Collections.Generic;
using System.Text;

namespace Utilities
{
    using System;
    using System.Security.Cryptography;

    public class SecureRandomNumberGenerator
    {
        public static byte[] GenerateRandomBytes(int length)
        {
            byte[] randomBytes = new byte[length];
            using (RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider())
            {
                rngCsp.GetBytes(randomBytes);
            }
            return randomBytes;
        }

        public static int GenerateRandomInt(int minValue, int maxValue)
        {
            if (minValue >= maxValue)
            {
                throw new ArgumentException("minValue must be less than maxValue");
            }

            byte[] randomBytes = GenerateRandomBytes(sizeof(int));
            int randomInt = BitConverter.ToInt32(randomBytes, 0);

            return Math.Abs(randomInt % (maxValue - minValue)) + minValue;
        }
    }
}
