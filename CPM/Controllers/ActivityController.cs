using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CPM.DAL;
using CPM.Services;
using CPM.Helper;

namespace CPM.Controllers
{
    [IsAuthorize(IsAuthorizeAttribute.Rights.ViewActivity)]//Special case for some dirty session-abandoned pages and hacks
    public class ActivityController : BaseController
    {
        //HT: Make sure this is initialized !
        public ActivityController() : base(20, ActivityLogService.sortOn, Filters.list.ActivityLog) { ;}

        #region List Grid
        
        public ActionResult Log(int? index, string qData)
        {   
            index = index ?? 0;  
            //_Session.NewSort = ActivityLogService.sortOn1;//Initialize (only once) Handles by the default sortOrder property
            //base.SetSearchOpts(index.Value);
            //Populate ddl Viewdata
            populateData(true);
            // No need to return view - it'll fetched by ajax in partial rendering
            return View();
            //Old kept for ref -- srv.Search(sortExpr, index, gridPageSize, (vw_ActivityLog)searchOpts)
        }

        #region Will need GET (for AJAX) & Post
        [CacheControl(HttpCacheability.NoCache)]//Don't mention GET or post as this is required for both!
        public PartialViewResult ActivityLog(int? index, string qData)
        {
            base.SetTempDataSort(ref index);// Set TempDate, Sort & index
            //Make sure searchOpts is assigned to set ViewState
            populateData(false);
            
            return PartialView("~/Views/Activity/EditorTemplates/ActivityLog.cshtml",
                new ActivityLogService(ActivityLogService.Activity.Login)
                .Search(sortExpr, index, gridPageSize, (vw_ActivityLog)searchOpts));
        }
        #endregion

        [HttpPost]
        [SkipModelValidation]//HT: Use with CAUTION only meant for POSTBACK search Action
        public ActionResult Log(vw_ActivityLog searchObj, string doReset, string qData)
        {
            searchOpts = (doReset == "on") ? new vw_ActivityLog() : searchObj; // Set or Reset Search-options
            populateData(true);// Populate ddl Viewdata
            
            //DO THE FOLLOWING WHEN SEARCH, SORT & PAGE - all come here
            //if (doReset == "on") _Session.NewSort = _Session.OldSort = string.Empty;//Set sort variables
            //AT PRESENT ONLY 'RESET' & 'SEARCH' come here so need to reset
            //_Session.NewSort = _Session.OldSort = string.Empty;//Set sort variables

            TempData["SearchData"] = searchObj;// To be used by partial view

            return RedirectToAction("ActivityLog");//Though ajaxified but DON'T return - return View();            
        }
        
        #endregion

        #region Extra Functions

        public void populateData(bool fetchOtherData)
        {
            //base.SetSearchOpts(index.Value);
            //Special case: Set the filter back if it existed so that if the user "re-visits" the page he gets the previous filter (unless reset or logged off)
            searchOpts = _Session.Search[Filters.list.ActivityLog];

            //vw_ActivityLog searchOptions = (vw_ActivityLog)(searchOpts);

            if (_Session.IsOnlyCustomer) //If its customer he can view only his activity
                (searchOpts as vw_ActivityLog).UserID = _SessionUsr.ID; // searchOptions

            if (fetchOtherData){
            ViewData["Activities"] = new ActivityLogService(ActivityLogService.Activity.Login).GetActivities();
            ViewData["UserList"] = new LookupService().GetLookup(LookupService.Source.User);
            }
        }

        #endregion
    }
}
