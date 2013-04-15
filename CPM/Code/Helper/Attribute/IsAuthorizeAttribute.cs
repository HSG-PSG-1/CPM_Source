using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Security.Principal;
using System.Text;
using CPM.Services;
using CPM.DAL;

namespace CPM.Helper
{
    // Based on http://stackoverflow.com/questions/971401/asp-net-mvc-custom-authorization
    public class IsAuthorizeAttribute : AuthorizeAttribute
    {
        public enum Rights
        {
            NONE,//Special case for some dirty pages escape forms authentication
            ManageUser,
            //UserList,
            //ClaimEntry,
            //ClaimComment,
            DeleteClaim,
            //DashboardInternal,
            ManageMaster,
            ManageSetting,
            ManageRole,
            ViewActivity,
            ArchiveClaim
        }
        
        Rights rightToAuth;

        public IsAuthorizeAttribute(Rights rightToAuthenticate)
        {
            rightToAuth = rightToAuthenticate;
        }

        public override void OnAuthorization(AuthorizationContext filterContext)
        {// This is the actual even triggerredto check authotization for this Action Filter

            if (filterContext == null) throw new ArgumentNullException("filterContext");
            
            #region AuthorizeCore (kept for ref)
            /* Its in the base class and does the work of checking if we have specified 
             * users or roles when we use our attribute
             * 
             * HT: We DON'T need this because instead of Membership we're directly using 
             * Dynamic Rights Authentication here so we skip it
            if (AuthorizeCore(filterContext.HttpContext)) // checks roles/users
            {
                SetCachePolicy(filterContext);
            }
            else 
                */
            #endregion

            //Make sure HttpContext.User is checked for NULL
            if (filterContext.HttpContext.User == null || !filterContext.HttpContext.User.Identity.IsAuthenticated
                || _SessionUsr.ID == new UserService().emptyView.ID)//Also includes session validation
            {
                // auth failed, redirect to login page
                filterContext.Result = new HttpUnauthorizedResult();
            }                        
            else if ( /*_Session.IsAdmin || */ HasRight(rightToAuth, filterContext))
            {// custom check for global role or ownership
                SetCachePolicy(filterContext);
            }
            else
            {// Access Rights Violation
                ViewDataDictionary viewData = new ViewDataDictionary();
                viewData.Add("Message", "You do not have sufficient privileges for this operation.");
                filterContext.Controller = new CPM.Controllers.CommonController();
                filterContext.Result =  new ViewResult { ViewName = "NoAccess", ViewData = viewData };
            }

        }

        private bool HasRight(Rights rightToAuth, AuthorizationContext filterContext)
        {
            #region Original Code kept for ref
            // helper method to determine ownership, uses factory to get data context,
            // then check the specified route parameter (property on the attribute)
            // corresponds to the id of the current user in the database.
            //using (IAuditableDataContextWrapper dc = this.ContextFactory.GetDataContextWrapper())
            //{
            //    int id = -1;
            //    if (filterContext.RouteData.Values.ContainsKey(this.RouteParameter))
            //    {
            //        id = Convert.ToInt32(filterContext.RouteData.Values[this.RouteParameter]);
            //    }

            //    string userName = filterContext.HttpContext.User.Identity.Name;

            //    return dc.Table<Participant>().Where(p => p.UserName == userName && p.ParticipantID == id).Any();
            //}
            #endregion
            switch (rightToAuth)
            {
                case Rights.NONE: return true;
                case Rights.ManageMaster: return _Session.RoleRights.ManageMaster;
                case Rights.ManageRole: return _Session.RoleRights.ManageRole;
                case Rights.ManageUser: return _Session.RoleRights.ManageUser;
                case Rights.DeleteClaim: return _Session.RoleRights.DeleteClaim;
                case Rights.ViewActivity: return _Session.RoleRights.ViewActivity;
                case Rights.ManageSetting: return _Session.RoleRights.ManageSetting;
                case Rights.ArchiveClaim: return _Session.RoleRights.ArchiveClaim;
                default: return false;
            }
        }

        private void CacheValidateHandler(HttpContext context, object data, ref HttpValidationStatus validationStatus)
        {
            validationStatus = OnCacheAuthorization(new HttpContextWrapper(context));
        }

        protected void SetCachePolicy(AuthorizationContext filterContext)
        {
            #region ** IMPORTANT ** 
            // Since we're performing authorization at the action level, the authorization code runs
            // after the output caching module. In the worst case this could allow an authorized user
            // to cause the page to be cached, then an unauthorized user would later be served the
            // cached page. We work around this by telling proxies not to cache the sensitive page,
            // then we hook our custom authorization code into the caching mechanism so that we have
            // the final say on whether a page should be served from the cache.
            #endregion
            HttpCachePolicyBase cachePolicy = filterContext.HttpContext.Response.Cache;
            cachePolicy.SetProxyMaxAge(new TimeSpan(0));
            cachePolicy.AddValidationCallback(CacheValidateHandler, null /* data */);
        }
	}
}