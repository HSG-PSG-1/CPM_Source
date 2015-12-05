using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Principal;
using System.Web;
using System.Web.Security;
using System.Web.Mvc;
using System.Web.Routing;
using System.Text;
using CPM.Models;
using CPM.Services;
using CPM.Helper;
using CPM.DAL;

namespace CPM.Controllers
{
    //[HandleError(View = "Error")] - handled in Application_Error
    public class CommonController : BaseController
    {
        #region Login/off Send password

        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Head)] /* For future generic capturing of HEAD - SO: 8329101 */
        //[ValidateInput(false)] // SO: 2673850/validaterequest-false-doesnt-work-in-asp-net-4
        public ActionResult Login(string from, string email, string pwd, bool remember = false)
        {
            if ((from ?? "").ToLower() == "logoff") LogOff();// Special Case: using an action named logoff creates complexity
            
            if (remember)
                SetCookie(new LogInModel() { Email = email, Password = Crypto.EncodeStr(pwd, false), RememberMe = remember });
            HttpCookie authCookie = Request.Cookies[Defaults.cookieName];
            LogInModel loginM = new LogInModel();

            #region If cookie present, then populate values from cookie
            
            if (authCookie != null)
            {
                try
                {// Set data as per cookie
                    //authCookie = HttpSecureCookie.Decode(authCookie, CookieProtection.Encryption);//HT: decode the encoded cookie
                    authCookie = new HttpCookie(Defaults.cookieName, Crypto.EncodeStr(authCookie.Value, false)); 
                    loginM.Email = authCookie.Values[Defaults.emailCookie];
                    loginM.Password = Crypto.EncodeStr(authCookie.Values[Defaults.passwordCookie], false);
                    loginM.RememberMe = true;
                }
                catch
                { /* BAD Cookie */loginM.RememberMe = false; }
            }

            #endregion

            #region Else, display Remember-Me check-box
            else
                loginM.RememberMe = false;
            #endregion

            return View(loginM);

            //Remember me issue with Forms Authentication
            //http://stackoverflow.com/questions/2452656/asp-net-mvc-remembermeits-large-please-dont-quit-reading-have-explained-the
        }

        [HttpPost]
        //[ValidateInput(false)] // SO: 2673850/validaterequest-false-doesnt-work-in-asp-net-4
        public ActionResult Login(LogInModel model, string ReturnUrl, bool? IsForgotPassword, string UserEmail)
        {
            if (IsForgotPassword.HasValue && IsForgotPassword.Value)
                return SendPassword(UserEmail, model);//SPECIAL CASE PROCESSING for Forgot password

            if (ModelState.IsValid)
            {
                vw_Users_Role_Org usr = new UserService().Login(model.Email, model.Password);
                //If the returned User object is not null or empty login is successfull
                if (usr != null && usr.ID > 0 && usr != new UserService().emptyView)
                {
                    Config.ConfigSettings = new SettingService().FetchSettings();//Initialize settings                    
                    FormsAuthentication.SetAuthCookie(usr.Email, model.RememberMe);//Set forms authentication!

                    #region Remember Me (add or remove cookie)

                    SetCookie(model);

                    #endregion

                    //Set session
                    _SessionUsr.setUserSession(usr);
                    _Session.RoleRights = new UserService().GetRoleRights(usr.RoleID);
                    //Log Activity
                    new ActivityLogService(ActivityLogService.Activity.Login).Add();

                    //Redirect to return url - if its valid
                    if (RedirectFromLogin(ref ReturnUrl))
                        return Redirect(ReturnUrl);
                    else//Go to the default url -  Dashboard?from=login
                        return RedirectToAction("List", "Dashboard");
                }
                else // Login failed
                {
                    ModelState.AddModelError("", Defaults.InvalidEmailPWD);
                    ViewData["oprSuccess"] = false;
                    ViewData["err"] = Defaults.InvalidEmailPWD;                        
                }
            }

            LogOff();// To make sure no session is set until Login (or it'll go in Login HttpGet instead of Post)
            // If we got this far, something failed, redisplay form
            return View(model);
        }

        public ActionResult SendPassword(string Email, LogInModel model)
        {//Unable to get any other action to skip security check - nor can we have another view\page as we're using dialog
            ViewData["showSendPWD"] = true;
            ModelState.Clear();//clear any Login issues

            if (string.IsNullOrEmpty(Email))// ModelState.IsValid) - validate data explicitly
                ModelState.AddModelError("UserEmail", "Email is required field.");

            #region If data is valid and email is found, send email and set result viewstate
            else
            {
                string Pwd = new UserService().GetUserPWDByEmail(Email);
                bool oprSuccess = !string.IsNullOrEmpty(Pwd);
                
                ViewData["oprSuccess"] = oprSuccess;//Err msg handled in View
                if (oprSuccess)//Send email
                    MailManager.ForgotPwdMail(Email, Pwd, new SettingService().GetContactEmail());
                else
                    ViewData["err"] = Defaults.ForgotPWDInvalidEmail;
            }
            #endregion

            LogOff();// To make sure no session is set until Login (or it'll go in Login HttpGet instead of Post)
            return View(model);
        }

        #endregion

        #region Functions & JSON result returning Actions

        //Remember Me (add or remove cookie)
        void SetCookie(LogInModel model)
        {
            bool remember = model.RememberMe;

            //Set Cookie (double encryption - encrypted pwd & encrypted cookie)
            HttpCookie authRememberCookie = new HttpCookie(Defaults.cookieName);
            authRememberCookie.Values[Defaults.emailCookie] = remember ? model.Email : "";
            authRememberCookie.Values[Defaults.passwordCookie] = remember ? Crypto.EncodeStr(model.Password, true) : "";
            authRememberCookie.Expires = remember ? DateTime.Now.AddHours(Config.RememberMeHours)
                : DateTime.Now.AddYears(-1);//to avoid any datetime diff
            /*HT: encode the cookie // Can't because of machine specific machine key - http://msdn.microsoft.com/en-us/library/ff649308.aspx#paght000007_webfarmdeploymentconsiderations
            authRememberCookie.Value = Encoding.Unicode.GetString(MachineKey.Protect(Encoding.Unicode.GetBytes(authRememberCookie.Value)));*/
            authRememberCookie.Value = Crypto.EncodeStr(authRememberCookie.Value, true);
            Response.Cookies.Add(authRememberCookie);
        }

        //[CacheControl(HttpCacheability.NoCache), HttpGet]
        [OutputCache(Duration=3600, VaryByParam="id;term;extras")]
        public JsonResult Lookup(string id, string term, string extras)
        {
            #region Ref code kept for testing
            //SelectList lst = new SelectList(new LookupService().GetLookup(id), LookupService.ID, LookupService.TEXT);
            //string[] items = lst.Select(o => string.Format(LookupService.format, o.Text, o.Value)).ToArray<string>();
            //return Content(string.Join(LookupService.sep, items));//"txt1|1\ntxt2|2");
            #endregion

            LookupService.Source enumObj = _Enums.ParseEnum<LookupService.Source>(id);

            #region Check explicit Security (Only for Customer)

            if (_Session.IsOnlyCustomer)
            {//Exclusive checks for Customer user
                switch (enumObj)
                {//Because Customer does not have access to any of this
                    case LookupService.Source.Customer:
                    case LookupService.Source.Internal:
                    case LookupService.Source.Vendor:
                        return Json(string.Empty, JsonRequestBehavior.AllowGet);
                    #region Folllowing cases are either accessible to ALL type of users or their access is NOT done via this JSON Lookup
                    /*
                case LookupService.Source.Item:break;
                case LookupService.Source.Item1:break;
                case LookupService.Source.Brand:break;
                case LookupService.Source.FileHeader:break;
                case LookupService.Source.FileDetail:break;
                case LookupService.Source.Defect:break;
                case LookupService.Source.Status: break;
                case LookupService.Source.ShipLoc:break;
                case LookupService.Source.User:break;
                case LookupService.Source.SalespersonUser:break;
                case LookupService.Source.CustomerUser:break;
                case LookupService.Source.VendorUser:break;
                default: return Json(string.Empty);
             */
                    #endregion
                }
            }

            #endregion

            return Json(new LookupService().GetLookup(enumObj, true, (term ?? "").Trim(), extras), JsonRequestBehavior.AllowGet);
            //For default jQueryUI: JsonRequestBehavior.AllowGet

            #region Kept for future ref
            /*
            //HT: http://stackoverflow.com/questions/313281/how-can-i-get-a-jsonresult-object-as-a-string-so-i-can-modify-it
            System.Web.Script.Serialization.JavaScriptSerializer jsr = new System.Web.Script.Serialization.JavaScriptSerializer();
            string yourJsonResult = jsr.Serialize(result.Data);
            
            //HT: http://stackoverflow.com/questions/4234455/jquery-autocomplete-not-working-with-json-data
            //[{ "id": "Erithacus rubecula", "label": "European Robin", "value": "European Robin" }, { "id": ...
            */
            #endregion
        }

        [CacheControl(HttpCacheability.NoCache), HttpGet]
        public JsonResult Validate(string src, string term, string extras)
        {// Used exclusively or User Email AJAX validation
            if((term??"").ToLower() != (extras??"").ToLower())//LookupService.Source enumObj = _Enums.ParseEnum<LookupService.Source>(id);
                return Json(!new UserService().IsUserEmailDuplicate(term), JsonRequestBehavior.AllowGet);
            else
                return Json(true, JsonRequestBehavior.AllowGet);//For default jQueryUI: JsonRequestBehavior.AllowGet
        }

        public void LogOff()
        {
            //Log Activity
            if (/*_Session.Usr != null && */ _SessionUsr.ID > 0)
                new ActivityLogService(ActivityLogService.Activity.Logout).Add();
            //Signout FormsAuthentication, session, etc..
            _Session.Signout();            
        }

        public bool RedirectFromLogin(ref string ReturnUrl)
        {
            char[] slash = new char[] { '/' };
            string[] obsoletURLtokens = new string[] { "login", "claimguid", "files", "filesdetail", "getfile", "excel" }; 

            //HT : CAUTION: if url contains "ClaimGUID" - DO NOT proceed with that url because it'll be INVALID
            if (string.IsNullOrEmpty(ReturnUrl) ||
                ReturnUrl.Trim(slash) == VirtualPathUtility.ToAbsolute("~/").Trim(slash)) ReturnUrl = "";

            #region Check and validate redirect url - if its valid
            //redirect if returnUrl is available and not '/'    OR    an unwanted url which contains login
            bool goAhead = !String.IsNullOrEmpty(ReturnUrl.Trim(slash));
            
            if(goAhead)
                foreach (string token in obsoletURLtokens)
                    if (ReturnUrl.ToLower().Contains(token))//!ReturnUrl.ToLower().Contains("login")
                    { 
                        goAhead = false; 
                        break; 
                    }

            #endregion

            return goAhead;
        }

        #endregion

        #region Misc actions

        public ActionResult NoAccess()
        {
            return View();// Will return the default view
        }

        public ActionResult Navigate()
        {

            ViewData["Message"] = "";
            return View();
        }
        [HttpPost]
        public ActionResult Navigate(string searchText)
        {
            object result = new object();
            try
            {
                NavigatorService.NavFeature NavTo = NavigatorService.NavFeature.Dashboard;
                result = new NavigatorService(searchText).Navigate(ref NavTo);
                switch (NavTo)
                {
                    case NavigatorService.NavFeature.Dashboard:
                        vw_Claim_Dashboard resultD = (vw_Claim_Dashboard)result;
                        int ClaimNo = -1;

                        if (int.TryParse(resultD.ClaimNos, out ClaimNo))
                            return RedirectToAction("Manage", "Claim", new { ClaimId = ClaimNo });
                        else
                        {
                            base.filter = Filters.list.Dashboard;
                            base.searchOpts = (vw_Claim_Dashboard)result;
                            return RedirectToAction("List", "Dashboard");
                        }                        
                    case NavigatorService.NavFeature.ClaimEntry:
                        return RedirectToAction("Manage", "Claim", new { ClaimId = (int)result });
                    case NavigatorService.NavFeature.Usr:
                        base.filter = Filters.list.User;
                        base.searchOpts = (vw_Users_Role_Org)result;
                        return RedirectToAction("List", "User");                        
                    default: result = "No match found!";break;
                }
            }
            catch (Exception ex)
            {
                result = ex.Message;   
            }
            ViewData["Message"] = "Still learning!<br/><br/>TECH INFO:" + result.ToString();
            return View();
        }

        #endregion
    }

    public class ErrorController : Controller
    {
        public ActionResult Error()
        {
            return View();// Will return the default view
        }

        public ActionResult Index()
        {
            return View();// Will return the default view
        }

        public ActionResult DataNotFound()
        {
            return View();// Will return the default view
        }

        public ActionResult TestException()
        {
            Exception exInner = new Exception("This is Inner exception"); exInner.Source = "TEST";
            Exception ex = new Exception("Testing Exception Handling ...", exInner);
            throw ex;//Explictl throw a new exception
        }

        public ActionResult TestUnauthorizedAccessException()
        {
            Exception ex = new UnauthorizedAccessException("UnauthorizedAccessException exception"); ex.Source = "TEST";
            throw ex;//Explictl throw a new exception
        }        
    }
}
