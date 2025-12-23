using System.Security.Cryptography;
using System.Text;

namespace Auth.API.Services
{
    public static class TotpService
    {
        // 30-second time step, 6 digits, HMAC-SHA1
        public static string ComputeTotp(string secretBase32, DateTime? now = null, int stepSeconds = 30, int digits = 6)
        {
            var counter = GetCounter(now ?? DateTime.UtcNow, stepSeconds);
            var key = Base32Decode(secretBase32);
            var msg = BitConverter.GetBytes(counter);
            if (BitConverter.IsLittleEndian) Array.Reverse(msg);

            using var hmac = new HMACSHA1(key);
            var hash = hmac.ComputeHash(msg);

            var offset = hash[^1] & 0x0F;
            var binaryCode = ((hash[offset] & 0x7F) << 24)
                            | (hash[offset + 1] << 16)
                            | (hash[offset + 2] << 8)
                            | (hash[offset + 3]);

            var otp = binaryCode % (int)Math.Pow(10, digits);
            return otp.ToString().PadLeft(digits, '0');
        }

        public static bool Verify(string secretBase32, string code, int stepSeconds = 30, int digits = 6, int driftWindows = 1)
        {
            var now = DateTime.UtcNow;
            for (int w = -driftWindows; w <= driftWindows; w++)
            {
                var expected = ComputeTotp(secretBase32, now.AddSeconds(w * stepSeconds), stepSeconds, digits);
                if (expected == code)
                    return true;
            }
            return false;
        }

        public static string GenerateSecret(int bytes = 20)
        {
            var buffer = RandomNumberGenerator.GetBytes(bytes);
            return Base32Encode(buffer);
        }

        public static string BuildOtpAuthUri(string issuer, string accountName, string secretBase32)
        {
            return $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(accountName)}?secret={secretBase32}&issuer={Uri.EscapeDataString(issuer)}&algorithm=SHA1&digits=6&period=30";
        }

        private static long GetCounter(DateTime dt, int stepSeconds)
        {
            var unix = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var elapsed = (long)(dt - unix).TotalSeconds;
            return elapsed / stepSeconds;
        }

        // Simple Base32 (RFC4648) helpers
        private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

        private static byte[] Base32Decode(string input)
        {
            input = input.Trim().Replace(" ", string.Empty).TrimEnd('=');
            int outputLength = input.Length * 5 / 8;
            var result = new byte[outputLength];
            int buffer = 0, bitsLeft = 0, index = 0;
            foreach (var c in input.ToUpperInvariant())
            {
                int val = Alphabet.IndexOf(c);
                if (val < 0) continue;
                buffer = (buffer << 5) | val;
                bitsLeft += 5;
                if (bitsLeft >= 8)
                {
                    bitsLeft -= 8;
                    result[index++] = (byte)(buffer >> bitsLeft);
                    buffer &= (1 << bitsLeft) - 1;
                    if (index >= result.Length) break;
                }
            }
            return result;
        }

        private static string Base32Encode(byte[] data)
        {
            int outputLength = ((data.Length + 4) / 5) * 8;
            var sb = new StringBuilder(outputLength);
            int buffer = 0, bitsLeft = 0;
            foreach (var b in data)
            {
                buffer = (buffer << 8) | b;
                bitsLeft += 8;
                while (bitsLeft >= 5)
                {
                    bitsLeft -= 5;
                    sb.Append(Alphabet[(buffer >> bitsLeft) & 0x1F]);
                }
            }
            if (bitsLeft > 0)
            {
                sb.Append(Alphabet[(buffer << (5 - bitsLeft)) & 0x1F]);
            }
            while (sb.Length % 8 != 0) sb.Append('=');
            return sb.ToString();
        }
    }
}
