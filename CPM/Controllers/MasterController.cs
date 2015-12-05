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
    //Can't because Security is also in the same controller
    //[IsAuthorize(IsAuthorizeAttribute.Rights.MasterManage)]
    public partial class MasterController : BaseController
    {
        const string defaultMaster = "Manage/Defect";
        private MasterService.Table ddlMaster
        {
            get
            {
                if(string.IsNullOrEmpty(Request.Form["ddlMaster"]))
                    return MasterService.Table.Claim_Status;

                try{return _Enums.ParseEnum<MasterService.Table>(Request.Form["ddlMaster"]);}
                catch { return MasterService.Table.Claim_Status; }
            }
        }                

        //HT: Make sure this is initialized !
        //public MasterController() : base(100, MasterService.sortOn, new Master()) { ; }

        #region Bulk Manage

        [IsAuthorize(IsAuthorizeAttribute.Rights.ManageMaster)]
        [CacheControl(HttpCacheability.NoCache), HttpGet]
        public ActionResult Manage(string masterTbl)
        {
            if (Response.IsRequestBeingRedirected) return View();//Access denied

            if (string.IsNullOrEmpty(masterTbl))
            // This is required so that url redirection works properly when dropdown is changed
            { Response.Redirect(defaultMaster, true); return View(); }
            else
                _Session.MasterTbl = _Enums.ParseEnum<MasterService.Table>(masterTbl);

            ViewData["oprSuccess"] = base.operationSuccess;//For successful operation

            ModelState.Clear();//Start FRESH
            return View();
        }

        [CacheControl(HttpCacheability.NoCache), HttpGet]
        public ActionResult ManageKOVM(string masterTbl)
        {
            if (string.IsNullOrEmpty(masterTbl))
            // This is required so that url redirection works properly when dropdown is changed
            { Response.Redirect(defaultMaster, true); return View(); }
            else
                _Session.MasterTbl = _Enums.ParseEnum<MasterService.Table>(masterTbl);

            return Json(new MasterService(_Session.MasterTbl).FetchAllCached(),
                JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [IsAuthorize(IsAuthorizeAttribute.Rights.ManageMaster)]
        //Old kept for ref - [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]//To avoid js sort issues
        public ActionResult Manage(string masterTbl, IEnumerable<Master> records)//[FromJson] 
        {
            MasterService srv = new MasterService(_Session.MasterTbl);
            bool CanCommit = true; string err = "";
            //Make sure If there's any DELETE - it is NOT being referred
            CanCommit = !isDeletedBeingReferred(records, false, ref err);
            //Check duplicates among New records only
            if (CanCommit && records != null && records.ToList<Master>().Exists(r => r._Added))
                CanCommit = !hasDuplicateInNewEntries(records, ref err);

            #region All OK so go ahead
            if (CanCommit)//Commit
            {
                srv.BulkAddEditDel(records.ToList<Master>());//Performs Add, Edit & Delete by chacking each item
                base.operationSuccess = true;// Set operation sucess
                //Log Activity
                new ActivityLogService(ActivityLogService.Activity.MasterManage).Add(new CPM.DAL.ActivityHistory());
            }
            else // worst case or hack
                return Json(err, JsonRequestBehavior.AllowGet);

            #endregion

            return Json(string.Empty, JsonRequestBehavior.AllowGet);
        }

        #endregion        

        #region Common functions

        /// <summary>
        /// Make sure no master-entry being deleted is referred
        /// </summary>
        /// <param name="items">Master object list</param>
        /// <returns>True if atleast one of the item(s) being deleted is referred, else false</returns>
        public static bool isDeletedBeingReferred(IEnumerable<Master> items, bool isSecurity, ref string err)
        {
            if (items == null || items.Count() < 1) return false;
            items = items.Where(m => m._Deleted && !m._Added).ToList();//reformat the list with only required items
            bool refFound = false;
            foreach (Master item in items)
            {
                if (item._Deleted && !refFound)//MAke sure the overridded method is called!
                {
                    #region HT:CAUTION: item._Deleted = false; won't work because of the following:
                    //http://stackoverflow.com/questions/2329329/mvc2-checkbox-problem
                    //http://iridescence.no/post/Mapping-a-Checkbox-To-a-Boolean-Action-Parameter-in-ASPNET-MVC.aspx
                    //http://stackoverflow.com/questions/4615494/textbox-reverts-to-old-value-while-modelstate-is-valid-on-postback
                    //http://forums.asp.net/t/1597366.aspx/1
                    //item._Deleted = false;//Reset so that it is visible to the user
                    #endregion
                    refFound = isSecurity ? new SecurityService().IsReferred(item) : new MasterService(_Session.MasterTbl).IsReferred(item);                                        
                }
            }
            if (refFound) err = Master.delRefChkMsg;//Ref found for an item being deleted
            return refFound;
        }

        /// <summary>
        /// check if the new entries made has any duplicates
        /// </summary>
        /// <param name="changes">list of master</param>
        /// <param name="err">error message</param>
        /// <returns>true if atleast one of the new entries are duplicate</returns>
        public static bool hasDuplicateInNewEntries(IEnumerable<Master> changes, ref string err)
        {
            //IMP: For consistency make sure that the Delete Ref check is done before this (as we ignore deleted records)
            List<Master> inserts = changes.Where(r => r._Added && !r._Deleted).ToList();// new inserts & not deleted
            List<Master> validEntries = changes.Where(r => !r._Deleted).ToList();// fetch valid entries - r.ID > 0 &&
            bool hasDuplicate = false;
            // check case-in-sensitive title duplication among all the records
            foreach (Master m in inserts)
            {
                hasDuplicate = (validEntries.Count(i => i.Title.ToUpper() == m.Title.ToUpper()) > 1);
                if (hasDuplicate) break;
            }
            if (hasDuplicate) err = Master.insTitleDuplicateMsg;//Ref found for an item being deleted, set error

            return hasDuplicate;
        }
        
        #endregion
    }
}
