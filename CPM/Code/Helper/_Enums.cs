#region Using Directives
using System;
using System.Data;
using System.Configuration;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
#endregion //Using Directives
namespace CPM.Helper
{
    public struct _Enums
    {
        /// <summary>
        /// Generic function to parse enum to string
        /// http://stackoverflow.com/questions/79126/create-generic-method-constraining-t-to-an-enum
        /// </summary>
        /// <typeparam name="T"> Enum Type</typeparam>
        /// <param name="enumString">string value</param>
        /// <returns></returns>
        public static T ParseEnum<T>(object enumString) where T : struct // enum 
        {
            if (enumString == null || String.IsNullOrEmpty(enumString.ToString()) || !typeof(T).IsEnum)
                throw new Exception("Type given must be an Enum");
            try
            {
                return (T)Enum.Parse(typeof(T), enumString.ToString().Replace(" ","_"),true);
            }
            catch (Exception ex)
            {
                return default(T);
            }
        }
    }
}