using System.Text;

namespace InvoiceAPI.Utils
{
    public class RandomUtil
    {
        private const string Numbers = "0123456789";

        /// <summary>
        /// 普通的隨機數字產生器、跟安全性有關不建議使用
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public static string GetCommonRandomNumber(int length = 6)
        {
            Random rn = new Random();
            return new string(Enumerable.Repeat(Numbers, length)
                .Select(s => s[rn.Next(s.Length)])
                .ToArray());
        }

        /// <summary>
        /// 多執行緒安全、密碼用的隨機數字產生器
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public static string GetRandomNumber(int length = 6)
        {
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            var byteArray = new byte[length];
            rng.GetBytes(byteArray);
            StringBuilder result = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                // 確保數字均勻分布
                result.Append(Numbers[byteArray[i] % Numbers.Length]);
            }
            return result.ToString();
        }
    }
}
