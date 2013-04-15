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
            {Response.Redirect(defaultMaster, true); return View();}
            else
                _Session.MasterTbl = _Enums.ParseEnum<MasterService.Table>(masterTbl);

            ViewData["oprSuccess"] = base.operationSuccess;//For successful operation

            ModelState.Clear();//Start FRESH
            return View(new MasterService(_Session.MasterTbl).FetchAll());
        }

        [HttpPost]
        [IsAuthorize(IsAuthorizeAttribute.Rights.ManageMaster)]
        //Old kept for ref - [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]//To avoid js sort issues
        public ActionResult Manage(string masterTbl, List<Master> changes)
        {
            MasterService srv = new MasterService(_Session.MasterTbl);
            
            #region Issue due to which we can't sort Model as done in js!
            //NOT possible to change value of Model during postback - 
            // URL --- http://www.gxclarke.org/2010/05/consumption-of-data-in-mvc2-views.html
            //ISSUE: http://stackoverflow.com/questions/5007330/mvc-jquery-reorder-list-not-ordered-on-postback
            //var changesReordered = new List<Master>(changes.Count);
            //if (changes != null && changes.Count > 0)//Apply sorting as it was applied by js
            //    changesReordered = (from c in changes select c).OrderBy(x => Convert.ToInt32(x.SortOrder)).ToList();
            #endregion
            
            bool CanCommit = ModelState.IsValid; string err = "";
            
            #region Can commit
            if (CanCommit)
            {
                //Make sure If there's any DELETE - it is NOT being referred
                CanCommit = !(isDeletedBeingReferred(changes, false,ref err));
                //Check duplicates among New records only
                if (CanCommit && changes != null && changes.Exists(r => r.IsAdded))
                    CanCommit = !hasDuplicateInNewEntries(changes, ref err);
                
                #region All OK so go ahead
                if (CanCommit)//Commit
                {
                    srv.BulkAddEditDel(changes);//Performs Add, Edit & Delete by chacking each item
                    base.operationSuccess = true;// Set operation sucess
                    //Log Activity
                    new ActivityLogService(ActivityLogService.Activity.MasterManage).Add(new CPM.DAL.ActivityHistory());
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

                // doAddEditPopulate(); -- if needed in futrue
                return View(changes);
            }
            #endregion

            #region NOTE regarding ModelState
            //HT:IMP:Caution: Clear ModelState so that old data is FLUSHED //http://stackoverflow.com/questions/2547111/unexpected-html-editorfor-behavior-in-asp-net-mvc-2
            //ModelState.Clear();//Caching & History issues (http://forums.asp.net/p/1527149/3687407.aspx)
            //return View(new MasterService(_Session.MasterTbl).FetchAll());
            //OR better Redirect !
            #endregion

            Response.Redirect(masterTbl, true); return View();
        }

        #endregion

        #region Common functions

        /// <summary>
        /// Make sure no master-entry being deleted is referred
        /// </summary>
        /// <param name="items">Master object list</param>
        /// <returns>True if atleast one of the item(s) being deleted is referred, else false</returns>
        public static bool isDeletedBeingReferred(List<Master> items, bool isSecurity, ref string err)
        {
            items = items.Where(m => m.IsDeleted && !m.IsAdded).ToList();//reformat the list with only required items
            bool refFound = false;
            foreach (Master item in items)
            {
                if (item.IsDeleted && !refFound)//MAke sure the overridded method is called!
                {
                    #region HT:CAUTION: item.IsDeleted = false; won't work because of the following:
                    //http://stackoverflow.com/questions/2329329/mvc2-checkbox-problem
                    //http://iridescence.no/post/Mapping-a-Checkbox-To-a-Boolean-Action-Parameter-in-ASPNET-MVC.aspx
                    //http://stackoverflow.com/questions/4615494/textbox-reverts-to-old-value-while-modelstate-is-valid-on-postback
                    //http://forums.asp.net/t/1597366.aspx/1
                    //item.IsDeleted = false;//Reset so that it is visible to the user
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
        public static bool hasDuplicateInNewEntries(List<Master> changes, ref string err)
        {
            List<Master> inserts = changes.Where(r => r.IsAdded && !r.IsDeleted).ToList();// new inserts & not deleted
            List<Master> validEntries = changes.Where(r => r.ID != 0 || !r.IsDeleted).ToList();// fetch valid entries
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
