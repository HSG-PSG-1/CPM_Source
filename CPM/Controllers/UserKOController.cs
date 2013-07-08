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
    public partial class UserController : BaseController
    {
        #region List Grid

        public ActionResult ListKO(int? index, string qData, bool? success, string source)
        {
            index = index ?? 0;
            ViewData["oprSuccess"] = base.operationSuccess;//oprSuccess will be reset after this
            //base.SetSearchOpts(index.Value);
            searchOpts = _Session.Search[Filters.list.User];
            //Populate ddl Viewdata
            populateData(true);
            ViewData["gridPageSize"] = gridPageSize; // Required to adjust pagesize for grid

            // No need to return view - it'll fetched by ajax in partial rendering
            return View((source == "mobile") ? "ListKOMobile" : "ListKO");
        }

        public ActionResult ListKOGrid(int? index, string qData, bool? success, string source)
        {
            return View();
        }
        
        #region Will need GET (for AJAX) & Post
        
        [CacheControl(HttpCacheability.NoCache)]//Don't mention GET or post as this is required for both!
        public JsonResult UserListKO(int? index, string qData, bool? fetchAll)
        {
            base.SetTempDataSort(ref index);// Set TempDate, Sort & index
            //Make sure searchOpts is assigned to set ViewState
            vw_Users_Role_Org oldSearchOpts = (vw_Users_Role_Org)searchOpts;
            searchOpts = new vw_Users_Role_Org();
            populateData(false);

            index = (index > 0) ? index + 1 : index; // paging starts with 2

            var result = new UserService().SearchKO(sortExpr, index, gridPageSize * 2, (vw_Users_Role_Org)searchOpts, fetchAll ?? false);

            return Json(new { records = result, search = oldSearchOpts }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [SkipModelValidation]//HT: Use with CAUTION only meant for POSTBACK search Action        
        public JsonResult UserListKO(vw_Users_Role_Org searchObj, string doReset, string qData)
        {
            searchOpts = (doReset == "on") ? new vw_Users_Role_Org() : searchObj; // Set or Reset Search-options
            populateData(false);// Populate ddl Viewdata

            return Json(true);// WE just need to set it in the session
        }

        [HttpPost]
        [SkipModelValidation]
        public ActionResult SetSearchOpts(vw_Claim_Dashboard searchObj)
        {
            if (searchObj != null)
            {//Called only to set filter via ajax
                searchOpts = searchObj;
                return Json(true);
            }
            return Json(false);
        }

        #endregion
        
        [HttpPost]
        public ActionResult UserKODelete(int? UserId)
        {
            Users uObj = new Users() { ID = UserId.Value };
            bool proceed = false; string err = "";
            proceed = !(new UserService().IsReferred(uObj));//If user being deleted is referred abort            
            if (!proceed)
                err = CPM.Models.Master.delRefChkMsg;
            else
            {
                proceed = !(uObj.ID == _SessionUsr.ID); // Self delete
                if (!proceed) err = "Cannot delete your own record!";
            }

            if (proceed) // NOT deleted because testing
            {//Delete & Log Activity
                //new UserService().Delete(uObj);
                //new ActivityLogService(ActivityLogService.Activity.UserDelete).Add();
            }
            //base.operationSuccess = proceed; HT: DON'T
            return this.Content(Defaults.getTaconite(proceed,
                Defaults.getOprResult(proceed, err) + (proceed?"(NOT DELETED just testing)":""), null, true), "text/xml");

            //return this.Content(Defaults.getTaconite(true, Defaults.getOprResult(true, ""), "cmtOprMsg"), "text/xml");            
        }

        #endregion
    }
}
