using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Reflection;
using CPM.DAL;
using CPM.Services;
using System.Text.RegularExpressions;
using System.IO.Compression;

namespace CPM.Helper
{
    #region Validation
    /* url: http://weblogs.asp.net/scottgu/archive/2010/01/15/asp-net-mvc-2-model-validation.aspx */
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public class EmailAddressAttribute : DataTypeAttribute
    {
        private readonly Regex regex = new Regex(@"\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*", RegexOptions.Compiled);

        public EmailAddressAttribute() : base(DataType.EmailAddress) { ;}

        public override bool IsValid(object value)
        {
            string str = Convert.ToString(value, CultureInfo.CurrentCulture);
            if (string.IsNullOrEmpty(str))
                return true;

            Match match = regex.Match(str);
            return ((match.Success && (match.Index == 0)) && (match.Length == str.Length));
        }
    }

    /* http://stackoverflow.com/questions/2280539/custom-model-validation-of-dependent-properties-using-data-annotations */
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public sealed class DuplicateMatchAttribute : ValidationAttribute
    {
        #region Variables
        private Target targt;
        private Field fld;
        //public MasterService.Table MasterType;

        public enum Target : int
        {
            Item,
            Org,
            Location,
            User,
            Master,
            Role
        }

        public enum Field : int
        {
            ItemCode,
            CustCode,
            InternalCode,
            VendorCode,
            LocationCode,
            Email,
            Title
        }
        #endregion

        private readonly object _typeId = new object();

        public DuplicateMatchAttribute(Target _target, Field _field, string _PropertyNEW, string _PropertyOLD)
            : base("Duplicate '{0}' found.")
        {
            targt = _target;
            fld = _field;
            PropertyNEW = _PropertyNEW;
            PropertyOLD = _PropertyOLD;
        }

        public string PropertyNEW { get; private set; }
        public string PropertyOLD { get; private set; }

        public override object TypeId { get { return _typeId; } }
        public override string FormatErrorMessage(string name)
        {
            return String.Format(CultureInfo.CurrentUICulture, ErrorMessageString, PropertyNEW);
        }

        public override bool IsValid(object value)
        {
            string val = Convert.ToString(value, CultureInfo.CurrentCulture);
            if (string.IsNullOrEmpty(val)) return true;

            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(value);
            object propNEW = properties.Find(PropertyNEW, true /* ignoreCase */).GetValue(value);
            object propOLD = properties.Find(PropertyOLD, true /* ignoreCase */).GetValue(value);

            propNEW = propNEW ?? ""; propOLD = propOLD ?? "";
            if (string.IsNullOrEmpty(propNEW.ToString()) || string.IsNullOrEmpty(propNEW.ToString())) return true;

            bool ischanged = !Object.Equals(propNEW.ToString().ToLower(), propOLD.ToString().ToLower());
            if (!ischanged) return true;/* Value is NOT changed so no need to check for duplication */

            return checkDuplicate(targt, propNEW, value, val);
        }

        private bool checkDuplicate(Target targt, object propNEW, object value, string val)
        {

            switch (targt)
            {
                case Target.User: if (fld == Field.Email) 
                    return !(new UserService().IsUserEmailDuplicate(propNEW.ToString())); break;
                // url: http://stackoverflow.com/questions/2280539/custom-model-validation-of-dependent-properties-using-data-annotations
                // http://bradwilson.typepad.com/blog/2010/01/remote-validation-with-aspnet-mvc-2.html
                /*case Target.Location: if (fld == Field.LocationCode) 
                    return !(new LocationService().IsLocationCodeDuplicate(val)); break;*/
                case Target.Master: if (fld == Field.Title)
                        return !(new MasterService(null).IsTitleDuplicate(propNEW.ToString())); break;
                case Target.Role: if (fld == Field.Title)
                        return !(new SecurityService().IsTitleDuplicate(propNEW.ToString())); break;
                default: return false;
            }
            
            return true;
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public sealed class DuplicateMatchAndRequiredForMASTERAttribute : ValidationAttribute
    {
        //HT: Exclusive class for the Bulk edit feature of MASTERs - for now it is meant only for Title
        //

        private readonly object _typeId = new object();

        public DuplicateMatchAndRequiredForMASTERAttribute(string _PropertyNEW, string _PropertyOLD)
            : base("{0}")
        {
            PropertyNEW = _PropertyNEW;
            PropertyOLD = _PropertyOLD;
        }

        public string PropertyNEW { get; private set; }
        public string PropertyOLD { get; private set; }
        public bool IsReqOrDup { get; private set; }

        public override object TypeId { get { return _typeId; } }
        public override string FormatErrorMessage(string name)
        {//Here we fool MVC for the error: base.ErrorMessage cannot be set more than once.
            return String.Format(CultureInfo.CurrentUICulture, ErrorMessageString, 
                IsReqOrDup ? "Title required." : "Duplicate 'Title' found.");// PropertyNEW
        }

        public override bool IsValid(object value)
        {
            string val = Convert.ToString(value, CultureInfo.CurrentCulture);
            if (string.IsNullOrEmpty(val)) return true;

            CPM.Models.Master mObj = (CPM.Models.Master)value;

            if (mObj._Deleted || !(mObj._Added || mObj._Updated))
            {
                return true;//Record will NOT be processed
            }

            //Required field check only for ADD / EDIT
            if (string.IsNullOrEmpty(mObj.Title)) { IsReqOrDup = true; return false; }
                //this.ErrorMessage = "Title required."; 

            //Title is non-empty so now we proceed towards dulpicate check
            if ((mObj.Title ?? "").ToLower() == (mObj.TitleOLD ?? "").ToLower()) return true;
            else
            {
                //this.ErrorMessage = "Duplicate 'Title' found.";
                IsReqOrDup = false;
                return !(new MasterService(null).IsTitleDuplicate((mObj.Title ?? "")));
            }
        }
    }

    // url: http://stackoverflow.com/questions/16747/whats-the-best-way-to-implement-field-validation-using-asp-net-mvc 
    #region public class UniqueAttribute : DataTypeAttribute
    /*
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public class UniqueAttribute : DataTypeAttribute
    {
        #region Variables
        private Target targt;
        private Field fld;

        public enum Target : int
        {
            Item,
            Org,
            Search,
            Location,
            User
        }

        public enum Field : int
        {
            ItemCode,
            OrgCode,
            SearchTitle,
            LocationCode,
            UserEmail
        }
        #endregion

        public UniqueAttribute(Target _target, Field _field) : base(DataType.Custom) { targt = _target; fld = _field; }

        public override bool IsValid(object value)
        {
            string val = Convert.ToString(value, CultureInfo.CurrentCulture);
            if (string.IsNullOrEmpty(val)) return true;

            switch (targt)
            {
                case Target.User: if (fld == Field.UserEmail) return !(new UserService().IsUserEmailDuplicate(val)); break;
                // url: http://stackoverflow.com/questions/2280539/custom-model-validation-of-dependent-properties-using-data-annotations
                // http://bradwilson.typepad.com/blog/2010/01/remote-validation-with-aspnet-mvc-2.html 
                case Target.Location: if (fld == Field.LocationCode) return !(new LocationService().IsLocationCodeDuplicate(val)); break;
                default: return false;
            }

            return true;
        }
    }*/
    #endregion

    //http://www.7388.info/index.php/article/wpf/2011-03-03/9112.html
    public class DateRangeAttribute : ValidationAttribute
    {
        private const string DateFormat = "yyyy/MM/dd";
        private const string DefaultErrorMessage = "'{0}' must be a date between {1:d} and {2:d}.";

        public DateTime MinDate { get; set; }
        public DateTime MaxDate { get; set; }

        public DateRangeAttribute() :
            this(Defaults.minSQLDate.ToString(DateFormat, CultureInfo.InvariantCulture), Defaults.maxSQLDate.ToString(DateFormat, CultureInfo.InvariantCulture)) { ; }
        public DateRangeAttribute(string minDate, string maxDate): base(DefaultErrorMessage)
        {
            MinDate = ParseDate(minDate);
            MaxDate = ParseDate(maxDate);
        }

        public override bool IsValid(object value)
        {
            if (value == null || !(value is DateTime))
            {
                return true;
            }
            DateTime dateValue = (DateTime)value;
            return MinDate <= dateValue && dateValue <= MaxDate;
        }
        public override string FormatErrorMessage(string name)
        {
            return String.Format(CultureInfo.CurrentCulture, ErrorMessageString,
            name, MinDate, MaxDate);
        }

        private static DateTime ParseDate(string dateValue)
        {
            return DateTime.ParseExact(dateValue, DateFormat, CultureInfo.InvariantCulture);
        }
    }
    #endregion

    /// <summary>
    /// Use with CAUTION. It is meant only for the POSTBACK Action on search pages which cause unnecessary validation. It'll clear/reset the ModelState
    /// </summary>
    public class SkipModelValidationAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            //Get ModelState
            ModelStateDictionary dict = ((Controller)filterContext.Controller).ModelState;

            if (dict != null && !dict.IsValid)
                dict.Clear();//HT: http://stackoverflow.com/questions/6048670/how-to-disable-validation-in-a-httppost-action-in-asp-net-mvc-3
                        
            base.OnActionExecuting(filterContext);
        }
    }
    /// <summary>
    /// Check Claim accessibility
    /// </summary>
    public class AccessClaimAttribute : ActionFilterAttribute
    {
        static string AccessDeniedPath = Defaults.commonRoot + "/NoAccess";
        string ClaimIdName = string.Empty;

        public AccessClaimAttribute(string ClaimIDpName)
        {
            ClaimIdName = ClaimIDpName;
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            HttpContext ctx = HttpContext.Current;
            bool goAhead = true;
            goAhead = filterContext.ActionParameters.ContainsKey(ClaimIdName);                
            
            if(goAhead)
            {// check if session is supported
                if (ctx.Session != null)
                {
                    int ClaimId;
                    if(int.TryParse((filterContext.ActionParameters[ClaimIdName]??"").ToString(),out ClaimId))
                    goAhead = IsClaimAccessible(ClaimId);
                }
            }
            if (goAhead)
                base.OnActionExecuting(filterContext);
            else// Ref: http://www.joe-stevens.com/2010/08/19/asp-net-mvc-authorize-attribute-using-action-parameters-with-the-actionfilterattribute/
                filterContext.Result = new RedirectResult(AccessDeniedPath);// or HttpUnauthorizedResult();
            //ctx.Response.Redirect(AccessDeniedPath);
            //ABOVE STATEMENT NOT WORKING: It still executes the action
        }

        public bool IsClaimAccessible(int ClaimId)
        {//Check only for Customers (for now)
            if (ClaimId > Defaults.Integer)
                return (!_Session.IsOnlyCustomer || (ClaimId <= CPM.Helper.Defaults.Integer) ||
                    new ClaimService().IsClaimAccessible(ClaimId, _SessionUsr.ID, _SessionUsr.OrgID));
            else
                return true;
        }
    }
    /// <summary>
    /// Enable action / controller level compression
    /// </summary>
    public class CompressFilter : ActionFilterAttribute
    {//Special attribute to leverage the benefits of compression
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (filterContext.IsChildAction)
                // http://stackoverflow.com/questions/5609169/gzip-or-deflate-compression-for-asp-net-mvc-2-without-access-to-server-config
                return; // Special case for child actions

            HttpRequestBase request = filterContext.HttpContext.Request;

            string acceptEncoding = request.Headers["Accept-Encoding"];

            if (string.IsNullOrEmpty(acceptEncoding)) return;

            acceptEncoding = acceptEncoding.ToUpperInvariant();

            HttpResponseBase response = filterContext.HttpContext.Response;

            if (acceptEncoding.Contains("GZIP"))
            {// IE might not support
                response.AppendHeader("Content-encoding", "gzip");
                response.Filter = new GZipStream(response.Filter, CompressionMode.Compress);
            }
            else if (acceptEncoding.Contains("DEFLATE"))
            {// IE and the rest who don't support GZIP
                response.AppendHeader("Content-encoding", "deflate");
                response.Filter = new DeflateStream(response.Filter, CompressionMode.Compress);
            }
        }
    }
    /// <summary>
    /// Control IE AJAX GET request caching
    /// http://stackoverflow.com/questions/2027610/asp-net-mvc-2-rc-caching-problem
    /// </summary>
    public class CacheControlAttribute : ActionFilterAttribute
    {//Also see: http://stackoverflow.com/questions/2243568/needs-a-cause-for-a-error-in-mvc
        public CacheControlAttribute(HttpCacheability cacheability)
        {
            this._cacheability = cacheability;
        }

        private HttpCacheability _cacheability;

        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            HttpCachePolicyBase cache = filterContext.HttpContext.Response.Cache;
            cache.SetCacheability(_cacheability);

            if (_cacheability == HttpCacheability.NoCache)
            {// Look for NoCacheAttribute - http://stackoverflow.com/questions/8488496/mvc-3-and-ajax-calls-cant-stop-it-caching
                cache.SetExpires(DateTime.UtcNow.AddDays(-1));
                cache.SetValidUntilExpires(false);
                cache.SetRevalidation(HttpCacheRevalidation.AllCaches);
                cache.SetCacheability(HttpCacheability.NoCache);
                cache.SetNoStore();  
            }
        }
    }

    #region Session Validation Action Filter(Kept for future ref)
    // HT: NOT needed any more as we're using forms based authentication (hoping all session vars are intact)
    //http://www.tyronedavisjr.com/index.php/2008/11/23/detecting-session-timeouts-using-a-aspnet-mvc-action-filter/
    /*    public class SessionExpireChkAttribute1 : ActionFilterAttribute
    {
        static string LoginPath = Defaults.commonRoot + "/Login";
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            HttpContext ctx = HttpContext.Current;

            // check if session is supported
            if (ctx.Session != null)
            {

                // check if a new session id was generated
                if (ctx.Session.IsNewSession)
                {

                    // If it says it is a new session, but an existing cookie exists, then it must
                    // have timed out
                    string sessionCookie = ctx.Request.Headers["Cookie"];
                    if ((null != sessionCookie) && (sessionCookie.IndexOf("ASP.NET_SessionId") >= 0))
                        ctx.Response.Redirect(LoginPath);
                }

                //HT: START:::::::::: Our own custom-session-checking code
                if(!_Session.IsValid(ctx))
                    ctx.Response.Redirect(LoginPath);
                //HT: END:::::::::::: Our own custom-session-checking code
            }

            base.OnActionExecuting(filterContext);
        }
    }
    public class RolesAttribute : ActionFilterAttribute
    {
        static string AccessDeniedPath = Defaults.commonRoot + "/Login";
        UserService.Roles[] roles = new UserService.Roles[] { };

        public RolesAttribute(UserService.Roles[] _roles)
        {
            roles = _roles;
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            bool isAuthorized = false;
            HttpContext ctx = HttpContext.Current;

            // check if session is supported
            if (ctx.Session != null)
            {
                foreach (UserService.Roles roleStr in roles)
                {
                    if (!isAuthorized)// check if atleast one Roles is Authorized
                   isAuthorized = IsUserInRole(roleStr);
                }

                if (!isAuthorized)
                    ctx.Response.Redirect(AccessDeniedPath);                
            }

            base.OnActionExecuting(filterContext);
        }

        public bool IsUserInRole(UserService.Roles role)
        {
            switch (role)
            {
                case UserService.Roles.Internal: return _Session.IsInternal;
                case UserService.Roles.InternalSales: return _Session.IsSales;
                case UserService.Roles.InternalAdmin: return _Session.IsAdmin;
                default: return true;
            }
            return false;
        }
    }
    */
    #endregion
}
