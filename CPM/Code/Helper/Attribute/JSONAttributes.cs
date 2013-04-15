using System;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using System.Text.RegularExpressions;

namespace CPM.Helper
{
    public class FromJsonAttribute : CustomModelBinderAttribute
    {
        private readonly static JavaScriptSerializer serializer = new JavaScriptSerializer();

        public override IModelBinder GetBinder()
        {
            return new JsonModelBinder();
        }

        private class JsonModelBinder : IModelBinder
        {
            public object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext)
            {
                var stringified = controllerContext.HttpContext.Request[bindingContext.ModelName];
                if (string.IsNullOrEmpty(stringified))
                    return null;

                // Special case for dates: http://forums.asp.net/t/1070058.aspx/1
                // Replace - /Date(123)/ with "\/Date(123)\/"
                //stringified = stringified.Replace("/Date(", "\"\\/Date(").Replace(")/",")\\/\"");
                //stringified = stringified.Replace("/Date(", "\\\"\\\\/Date(").Replace(")/", ")\\\\/\\\"");

                //http://stackoverflow.com/questions/10890682/c-asp-net-json-date-deserialize
                MatchEvaluator matchEvaluator = ConvertJsonDateToDateString;
                var reg = new Regex(@".Date\([-]*(\d+)\)/");//.Date\(\d+\)\/.
                stringified = reg.Replace(stringified, matchEvaluator);

                object result = serializer.Deserialize(stringified, bindingContext.ModelType);
                return result;
            }

            public static string ConvertJsonDateToDateString(Match m)
            {
                var dt = new DateTime(1970, 1, 1);
                dt = dt.AddMilliseconds(long.Parse(m.Groups[1].Value));
                dt = dt.ToLocalTime();
                var result = dt.ToString("yyyy-MM-dd HH:mm:ss");
                return result;
            }
        }
    }
}