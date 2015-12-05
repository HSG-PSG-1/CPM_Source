using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Collections.Specialized;

namespace CPM.Helper
{
    public class Defaults
    {
        public const string baseNamespace = "CPM";
        public static string commonRoot = VirtualPathUtility.ToAbsolute("~/Common");
        public static string contentRoot = VirtualPathUtility.ToAbsolute("~/Content");
        public static string masterLayout = "~/Views/Shared/_SiteMasterLayout.cshtml";
        public static char[] reportDataTrim = new char[] { ',' };

        #region Defaults & Messages
        
        public static int Integer = 0; public static int aNewID = -1;
        public static string String = "";
        public static DateTime DateTime = DateTime.MinValue;
        public static bool Boolean = false;
        public static double Double = -1;
        public static decimal Decimal = 0.0M;
        public static DateTime? NullDate = null;
        public static DateTime minSQLDate = new DateTime(1753, 1, 1);
        public static DateTime maxSQLDate = new DateTime(9999, 12, 31);
        public static DateTime nullDate = new DateTime(1, 1, 1);
        public const string cookieName = "CPMcookie";
        public const string emailCookie = "CPMCookieEmail";
        public const string passwordCookie = "CPMCookiePWD";
        //Validation i.e. Email, regex, etc...
        public const string EmailRegEx = @"^(([A-Za-z0-9]+_+)|([A-Za-z0-9]+\-+)|([A-Za-z0-9]+\.+)|([A-Za-z0-9]+\++))*[A-Za-z0-9]+@((\w+\-+)|(\w+\.))*\w{1,63}\.[a-zA-Z]{2,6}$";
        public const string InvalidEmailPWD = "The email and/or password provided is incorrect.";
        public const string ForgotPWDInvalidEmail = "Email address does not exist.";
        public const string MaxLengthMsg = "Maximum length {1}.";
        public const string InvalidEmailMsg = "Invalid Email.";
        public const string RequiredMsg = "{0} required.", RequiredMsgAppend = " required.";
        
        public const string validatorJQsetting =
        " ignoreTitle: true , focusInvalid: false , focusCleanup: false , onsubmit: true , onkeyup: false "; 
        //onfocusout: function(element) {$(element).valid()} 
        #endregion

        #region Date variables & functions
        
        public const string dtFormat = "MM/dd/yyyy"; // Make sure you use "ci" with it or it'll become MM-dd-yyyy
        public const string dtFormatNeutral = "dd-MMM-yyyy hh:mm:ss"; // to avoid culture
        public const string dtUniFormat = "dd-MM-yyyy";
        public const string dtUniFormat2 = "MMMM dd,yyyy";
        public const string dtTFormat = "MM/dd/yyyy hh:mm:ss"; // Make sure you use "ci" with it or it'll become MM-dd-yyyy
        public static readonly System.Globalization.CultureInfo ci = new System.Globalization.CultureInfo(String.Empty, false);
        public static DateTime getValidDate(DateTime dt)
        {// Check and return a valid(within its range SQL date
            if (dt > maxSQLDate) return maxSQLDate;
            else if (dt < minSQLDate) return minSQLDate;
            else return dt;
        }

        #endregion

        #region Autocomplete list img
        
        public const string AutoCompTitle = "Start typing to search or type space twice to view all";
        public static string lookupList = 
        "<img {0} src=\"" + contentRoot + "/Images/list.gif\" title='Click here to view all or Start typing to search (enter atleast two characters)' />";
        public static string lookupDown = "<img {0} src=\"" + contentRoot + "/Images/down.gif\" title = 'close the list' />";
        // Add this to span if not in js : onclick1=\"$(this).children('img').toggle();\"
        public static string lookupImgBtn ="<span class='lookup' >" +
        string.Format(lookupList, " onclick=\"$('#{0}').autocomplete('search',getStuffedAutoCompVal('#{0}')); \" ") + //$('#{0}').val()
        string.Format(lookupDown, " onclick=\"$('#{0}').autocomplete('close');\" style='display:none' ") + "</span>";

        #endregion

        #region Edit, Cancel & Clip img
        public static string editImg = @"<img src='" + contentRoot + 
            "/Images/edit.gif' title='Click here to edit' border='0'/>";
        public static string clipImg = @"<img src='" + contentRoot + 
            "/Images/clip.gif' title='Click to download' border='0'/>";
        public static string clipImgLink = "<a href=\"{0}\"><img src='" + contentRoot + 
            "/Images/clip.gif' title='Click to download' border='0' target=\"_blank\"/></a>";
        public static string cancelImgOnly = "<img {0} src='" + contentRoot + 
            "/Images/cancel.png' title='Click here to undo' border='0' style='cursor: pointer' />";
        #endregion

        #region Delete img / taconite, link & input
        
        public static string delImgPathTitle = " src='" + contentRoot + 
            "/Images/del.gif' title='Click here to delete' border='0' ";
        
        public static string delImgOnly = @"<img {0} " + delImgPathTitle + " style='cursor: pointer' />";
        public static string delImg = "<img " + delImgPathTitle + " border='0' onclick='return confirmDelete(event);' />";
        public static string delImgForObj(string obj) { return "<img " + delImgPathTitle + " border='0' onclick='return confirmDeleteM(event,\"Are you sure you want to delete this "+ obj + "?\");' />"; }
        public static string delImgForObjKO(string obj)
        {
            return "<img " + delImgPathTitle + " border='0' class='dDialog' " +
                "data-bind='click:function(data,event){if(confirmDeleteM(event,\"Are you sure you want to delete this " + obj + "?\")) $parent.removeSelected($data); return false;}' />";
        }
        public static string delPOSTImg = "<input type='image' " + delImgPathTitle + 
            " border='0' onclick='return confirmDelete(event);' />";
        public static string delImgLink = "<img " + delImgPathTitle + 
            " border='0' onclick=\"javascript:if(confirmDelete(event)){{ {0}; }}\" style='cursor:pointer' />";
        
        //public static string delPOSTImgTACO(string txtID, int txtVal) { return delPOSTImgTACO(txtID, txtVal, null); }
        public static string delPOSTImgTACO(string txtID, int txtVal, string doDelPostFunction = null)
        {
            return string.Format(delImgLink, ("delTR = this.parentNode.parentNode;$(this).toggle();" + 
                (doDelPostFunction ?? "doDelPost") + "('" + txtID + "'," + txtVal + ");"));
        }
        
        #endregion

        #region Operation result variables & functions

        const string oprMsg =
        "<span id='oprResult'>{0}<span class='error'>{1}</span></span><script>$().ready(function() {{showOprResult('#oprResult',{2});}});</script>";
        //const string oprMsgNOTY = "<eval><![CDATA[<script>$().ready(function() {{showNOTY('{0}{1}',{2});}});</script>]]></eval>";
        const string oprMsgNOTY = "showNOTY('{0}{1}',{2});";

        public static string getOprResult(bool result, string msg)
        {//Two in one function - displays Opr success else displays opr unsuccessful a well as its err msg (sent as the second arg)
            return string.Format(oprMsgNOTY,
                result ? "Operation was successful" : "<u>ERROR</u> : ",
                result ? "" : (msg.Length > 0 ? "<br/>" + msg : ""),
                result ? "true" : "false");//result?"1":"0");
        }

        public static string getTaconiteRemoveTR(bool success, string msg, string msgContainer = "msg", bool doCallback = false)
        {
            string callback = doCallback ? ",function(){doFurtherProcessing();}" : "";
            string hide = success ? (".toggle(500" + callback + ")") : "";
            return string.Format(//"<taconite><replaceContent select=\"#{0}\">{1}</replaceContent>" +//<slideDown select=\"#{0}\" value=\"1000\" />
     "<taconite><eval><![CDATA[ try{{{1} $(delTR).effect('highlight',{{}},1000){2};delTR = '';}}catch(e){{;}} ]]> </eval></taconite>",
     msgContainer ?? "msg", msg, hide);
        }

        public static string getTaconiteResult(bool success, string msg, string msgContainer, string callback)
        {
            //return string.Format("<taconite><replaceContent select=\"#{0}\">{1}</replaceContent>" + callback + "</taconite>", msgContainer ?? "msg", msg);
            return string.Format("<taconite><eval><![CDATA[ {0} {1}; ]]> </eval></taconite>", msg, success ? callback : "");
        }

        #endregion

        #region Format, Grouping, Misc
        const string noRecords = "<tr><td colspan=\"{0}\" align=\"center\"> No records found </td></tr>";
        public const string chkCollapse = "return chkCollapse(this);"; // Make sure a table = tblSearch exists and its collapsible

        public static string chkNoRecords(int? count, int colspan){return ((count??0) > 0) ? "" : string.Format(noRecords, colspan);}

        public static string trimLastURLSegment(string url)
        { 
            int segLength = HttpContext.Current.Request.Url.Segments.Length;
            //return if url is empty
            if(segLength <= 1)return url;
            //return trimmed
            url = HttpContext.Current.Request.Url.AbsoluteUri.Replace(HttpContext.Current.Request.Url.Query??"", "");
            return url.Replace(//HttpContext.Current.Request.Url.AbsoluteUri
                HttpContext.Current.Request.Url.Segments[segLength-1],"").Trim(new char[]{'/'});
        }

        public static string formatExceptionDetails(object err, ref string errDetails)
        {
            Exception ex = (Exception)(err ?? new Exception());
            while (ex.InnerException != null) ex = ex.InnerException;//To make sure we reach the innermost exception!

            System.Text.StringBuilder errMsg = new System.Text.StringBuilder();

            //Error-message
            errMsg.Append(
                //"<LINK href=\"" + cssPath + "\" type=\"text/css\" rel=\"stylesheet\">"+
                "<hr /><span class=\"error\">ERROR</span><br>Source : " + ex.Source
                + "<br><b><span class=\"error note\">Message :" + ex.Message + @"</span></b><br/>");

            #region User Details

            if (/*_SessionUsr != null && */ _SessionUsr.ID > 0)
            {//Append only if User exists

                errMsg.Append("<div style=\"padding:15px 25px;\">");
                errMsg.Append("<i>User Information</i>");
                errMsg.Append("<br><br><b>User Email : </b>" + _SessionUsr.Email);
                errMsg.Append("<br><b>Name : </b>" + _SessionUsr.UserName);
                errMsg.Append("<br><b>OrganizationName : </b>" + _SessionUsr.OrgName);
                errMsg.Append("</div>");
            }            
            string datetimeerror = "<b>Error Occur at: </b>" + DateTime.Now.ToString();
            
            errMsg.Append(datetimeerror);

            errMsg.Append("<br><b>IP Address: </b>" + HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"]);
            errMsg.Append("<br><b>URL: </b>" + HttpContext.Current.Request.ServerVariables["HTTP_REFERER"]);
            errMsg.Append("(" + HttpContext.Current.Request.ServerVariables["PATH_INFO"]+")");  
            errMsg.Append("<br><b>Browser: </b>" + HttpContext.Current.Request.ServerVariables["HTTP_USER_AGENT"]);
            #endregion

            #region Show/Hide Techincal details based on config settings
            System.Text.StringBuilder errMsgDetails = new System.Text.StringBuilder();
            //errMsgDetails.Append("<br/><i>InnerException:" + ex.InnerException + "</i><br/>");
            errMsgDetails.Append("<br/><i>TECHNICAL INFO:</i><br/><div style=\"padding:10px 25px;\"><b><u>Stack Trace</u></b>:");
            
            //Set Stacktrace (sometimes InnerException has no stacktrace)
            if (string.IsNullOrEmpty(ex.StackTrace)) ex = (Exception)(err ?? new Exception());

            if(!string.IsNullOrEmpty(ex.StackTrace))errMsgDetails.Append(
               ex.StackTrace.Replace(" at ", "</b></u></span>at<br>").
                             Replace(":line ", "<br>:<span class=\"error\"><u>line<b> ").
                             Replace(baseNamespace + ".", "<span class=\"note \">" + baseNamespace + ".").
                             Replace(") in ", ") in </span><span class=\"smallHeader error blackLabel\"><b>").
                             Replace(".vb", ".vb</b></span>").
                             Replace(".cs", ".cs</b></span>")
                             );
            errMsgDetails.Append("</div>");

            if (Config.ErrorLevelDetail == "1")
                errDetails = errMsgDetails.ToString();
            //errMsg.Append(errMsgDetails.ToString());
            //HttpContext.Current.Response.Write(errorDetails);
            #endregion

            return errMsg.ToString();
        }

        public static IEnumerable<int> stringToIntList(string str)
        {
            //http://stackoverflow.com/questions/1763613/convert-comma-separated-string-of-ints-to-int-array
            //Easy but crashable: int[] nos = (das.ClaimNos ?? "").Split(',').Select<string, int>(int.Parse).ToArray();
            
            if (String.IsNullOrEmpty(str))
                yield break;

            foreach (var s in str.Split(','))
            {
                int num;
                if (int.TryParse(s, out num))
                    yield return num;
            }
        }

        public static IEnumerable<string> stringToStrList(string str){return (str??"").Split(',');}

        public static string getGroupOnSortTbody(object item, string prevTxt, StringDictionary sortGrps, out string val,
            int ID, ref int count, int columns, ref string prevID)
        {
            string sort = HttpContext.Current.Request.QueryString["sidx"]; bool isNew = false;
            System.Text.StringBuilder tBodyStr = new System.Text.StringBuilder();
            //ignore other sorts
            if (string.IsNullOrEmpty(sort) || !sortGrps.ContainsKey(sort))
            { val = ""; count = 0; return string.Empty; }

            #region Get the value of property dynamically
            val = (item.GetType().GetProperty(sort).GetValue(item, null) ?? "").ToString();
            if (!string.IsNullOrEmpty(val) && item.GetType().GetProperty(sort).PropertyType == DateTime.GetType())
            {//HT: Handle special case for datetime field
                val = (DateTime.Parse(val)).ToString(Defaults.dtFormat, Defaults.ci);
            }
            #endregion

            isNew = (val != prevTxt);
            if (isNew)
            {//Its a new group so append/prepend tbody for grouping
                count = 0; string trID = "hdr" + ID.ToString(); prevID = trID;
                if (prevTxt.Length > 0) tBodyStr.Append(@"</tbody>");

                #region append tbody & script
                tBodyStr.Append("<tbody><tr id=\"" + trID + "\"");
                tBodyStr.Append(" onclick=\"$('#" + trID + "').siblings().toggle();\" class=\"grpTR\">");
                tBodyStr.Append("<td colspan=\"" + (columns - 1) + "\"><i>" + sortGrps[sort] + ": " + val + "</i></td>");
                tBodyStr.Append("<td class=\"grpTD\" id=\"td" + trID + "\">");
                tBodyStr.Append("<script>$('#td" + trID + "').html('Count: ' + $('#" + trID + "').siblings().length);</script>");
                tBodyStr.Append("</td></tr>");
                #endregion
            }
            else
                count += 1;

            return tBodyStr.ToString();
        }

        public static string getAbsSiteURL(string uri)
        {
            Uri url = new Uri(uri);
            return url.Scheme + Uri.SchemeDelimiter + url.Host + (url.IsDefaultPort ? "" : (":" + url.Port));                       
        }

        #endregion
    }
}