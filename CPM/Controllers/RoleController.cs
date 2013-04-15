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

            doAddEditPopulate(); 

            _Session.MasterTbl = null;//Make sure this is set because we use it for Duplicate validation
            ViewData["oprSuccess"] = base.operationSuccess;//For successful operation

            ModelState.Clear();//Start FRESH
            return View(new SecurityService().FetchAll());
        }

        [HttpPost]
        [IsAuthorize(IsAuthorizeAttribute.Rights.ManageRole)]
        public ActionResult Manage(List<RoleRights> changes)
        {
            bool CanCommit = ModelState.IsValid; string err = "";

            #region Can commit
            if (CanCommit)
            {
                //Make sure If there's any DELETE - it is NOT being referred
                CanCommit = !(MasterController.isDeletedBeingReferred(changes.Cast<Master>().ToList(), true,ref err));
                
                //Check duplicates among New records only
                if (CanCommit && changes != null && changes.Exists(r => r.IsAdded))
                    CanCommit = !MasterController.hasDuplicateInNewEntries(changes.Cast<Master>().ToList(), ref err);

                #region All OK so go ahead and commit
                if (CanCommit)//Commit
                {
                    new SecurityService().BulkAddEditDel(changes);//Performs Add, Edit & Delete by chacking each item
                    base.operationSuccess = true; // Set operation sucess
                    //Log Activity
             new ActivityLogService(ActivityLogService.Activity.RoleManage).Add(new CPM.DAL.ActivityHistory());
                }
                #endregion
            }
            #endregion

            #region Can't commit
            if (!CanCommit)
            {
                ModelState.AddModelError(string.Empty, err);
                ViewData["oprSuccess"] = false;//Don't use base.oprSuccess because it doesn't redirect!
                ViewData["err"] = err;

                doAddEditPopulate();
                return View(changes);
            }
            #endregion

            //return and refresh the same action
            return RedirectToAction("");//Manage
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
