using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CPM.DAL;
using CPM.Services;
using CPM.Helper;
using System.Collections;

namespace CPM.Controllers
{
    [IsAuthorize(IsAuthorizeAttribute.Rights.ManageUser)]
    public partial class UserController : BaseController
    {
        
        public UserController(): //HT: Make sure this is initialized with default constructor values!
            base(Config.UserListPageSize, UserService.sortOn, Filters.list.User){;}

        #region List Grid

        public ActionResult List()
        {
            ViewData["oprSuccess"] = base.operationSuccess;//oprSuccess will be reset after this
            //base.SetSearchOpts(index.Value);
            searchOpts = _Session.Search[Filters.list.User];
            //Populate ddl Viewdata
            populateData(true);
            ViewData["gridPageSize"] = gridPageSize; // Required to adjust pagesize for grid

            // No need to return view - it'll fetched by ajax in partial rendering
            return View();
            //return View((source == "mobile") ? "ListKOMobile" : "List");
        }

        public ActionResult ListKOGrid(int? index, string qData, bool? success, string source)
        {
            return View();
        }

        #region Will need GET (for AJAX) & Post

        [CacheControl(HttpCacheability.NoCache)]//Don't mention GET or post as this is required for both!
        public JsonResult UserList(int? index, string qData, bool? fetchAll)
        {
            //base.SetTempDataSort(ref index);// Set TempDate, Sort & index
            //Make sure searchOpts is assigned to set ViewState
            vw_Users_Role_Org oldSearchOpts = (vw_Users_Role_Org)searchOpts;
            searchOpts = new vw_Users_Role_Org();
            populateData(false);

            index = (index > 0) ? index + 1 : index; // paging starts with 2

            var result = from vw_u in new UserService().SearchKO((vw_Users_Role_Org)searchOpts) //sortExpr, index, gridPageSize * 2, (vw_Users_Role_Org)searchOpts, fetchAll ?? false)
                         select new
                         {
                             ID = vw_u.ID,
                             Email = vw_u.Email,
                             OrgID = vw_u.OrgID,
                             OrgName = vw_u.OrgName,
                             OrgType = vw_u.OrgType,
                             OrgTypeId = vw_u.OrgTypeId,
                             RoleID = vw_u.RoleID,
                             RoleName = vw_u.RoleName,
                             SalespersonCode = vw_u.SalespersonCode,
                             UserName = vw_u.UserName
                         };

            return Json(new { records = result, search = oldSearchOpts }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [SkipModelValidation]//HT: Use with CAUTION only meant for POSTBACK search Action        
        public JsonResult UserList(vw_Users_Role_Org searchObj, string doReset)
        {
            searchOpts = (doReset == "on") ? new vw_Users_Role_Org() : searchObj; // Set or Reset Search-options
            populateData(false);// Populate ddl Viewdata

            return Json(true);// WE just need to set it in the session
        }

        [HttpPost]
        [SkipModelValidation]
        public ActionResult SetSearchOpts(vw_Users_Role_Org searchObj)
        {
            if (searchObj != null)
            {//Called only to set filter via ajax
                searchOpts = searchObj;
                return Json(true);
            }
            return Json(false);
        }

        [CacheControl(HttpCacheability.NoCache), HttpGet]
        public ActionResult UsersKOVM(vw_Users_Role_Org searchObj, string doReset)
        {
            //Set Item object
            vw_Users_Role_Org newObj = new vw_Users_Role_Org() { ID = 0, LastModifiedBy = _SessionUsr.ID, LastModifiedByName = _SessionUsr.UserName, LastModifiedDate = DateTime.Now, Editing = true, Edited = true };

            //Make sure searchOpts is assigned to set ViewState
            vw_Users_Role_Org oldSearchOpts = (vw_Users_Role_Org)searchOpts;
            searchOpts = new vw_Users_Role_Org();

            populateData(false);// Populate ddl Viewdata

            DAL.UserKOModel vm = new UserKOModel()
            {
                NewRecord = newObj,
                AllUsers = new UserService().SearchKO((vw_Users_Role_Org)searchOpts),
                Search = oldSearchOpts,
                Roles = new SecurityService().GetRolesCached()
            };

            // Lookup data
            vm.Roles = new SecurityService().GetRolesCached();

            vm.showGrid = true;
            return Json(vm, JsonRequestBehavior.AllowGet);
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
                new UserService().Delete(uObj);
                new ActivityLogService(ActivityLogService.Activity.UserDelete).Add();
            }
            //base.operationSuccess = proceed; HT: DON'T
            return this.Content(Defaults.getTaconiteResult(proceed,
                Defaults.getOprResult(proceed, err), null, "removeUser()"), "text/xml");

            /*return this.Content(Defaults.getTaconite(proceed,
                Defaults.getOprResult(proceed, err), null, true), "text/xml");*/
        }

        #endregion

        #region Customer Location GET (for AJAX)
        [CacheControl(HttpCacheability.NoCache)]//Don't mention GET or post as this is required for both!
        public PartialViewResult UserLocation(int UserId, int CustOrgId, int OriCustOrgId)
        {
            return PartialView("~/Views/User/UserLocation.cshtml", 
                new UserService().GetUserLocations(UserId, CustOrgId, (CustOrgId != OriCustOrgId)));
        }
        #endregion

        #region Add, Edit & Delete

        public ActionResult AddEdit(int id)
        {
            Users usr = new UserService().GetUserById(id, _SessionUsr.OrgID);

            if (id > Defaults.Integer && usr.ID == Defaults.Integer && usr.OrgID == Defaults.Integer)
            { ViewData["Message"] = "User not found"; return View("DataNotFound"); /* deleted claim accessed from Log*/}

            doAddEditPopulate(id, usr);

            return View(usr);
        }

        [HttpPost]
        public ActionResult AddEdit(int id, Users usr, string LinkedLoc, string UnlinkedLoc)
        {
            if (base.IsAutoPostback() || !ModelState.IsValid)
            {
                doAddEditPopulate(id, usr);
                #region Use if using Users.Location -at present its aAJAXified
                /*if (locations != null)
                {
                    //HT:CAUTION: Using or assigning to User.UserLocations will create issues
                    locations = locations.Where(l => l != null && l.LocID > Defaults.Integer);
                    usr.UserLocations.Clear(); usr.UserLocations.AddRange(locations);//Pour in location data for View
                }*/
                #endregion
                //In case there's an invalid postback
                ViewData["LinkedLoc"] = LinkedLoc; ViewData["UnlinkedLoc"] = UnlinkedLoc;
                return View(usr);//Request.Form["chkDone"] must be present
            }
            if (usr.isSalesperson) usr.SalespersonCode = "";
            int result = new UserService().AddEdit(usr, LinkedLoc, UnlinkedLoc);
            //Log Activity
            new ActivityLogService((id > Defaults.Integer) ? ActivityLogService.Activity.UserEdit : 
                ActivityLogService.Activity.UserAdd).Add();

            TempData["oprSuccess"] = true;
            return RedirectToAction("List");
        }
        [HttpPost]
        public ActionResult AddEditKO([FromJson] vw_Users_Role_Org usr,string LinkedLoc, string UnlinkedLoc)
        {
            if (!ModelState.IsValid)    return Json(false, JsonRequestBehavior.AllowGet);
            
            int UserEmailCount = new UserService().UserEmailCount(usr.Email);
            bool isEdit = usr.ID > 0;
            if((isEdit && UserEmailCount > 1) || (!isEdit && UserEmailCount > 0))
                return Json(false, JsonRequestBehavior.AllowGet);            
            Users objUsr = UserService.GetObjFromVW(usr);
            int result = new UserService().AddEdit(objUsr, LinkedLoc, UnlinkedLoc);
            new ActivityLogService(isEdit ? ActivityLogService.Activity.UserEdit : ActivityLogService.Activity.UserAdd).Add();
            usr.ID = objUsr.ID; usr.LastModifiedDate = objUsr.LastModifiedDate; 
            usr.LastModifiedBy = objUsr.LastModifiedBy; usr.LastModifiedByName = objUsr.LastModifiedByVal;
            usr.Edited = true; usr.Editing = false;
            
            return Json(usr, JsonRequestBehavior.AllowGet);
        }
        
        [HttpPost]
        public ActionResult DeleteTaco(int? UserId)
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
            
            if (proceed)
            {//Delete & Log Activity
                new UserService().Delete(uObj);
                new ActivityLogService(ActivityLogService.Activity.UserDelete).Add();
            }
            //base.operationSuccess = proceed; HT: DON'T
            return this.Content(Defaults.getTaconiteRemoveTR(proceed,
                Defaults.getOprResult(proceed, err), null, true), "text/xml");
        }

        /* OLD, kept for ref: HttpPost based Delete
        [HttpPost]
  //Secure:http://stephenwalther.com/blog/archive/2009/01/21/asp.net-mvc-tip-46-ndash-donrsquot-use-delete-links-because.aspx
        public ActionResult Delete(int id)
        {  
            Users uObj = new Users() { ID = id };
            bool success = false;
            if (new UserService().IsReferred(uObj))//If user being deleted is referred abort
                ModelState.AddModelError(string.Empty, CPM.Models.Master.delRefChkMsg);
            else
                success = new UserService().Delete(new Users() { ID = id });
            //Log Activity if success
            if (success) new ActivityLogService(ActivityLogService.Activity.UserDelete).Add();
            //Set operation success and redirect
            base.operationSuccess = success;
            return RedirectToAction("List");
        }*/

        #endregion

        #region Extra Functions

        public void doAddEditPopulate(int id, Users usr)
        {
            ViewData["IsEditMode"] = (id > Defaults.Integer);
            populateData(true);
        }

        public void populateData(bool fetchOtherData)//object of type: vw_Users_Role_Org
        {
            //if(!IsAddEditMode) NOT needed because "searchOpts" is called (required to set viewdata) from all search / List actions
            //    vw_Users_Role_Org searchOptions = (vw_Users_Role_Org)(searchOpts);

            //Set any other constraint on searchObj
            if (fetchOtherData)
            {
                //ViewData["Orgs"] = new OrgService().GetOrgs(); - useful in case ORgs filter is needed
                ViewData["Roles"] = new SecurityService().GetRolesCached();
            }
        }

        #endregion


    }
}
namespace CPM.DAL
{
    public class UserKOModel
    {
        public vw_Users_Role_Org NewRecord { get; set; }
        public vw_Users_Role_Org Search { get; set; }
        public List<vw_Users_Role_Org> AllUsers { get; set; }
        public IEnumerable Roles { get; set; }
        public bool showGrid { get; set; }
    }
}