using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using CPM.DAL;
using CPM.Models;
using CPM.Services;
using System.Web.Security;

namespace CPM.Helper
{
    public class _Session
    {
        const string sep = ";";

        #region Object (class) containing session objects

        /* Obsolete - kept for future ref - public static UserSession Usr
        { // SO: 11955094 Go back to In proc session
            get
            {
                try
                {
                    string byteArrStr = HttpContext.Current.Session["UserObj"].ToString();
                    if (string.IsNullOrEmpty(byteArrStr))
                        return new UserService().emptyView;
                    return Serialization.Deserialize<vw_Users_Role_Org>(byteArrStr);
                }
                catch { return new UserService().emptyView; }
            }
            set {
                if (value == null) return;//Extra worst-case check
                //HttpContext.Current.Session["UserObj"] = Serialization.Serialize<vw_Users_Role_Org>(value);
                UserSession.setUserSession(value);
            }
        }*/

        public static MasterRole RoleRights
        {
            get
            {
                MasterRole rDataEmpty = new MasterRole();
                try
                {
                    string byteArrStr = HttpContext.Current.Session["RoleRights"].ToString();
                    if (string.IsNullOrEmpty(byteArrStr))
                        return rDataEmpty;

                    return Serialization.Deserialize<MasterRole>(byteArrStr);
                }
                catch { return rDataEmpty; }
            }
            set
            {
                if (value == null) return;//Extra worst-case check
                HttpContext.Current.Session["RoleRights"] = Serialization.Serialize<MasterRole>(value);
            }
        }

        public static MasterService.Table? MasterTbl
        {
            get
            {
                if (HttpContext.Current.Session["MasterTbl"] == null) return null;
                else return _Enums.ParseEnum<MasterService.Table>(HttpContext.Current.Session["MasterTbl"]);
            }
            set { HttpContext.Current.Session["MasterTbl"] = value; }
        }

        public static Filters Search { get { return new Filters(); } }

        #endregion

        #region Security objects

        public static bool IsInternal
        {
            get
            {
                try { return IsAdmin || (_SessionUsr.OrgTypeId == (int)OrgService.OrgType.Internal); }
                catch { return false; }
            }            
        }

        public static bool IsAdmin
        {
            get
            {
                try { return (_SessionUsr.OrgTypeId == (int)OrgService.OrgType.Internal); }
                catch { return false; }
            }
        }

        public static bool IsSales
        {
            get
            {
                try { return IsAdmin || (_SessionUsr.RoleID == (int)SecurityService.Roles.Sales); }
                catch { return false; }
            }
        }

        public static bool IsOnlyCustomer
        {
            get
            {
                try { return (_SessionUsr.OrgTypeId == (int)OrgService.OrgType.Customer); }
                catch { return false; }
            }
        }

        public static bool IsOnlyVendor
        {
            get
            {
                try { return (_SessionUsr.RoleID == (int)SecurityService.Roles.Vendor); }
                catch { return false; }
            }
        }

        public static bool IsOnlySales
        {
            get
            {
                try { return (_SessionUsr.RoleID == (int)SecurityService.Roles.Sales); }
                catch { return false; }
            }
        }

        #endregion
        
        #region Claim related

        public static bool SkipCustLocCheck
        {
            get
            {
                try { return !IsOnlyCustomer; }
                catch { return false; }
            }
        }

        // Special case: Send his cust org id for Customer and Default for other type of users
        public static int NewCustOrgId
        { get { return IsOnlyCustomer? _SessionUsr.OrgID: Defaults.Integer; /*Config.DefaultClaimCustOrgId*/ } }                
        
        public static Claim Claim1
        {
            get
            {
                try
                {
                    string byteArrStr = HttpContext.Current.Session["ClaimObj"].ToString();
                    if (string.IsNullOrEmpty(byteArrStr))
                        return new ClaimService().emptyClaim;
                    return Serialization.Deserialize<Claim>(byteArrStr);
                }
                catch { return new ClaimService().emptyClaim; }
            }
            set
            {
                if (value == null) return;//Extra worst-case check
                //Set here to avoid replication and maintain at a single location
                if (string.IsNullOrEmpty(value.ClaimGUID)) //initiate GUID - if not done already (OBSOLETE NOW)
                    value.ClaimGUID = System.Guid.NewGuid().ToString();
                HttpContext.Current.Session["ClaimObj"] = Serialization.Serialize<Claim>(value);
            }
        }

        public static Claims ClaimsInMemory { get{return new Claims();} }

        public static void ResetClaimInSessionAndEmptyTempUpload(int ClaimID, string ClaimGUID)
        { // Use ClaimGUID to find the exact claim from
            if (!string.IsNullOrEmpty(ClaimGUID)) // HT: ENSURE ClaimGUID is present
                FileIO.CleanTempUpload(ClaimID, ClaimGUID);
            
            ClaimsInMemory.Remove(ClaimGUID); // Remove the Claim from session
            //HttpContext.Current.Session.Remove("ClaimObj");
        }

        #endregion

        #region Misc & functions

        public static string OldSort1
        {
            get
            {
                try { return (HttpContext.Current.Session["OldSort"] ?? "").ToString().Trim(); }
                catch { return ""; }//DON'T forget to trim
            }
            set
            {
                HttpContext.Current.Session["OldSort"] = value;
            }
        }

        public static string NewSort1
        {
            get
            {
                try { return (HttpContext.Current.Session["NewSort"] ?? "").ToString().Trim(); }
                catch { return ""; }//DON'T forget to trim
            }
            set
            {
                HttpContext.Current.Session["NewSort"] = value;
            }
        }

        public static bool IsValid(HttpContext ctx)
        {/*See in future if need more deep validation 
            if (!string.IsNullOrEmpty((ctx.Session["UserObj"] ?? "").ToString()))
                return (_SessionUsr != new UserService().emptyView);

            return false;*/
            return _SessionUsr.ID > 0;
        }

        public static void Signout()
        {
            FormsAuthentication.SignOut();//HT: reset forms authentication!

            #region clear authentication cookie
            // Get all cookies with the same name
            string[] cookies = new string[] { Defaults.cookieName, Defaults.emailCookie, Defaults.passwordCookie };
            
            //Iterate for each cookie and remove
            foreach (string cookie in HttpContext.Current.Request.Cookies.AllKeys)
                if (!cookies.Contains(cookie))
                    HttpContext.Current.Request.Cookies.Remove(cookie);
            // Strange but it is needed to do it the second time
            foreach (string cookie in HttpContext.Current.Response.Cookies.AllKeys)
                if (!cookies.Contains(cookie))
                    HttpContext.Current.Response.Cookies.Remove(cookie);

            #endregion
            
            //Clear & Abandon session
            HttpContext.Current.Session.Clear();
            HttpContext.Current.Session.Abandon();
        }

        public static string ErrDetailsForELMAH
        {
            get
            {
                try { return (HttpContext.Current.Session["ErrDetailsForELMAH"] ?? "").ToString(); }
                catch { return ""; }
            }
            set
            {
                if (HttpContext.Current != null && HttpContext.Current.Session != null)
                    HttpContext.Current.Session["ErrDetailsForELMAH"] = value;
            }
        }

        public static string WebappVersion
        { // http://www.craftyfella.com/2010/01/adding-assemblyversion-to-aspnet-mvc.html
            get
            {
                if (string.IsNullOrEmpty((HttpContext.Current.Session["WebappVersion"] ?? "").ToString()))
                {
                    try
                    {
                        System.Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                        return version.Major + "." + version.Minor + "." + version.Build;
                    }
                    catch (Exception)
                    {
                        return "?.?.?";
                    }
                }
                else
                    return HttpContext.Current.Session["WebappVersion"].ToString();
            }
        }

        #endregion
    }

    public class Claims
    {
        //http://stackoverflow.com/questions/287928/how-do-i-overload-the-square-bracket-operator-in-c
        //http://msdn.microsoft.com/en-us/library/2549tw02.aspx

        // Indexer declaration. 
        // If index is out of range, the array will throw the exception.         
        public Claim this[string claimGUID]
        {
            get
            {
                object data = HttpContext.Current.Session[claimGUID];
                try
                {
                    if (string.IsNullOrEmpty(data.ToString()))//byteArrStr
                        return new ClaimService().emptyClaim;
                    return Serialization.Deserialize<Claim>(data.ToString());//byteArrStr
                }
                catch { return new ClaimService().emptyClaim; }
                //foreach (Claim clm in this)
                //    if (clm.ClaimGUID == claimGUID) return clm;                
            }
            set
            {
                if (!string.IsNullOrEmpty(value.ClaimGUID))
                    HttpContext.Current.Session[value.ClaimGUID] = Serialization.Serialize<Claim>(value);
                #region OLD Kept for indexer ref / usage
                /* if (this[value.ClaimGUID] != null)
                {
                    for(int i=0;i < this.Count; i++) // can't use foreach!
                        if(this[i].ClaimGUID == value.ClaimGUID)
                        { this[i] = value; break;}
                }
                else // Object has been NOT added yet
                    this.Add(value);
                */
                #endregion
            }
        }

        public void Remove(string claimGUID)
        {
            HttpContext.Current.Session.Remove(claimGUID);
        }
    }

    [Serializable]
    public class Filters
    {//http://stackoverflow.com/questions/287928/how-do-i-overload-the-square-bracket-operator-in-c
        const string prefix = "ObjFor";
        public static readonly object empty = new object();
        public enum list
        {
            _None,
            Dashboard,
            ActivityLog,
            User
        }

        // Indexer declaration. 
        // If index is out of range, the array will throw the exception.         
        public object this[Enum filterID]
        {
            get
            {
                object filterData = HttpContext.Current.Session[prefix + filterID.ToString()];
                try
                {
                    if (filterData == null || filterData == empty)
                    {
                        switch (_Enums.ParseEnum<list>(filterID))
                        {
                            case list.Dashboard: filterData = new DashboardService().emptyView; break;
                            case list.ActivityLog: filterData = new ActivityLogService(ActivityLogService.Activity.Login).emptyView; break;
                            case list.User: filterData = new UserService().emptyView; break;
                        }
                    }                 
                    return filterData;
                }
                catch { return null; }                
            }
            set
            {
                HttpContext.Current.Session[prefix + filterID.ToString()] = value;
            }
        }

        public void Remove(string filterID)
        {
            HttpContext.Current.Session.Remove(prefix + filterID);
        }
    }
}
