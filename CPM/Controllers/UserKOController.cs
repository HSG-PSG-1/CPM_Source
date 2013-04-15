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
            //base.SetSearchOpts(index.Value);
            searchOpts = _Session.Search[Filters.list.User];
            //Populate ddl Viewdata
            populateData(true);
            // No need to return view - it'll fetched by ajax in partial rendering
            return View((source == "mobile") ? "ListKOMobile" : "ListKO");
        }

        
        #region Will need GET (for AJAX) & Post
        [CacheControl(HttpCacheability.NoCache)]//Don't mention GET or post as this is required for both!
        public JsonResult UserListKO(int? index, string qData)
        {
            base.SetTempDataSort(ref index);// Set TempDate, Sort & index
            //Make sure searchOpts is assigned to set ViewState
            populateData(false);

            return Json(new UserService().SearchKO(sortExpr, index, gridPageSize, (vw_Users_Role_Org)searchOpts),
                JsonRequestBehavior.AllowGet);            
        }
        #endregion

        [HttpPost]
        [SkipModelValidation]//HT: Use with CAUTION only meant for POSTBACK search Action        
        public JsonResult UserListKO(vw_Users_Role_Org searchObj, string doReset, string qData)
        {
            searchOpts = (doReset == "on") ? new vw_Users_Role_Org() : searchObj; // Set or Reset Search-options
            populateData(true);// Populate ddl Viewdata
            //AT PRESENT ONLY 'RESET' & 'SEARCH' come here so need to reset
            //_Session.NewSort = _Session.OldSort = string.Empty;//Set sort variables

            TempData["SearchData"] = searchObj;// To be used by partial view
            //return RedirectToAction("UserListKO");//Though ajaxified but DON'T return - return View();
            return Json(new UserService().SearchKO(sortExpr, 0, gridPageSize, (vw_Users_Role_Org)searchOpts),
                JsonRequestBehavior.AllowGet);            
        }
        
        #endregion
    }
}
