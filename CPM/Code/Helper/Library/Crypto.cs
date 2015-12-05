using System;
using System.Web;
using System.Text;

namespace CPM.Helper
{
    // class for security helper methods
    public class Crypto
    {
        /// <summary>
        /// function to encode/decode string 
        /// </summary>
        /// <param name="textVal">text to process</param>
        /// <param name="doEncrypt">flag to encode/decode</param>
        /// <returns>processed string</returns>
        public static string EncodeStr(string textVal, bool doEncrypt)
        {
            if (string.IsNullOrEmpty(textVal)) //return if empty
                return string.Empty;

            int incr = (doEncrypt? 1: -1);
            int seedTrim = 9;//the added byte value shud not be > 9
            int seed = textVal.Length % seedTrim * incr;
            //make sure the seed <> 0
            while (seed == 0)
            {
                seedTrim = seedTrim - 1;
                seed = textVal.Length % seedTrim * incr;
            }
            //Get bytes
            byte[] byts = Encoding.Unicode.GetBytes(textVal);
            int pos = 0;
            //ENCODE
            while (pos < byts.Length)
            {
                byts[pos] = Convert.ToByte(Convert.ToInt16(byts[pos]) + seed);                
                pos += 2;
            }
            return Encoding.Unicode.GetString(byts);
        }

        /// <summary>
        /// function to encode/decode string for HTML
        /// </summary>
        /// <param name="textVal">text to process</param>
        /// <param name="doEncode">flag to encode/decode</param>
        /// <returns>processed string</returns>
        public static string HTMLEncodeStr(string textVal, bool doEncode)
        {
            if (string.IsNullOrEmpty(textVal)) // return if empty string
                return string.Empty;

            return (doEncode? HttpUtility.HtmlEncode(textVal).Replace('&', '$').Replace(';', '!'): 
                HttpUtility.HtmlDecode(textVal).Replace("$", "&").Replace("!", ";"));
        }
    }
}
