using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Routing;
using CPM.Helper;

namespace System.Web.Mvc.Html
{
    //Ref: http://forums.asp.net/t/1599009.aspx/1?Dropdownlist+Optgroup+with+mvc
    public static class GroupDropListExtensions
    {
        public static string GroupDropList(this HtmlHelper helper, string name, IEnumerable<GroupDropListItem> data, 
            object SelectedValue, object htmlAttributes)
        {
            #region Handle null data
            
            if (data == null && helper.ViewData != null)
                data = helper.ViewData.Eval(name) as IEnumerable<GroupDropListItem>;
            if (data == null) 
                return string.Empty;

            #endregion

            var select = new TagBuilder("select");

            if (htmlAttributes != null)
                select.MergeAttributes(new RouteValueDictionary(htmlAttributes));

            //http://stackoverflow.com/questions/4142986/optgroup-drop-down-support-in-mvc-problems-with-model-binding
            select.GenerateId(name); select.MergeAttribute("name", name);

            var optgroupHtml = new StringBuilder();
            var groups = data.ToList();
            
            #region Iterate through Group

            foreach (var group in data)
            {//Iterate for each Group
                var groupTag = new TagBuilder("optgroup");
                groupTag.Attributes.Add("label", helper.Encode(group.Name));//Set label
                var optHtml = new StringBuilder();
                
                #region Iterate through Group
                
                foreach (var item in group.Items)
                {//Iterate for each item in Group
                    var option = new TagBuilder("option");
                    option.Attributes.Add("value", helper.Encode(item.Value));
                    if (SelectedValue != null && item.Value == SelectedValue.ToString())
                        option.Attributes.Add("selected", "selected");
                    option.InnerHtml = helper.Encode(item.Text);
                    optHtml.AppendLine(option.ToString(TagRenderMode.Normal));
                }

                #endregion

                groupTag.InnerHtml = optHtml.ToString();//Pour innerHTML for a Group
                optgroupHtml.AppendLine(groupTag.ToString(TagRenderMode.Normal));
            }

            #endregion

            select.InnerHtml = optgroupHtml.ToString();// Final inner HTML
            return select.ToString(TagRenderMode.Normal);
        }
    }
}

namespace CPM.Helper
{
    public class GroupDropListItem
    {
        public string Name { get; set; }
        public List<OptionItem> Items { get; set; }
    }

    public class OptionItem
    {
        public string Text { get; set; }
        public string Value { get; set; }
    }
}