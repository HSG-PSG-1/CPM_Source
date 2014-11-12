using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CPM.Services;
using CPM.Models;
using CPM.Helper;

namespace CPM.Controllers
{
    public partial class RoleController : BaseController
    {

        //HT: Make sure this is initialized when search is required !
        //public RoleController() : base(100, SecurityService.sortOn, new RoleRights()) { ; }

        #region Bulk Manage

        [IsAuthorize(IsAuthorizeAttribute.Rights.ManageRole)]
        [CacheControl(HttpCacheability.NoCache), HttpGet]
        public ActionResult Manage()
        {
            if (Response.IsRequestBeingRedirected) return View();//Access denied

            //doAddEditPopulate(); 

            _Session.MasterTbl = null;//Make sure this is set because we use it for Duplicate validation
            ViewData["oprSuccess"] = base.operationSuccess;//For successful operation

            ModelState.Clear();//Start FRESH
            return View();//new SecurityService().FetchAll()
        }

        [CacheControl(HttpCacheability.NoCache), HttpGet]
        public ActionResult RolesKOVM()
        {
            return Json(new
            {
                records = new SecurityService().FetchAll(),
                OrgTypes = new LookupService().GetLookup(LookupService.Source.OrgType)
            },
                JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [IsAuthorize(IsAuthorizeAttribute.Rights.ManageRole)]
        public ActionResult Manage(IEnumerable<RoleRights> changes)
        {
            MasterService srv = new MasterService(_Session.MasterTbl);
            bool CanCommit = true; string err = "";
            //Make sure If there's any DELETE - it is NOT being referred
            CanCommit = !MasterController.isDeletedBeingReferred(changes, true, ref err);//.Cast<Master>().ToList()
            //Check duplicates among New records only
            if (CanCommit && changes != null && changes.ToList<Master>().Exists(r => r._Added))
                CanCommit = !MasterController.hasDuplicateInNewEntries(changes, ref err);

            #region All OK so go ahead
            if (CanCommit)//Commit
            {
                new SecurityService().BulkAddEditDel(changes.ToList<RoleRights>());//Performs Add, Edit & Delete by chacking each item
                base.operationSuccess = true; // Set operation sucess
                //Log Activity
                new ActivityLogService(ActivityLogService.Activity.RoleManage).Add(new CPM.DAL.ActivityHistory());
            }
            else // worst case or hack
                return Json(err, JsonRequestBehavior.AllowGet);

            #endregion

            return Json(string.Empty, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Extra Functions

        public void doAddEditPopulate()
        {
           // ViewData["oprSuccess"] = base.operationSuccess;//oprSuccess will be reset after this
            ViewData["OrgTypes"] = new LookupService().GetLookup(LookupService.Source.OrgType);
        }

        #endregion
    }
}
