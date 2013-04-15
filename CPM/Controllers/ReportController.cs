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
    [IsAuthorize(IsAuthorizeAttribute.Rights.NONE)]
    public partial class ReportController : BaseController
    {
        const string baseRptPath = "~/Views/Report/";        const string activityRptPath = baseRptPath + "Activity/";
        const string userRptPath = baseRptPath + "User/";    const string claimRptPath = baseRptPath + "Claim/";

        public ReportController() //HT: Make sure this is initialized with default constructor values!
            :base(-1, string.Empty, new ReportingService().emptyActView) { }

        #region Activity Reports
        
        public ActionResult Activity(string reportStr)
        {
            reportStr = string.IsNullOrEmpty(reportStr) ? ReportingService.Reports.ActivityCount.ToString() : reportStr;
            //Unlike search pages, here we've to explicit empty the prev searchOpts
            searchOpts = new ReportingService().emptyActView;
            //Populate ddl Viewdata
            populateActData(searchOpts, true);
            // No need to return view - it'll fetched by ajax in partial rendering
            return View(new ActivityRpt() { Report = reportStr, SearchData = searchOpts});
        }

        #region Will need GET (for AJAX) & Post
        //[CacheControl(HttpCacheability.NoCache)]//Don't mention GET or post as this is required for both!
        public PartialViewResult ActivityReport(string reportStr)
        {
            //Make sure searchOpts is assigned to set ViewState
            ReportingService.Reports report = ReportingService.Reports.ActivityCount;// Default
            try { report = _Enums.ParseEnum<ReportingService.Reports>(reportStr); } catch { ;}

            ViewData["SearchData"] = TempData["SearchData"]; // Fetch and set viewstate
            //Make sure searchOpts is assigned to set ViewState
            populateActData(searchOpts, false);

            #region Select and render report
            
            switch (report)
            {
                case ReportingService.Reports.ActivityCount:                
                    return PartialView(activityRptPath + "ActivityCount.cshtml", new ReportingService().GetActivityCount((vw_ActivityLog)searchOpts));
                case ReportingService.Reports.UserwiseActivity:
                    return PartialView(activityRptPath + "UserActivity.cshtml", new ReportingService().GetUserwiseActivity((vw_ActivityLog)searchOpts));
                case ReportingService.Reports.MonthlyUserActivity:
                    return PartialView(activityRptPath + "MonthlyUserActivity.cshtml", new ReportingService().GetMonthlyUserActivity((vw_ActivityLog)searchOpts)); 
                
                default:
                    return PartialView(activityRptPath + "ActivityCount.cshtml", new ReportingService().GetActivityCount((vw_ActivityLog)searchOpts));                    
            }       
     
            #endregion
        }

        #endregion

        [HttpPost]
        [SkipModelValidation]//HT: Use with CAUTION only meant for POSTBACK search Action        
        public ActionResult Activity(vw_ActivityLog searchObj, string doReset)
        {
            string report = ReportingService.Reports.ActivityCount.ToString();
            searchOpts = (doReset == "on") ? new vw_ActivityLog() : searchObj; // Set or Reset Search-options
            populateActData(searchOpts, true);// Populate ddl Viewdata

            if (searchObj.ActivityID > 0)//No need to group by Activity
            {
                if (searchObj.UserID > 0) report = ReportingService.Reports.MonthlyUserActivity.ToString();
                else //searchObj.ClaimID = searchObj.UserID = Defaults.Integer; // To avoid senseless reports
                report = ReportingService.Reports.UserwiseActivity.ToString();
            }            
            
            TempData["SearchData"] = searchObj;// To be used by partial view
            return RedirectToAction("ActivityReport", new { reportStr = report });//Though ajaxified but DON'T return - return View();
        }

        #endregion

        #region Extra functions

        public void populateActData(object searchObj, bool fetchOtherData)
        {
            if (_Session.IsOnlyCustomer) //If its customer he can view only his activity
                ((vw_ActivityLog)searchObj).UserID = _SessionUsr.ID;

            if (fetchOtherData)
            {
                ViewData["Activities"] = new ActivityLogService(ActivityLogService.Activity.Login).GetActivities();
                ViewData["UserList"] = new LookupService().GetLookup(LookupService.Source.User);
            }
        }

        #endregion
    }
}