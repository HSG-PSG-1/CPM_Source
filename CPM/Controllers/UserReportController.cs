using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CPM.Services;
using CPM.DAL;
using CPM.Helper;
namespace CPM.Controllers
{
    public partial class ReportController: BaseController
    {
        #region User Reports

        public ActionResult UserData(string reportStr)
        {
            reportStr = string.IsNullOrEmpty(reportStr) ? ReportingService.Reports.RolewiseUser.ToString() : reportStr;
            //Unlike search pages, here we've to explicit empty the prev searchOpts
            searchOpts = new ReportingService().emptyUserView;
            //Populate ddl Viewdata
            populateUsrData(searchOpts, true);
            // No need to return view - it'll fetched by ajax in partial rendering
            return View(new UserRpt() { Report = reportStr, SearchData = searchOpts });
        }

        #region Will need GET (for AJAX) & Post
        //[CacheControl(HttpCacheability.NoCache)]//Don't mention GET or post as this is required for both!
        public PartialViewResult UserReport(string reportStr)
        {
            //Make sure searchOpts is assigned to set ViewState
            ReportingService.Reports report = ReportingService.Reports.RolewiseUser;// Default
            try { report = _Enums.ParseEnum<ReportingService.Reports>(reportStr); }
            catch { ;}

            ViewData["SearchData"] = TempData["SearchData"]; // Fetch and set viewstate
            //Make sure searchOpts is assigned to set ViewState
            populateUsrData(searchOpts, false);

            #region Select and render report
            switch (report)
            {
                case ReportingService.Reports.RolewiseUser:
                    return PartialView(userRptPath + "RoleUser.cshtml",
                        new ReportingService().GetRoleUserCount((vw_Users_Role_Org)searchOpts));
                case ReportingService.Reports.OrgTypewiseUser:
                    return PartialView(userRptPath + "OrgTypeUser.cshtml",
                        new ReportingService().GetOrgTypeUserCount((vw_Users_Role_Org)searchOpts));
                
                default:
                    return PartialView(userRptPath + "RoleUser.cshtml", new ReportingService().GetRoleUserCount((vw_Users_Role_Org)searchOpts));
            }

            #endregion
        }

        #endregion

        [HttpPost]
        [SkipModelValidation]//HT: Use with CAUTION only meant for POSTBACK search Action        
        public ActionResult UserData(vw_Users_Role_Org searchObj, string doReset, string reportStr)
        {
            string report = string.IsNullOrEmpty(reportStr) ? ReportingService.Reports.RolewiseUser.ToString() : reportStr;
            searchOpts = (doReset == "on") ? new vw_Users_Role_Org() : searchObj; // Set or Reset Search-options
            populateUsrData(searchOpts, true);// Populate ddl Viewdata

            TempData["SearchData"] = searchObj;// To be used by partial view
            return RedirectToAction("UserReport", new { reportStr = report });//Though ajaxified but DON'T return - return View();
        }

        #endregion

        #region Extra functions

        public void populateUsrData(object searchObj, bool fetchOtherData)
        {
            //Set any other constraint on searchObj
            if (fetchOtherData)
            {
                //ViewData["Orgs"] = new OrgService().GetOrgs(); - useful in case ORgs filter is needed
                ViewData["Roles"] = new SecurityService().GetRoles();
            }
        }

        #endregion
    }
}