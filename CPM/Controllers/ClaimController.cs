using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CPM.DAL;
using CPM.Services;
using CPM.Helper;

namespace CPM.Controllers
{
    //[CompressFilter] - don't use it here
    [IsAuthorize(IsAuthorizeAttribute.Rights.NONE)]//Special case for some dirty session-abandoned pages and hacks
    public partial class ClaimController : BaseController
    {
        #region Actions for Claim (Secured)

        [AccessClaim("ClaimID")]
        [CacheControl(HttpCacheability.NoCache), HttpGet]
        public ActionResult Manage(int ClaimID, bool? printClaimAfterSave)
        {
            ViewData["oprSuccess"] = base.operationSuccess; //oprSuccess will be reset after this
            ViewData["printClaimAfterSave"] = (TempData["printClaimAfterSave"]??false);

            #region Add mode - add new and return it in editmode
            if (ClaimID <= Defaults.Integer)
            {// HT: CAREFUL: Add mode in which we need to add a new record
                // Also handles special case for customer to set default SP for him
                string spNameForCustomer = string.Empty;
                Claim NewClaim = new ClaimService().AddDefault(_SessionUsr.ID, _SessionUsr.OrgID, _Session.IsOnlyCustomer, ref spNameForCustomer);
                //Session.Claims[NewClaim.ClaimGUID] = NewClaim;
                //return RedirectToAction("Manage", new { ClaimID = NewClaim.ID, ClaimGUID = NewClaim.ClaimGUID });
                ClaimKOModel vmClaim = doAddEditPopulateKO(ClaimService.GetVWFromClaimObj(NewClaim, spNameForCustomer));
                return View(vmClaim);
            }
            #endregion

            #region Edit mode
            else
            {
                #region Get Claim view and check if its empty or archived - redirect
                
                vw_Claim_Master_User_Loc vw = new ClaimService().GetClaimById(ClaimID);

                if (vw.ID == Defaults.Integer && vw.StatusID == Defaults.Integer && vw.AssignedTo == Defaults.Integer)
                {
                    ViewData["Message"] = "Claim not found"; return View("DataNotFound"); /* deleted claim accessed from Log*/
                }
                // In case an archived entry is accessed
                if (vw.Archived)
                    return RedirectToAction("Archived", new { ClaimID = ClaimID });
                //Empty so invalid ClaimID - go to Home
                if (vw == new ClaimService().emptyView)
                    return RedirectToAction("List", "Dashboard");

                #endregion

                //Reset the Session Claim object
                Claim claimObj = ClaimService.GetClaimObjFromVW(vw);
                //_Session.Claim = claimObj;
                //_Session.Claims[claimObj.ClaimGUID] = claimObj;// Populate original obj

                ClaimKOModel vmClaim = doAddEditPopulateKO(vw);
                return View(vmClaim);
            }
            #endregion
        }

        [CacheControl(HttpCacheability.NoCache), HttpGet]
        public JsonResult ClaimEntryKOViewModel(int ClaimID, string ClaimGUID, int? AssignedTo)
        {// NEW consolidated viewmodel

            CEKOViewModel cvm = new CEKOViewModel(); // Main consolidated viewmodel

            cvm.ClaimDetail = GetItemKOModel(ClaimID, ClaimGUID);
            cvm.Comment = GetCommentKOModel(ClaimID, ClaimGUID, AssignedTo ?? -1);
            cvm.File = GetFileKOModel(ClaimID, ClaimGUID);
            // Status History
            cvm.StatusHistory = new StatusHistoryService().FetchAll(ClaimID);

            return Json(cvm, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [AccessClaim("ClaimID")]
        public ActionResult Delete(int ClaimID, string ClaimGUID, int ClaimNo)
        {
            //http://www.joe-stevens.com/2010/02/16/creating-a-delete-link-with-mvc-using-post-to-avoid-security-issues/
            //http://stephenwalther.com/blog/archive/2009/01/21/asp.net-mvc-tip-46-ndash-donrsquot-use-delete-links-because.aspx
            //Anti-FK: http://blog.codeville.net/2008/09/01/prevent-cross-site-request-forgery-csrf-using-aspnet-mvcs-antiforgerytoken-helper/

            #region Delete claim & log activity

            new ClaimService().Delete(new Claim() { ID = ClaimID });
            //Log Activity (before directory del and sesion clearing)
            new ActivityLogService(ActivityLogService.Activity.ClaimDelete).Add(
                new ActivityHistory() { ClaimID = ClaimID, ClaimText = ClaimNo.ToString() });

            #endregion
            
            // Make sure the PREMANENT files are also deleted
            FileIO.DeleteDirectory(System.IO.Directory.GetParent(FileIO.GetClaimFilesDirectory(ClaimID, ClaimGUID)).FullName);
            // Reset Claim in session (no GUID cleanup needed)
            _Session.ClaimsInMemory.Remove(ClaimGUID); // Remove the Claim from session
            //_Session.ResetClaimInSessionAndEmptyTempUpload(ClaimID, ClaimGUID);

            return Redirect("~/Dashboard");
        }

        [HttpPost]
        [AccessClaim("ClaimID")]
        public ActionResult Archive(int ClaimID, string ClaimGUID, bool Archive, int ClaimNo)
        {
            new ClaimService().Archive(ClaimID, Archive);// Delete claim
            //Log Activity (before directory del and sesion clearing)
            new ActivityLogService(
                Archive ? ActivityLogService.Activity.ClaimArchive : ActivityLogService.Activity.ClaimUnarchive)
                .Add(new ActivityHistory() { ClaimID = ClaimID, ClaimText = ClaimNo.ToString() });
            
            if (Archive) 
                _Session.ResetClaimInSessionAndEmptyTempUpload(ClaimID, ClaimGUID);//reset after act log!
            
            if (Archive) return Redirect("~/Dashboard");
            else return RedirectToAction("Manage", new { ClaimID = ClaimID, ClaimGUID = ClaimGUID });
        }
                
        [HttpPost]
        public JsonResult CleanupTempUpload(int ClaimID, string ClaimGUID)
        {   // Unable to trigger action due to - e.returnValue = 'Make ..'; (frozen for now)
            // Make sure the temp files are also deleted
            _Session.ResetClaimInSessionAndEmptyTempUpload(ClaimID, ClaimGUID);
            return new JsonResult() { Data = new{ msg = "Temp file upload cleanup triggered."}};
        }

        [HttpPost]
        [AccessClaim("ClaimID")]
        public ActionResult Manage(int ClaimID, bool isAddMode,
            [FromJson]vw_Claim_Master_User_Loc claimObj, [FromJson] IEnumerable<ClaimDetail> items,
            [FromJson] IEnumerable<Comment> comments, [FromJson] IEnumerable<FileHeader> files, bool? printClaimAfterSave)
        {
            bool success = false;
            //return new JsonResult() { Data = new{ msg = "success"}};
            
            //HT: Note the following won't work now as we insert a record in DB then get it back in edit mode for Async edit
            //bool isAddMode = (claimObj.ID <= Defaults.Integer); 

            #region Perform operation proceed and set result

            int result = new ClaimService().AsyncBulkAddEditDelKO(claimObj, claimObj.StatusIDold, items, comments, files);
            success = result > 0;

            if (!success) {/*return View(claimObj);*/}
            else //Log Activity based on mode
            {
                claimObj.ClaimNo = result;// Set Claim #
                ActivityLogService.Activity act = isAddMode ? ActivityLogService.Activity.ClaimAdd : ActivityLogService.Activity.ClaimEdit;
                new ActivityLogService(act).Add(new ActivityHistory() { ClaimID = result, ClaimText = claimObj.ClaimNo.ToString() });
            }

            #endregion

            base.operationSuccess = success;//Set opeaon success
            _Session.ClaimsInMemory.Remove(claimObj.ClaimGUID); // Remove the Claim from session
            //_Session.ResetClaimInSessionAndEmptyTempUpload(claimObj.ClaimGUID); // reset because going back to Manage will automatically create new GUID
            
            if(success)
                TempData["printClaimAfterSave"] = printClaimAfterSave.HasValue && printClaimAfterSave.Value;
            
            return RedirectToAction("Manage", new { ClaimID = result });
        }

        [AccessClaim("ClaimID")]
        [CacheControl(HttpCacheability.NoCache), HttpGet]
        public ActionResult Archived(int ClaimID)
        {
            ClaimInternalPrint printView = new ClaimInternalPrint();

            List<Comment> comments = new List<Comment>();
            List<FileHeader> filesH = new List<FileHeader>();
            List<ClaimDetail> items = new List<ClaimDetail>();

            #region Fetch Claim data and set Viewstate
            vw_Claim_Master_User_Loc vw = new ClaimService().GetClaimByIdForPrint(ClaimID,
                ref comments, ref filesH, ref items, !_Session.IsOnlyCustomer);
            
            vw.ClaimGUID = System.Guid.NewGuid().ToString();

            //Set data in View
            ViewData["comments"] = comments;
            ViewData["filesH"] = filesH;
            ViewData["items"] = items;

            printView.view = vw;
            printView.comments = comments;
            printView.filesH = filesH;
            printView.items = items;
            #endregion

            if (vw.ID == Defaults.Integer && vw.StatusID == Defaults.Integer && vw.AssignedTo == Defaults.Integer)
            { ViewData["Message"] = "Claim not found"; return View("DataNotFound"); /* deleted claim accessed from Log*/}
                        
            //Reset the Session Claim object
            //Claim claimObj = ClaimService.GetClaimObjFromVW(vw);
            
            if (vw == null || vw.ID < 1)//Empty so invalid ClaimID - go to Home
                return RedirectToAction("List", "Dashboard");

            return View(printView);
        }

        #endregion        
                
        #region Actions for Status (Secured)

        [HttpPost]
        [AccessClaim("ClaimID")]
        public ActionResult ChangeClaimStatus(int ClaimID, int OldStatusID, int NewStatusID)
        {
            bool result = false; string msg = String.Empty;
            if (OldStatusID != NewStatusID)
            {
                result = new StatusHistoryService().UpdateClaimStatus(ClaimID, OldStatusID, NewStatusID);
                //Log Activity (before directory del and sesion clearing)
                new ActivityLogService(ActivityLogService.Activity.ClaimEdit)
                    .Add(new ActivityHistory() { ClaimID = ClaimID, ClaimText = ClaimID.ToString() });
            }
            else // same status so no need to update
                msg = "No change, same status.";
            //Taconite XML
            return this.Content(Defaults.getTaconiteResult(result,
                Defaults.getOprResult(result, msg), "msgStatusHistory", "updateStatusHistory()"), "text/xml");
        }

        [AccessClaim("ClaimID")]
        [CacheControl(HttpCacheability.NoCache), HttpGet]
        public ActionResult Status(int ClaimID, bool? Archived)
        {
            //http://localhost:4915/Claim/1/Status
            ViewData["IsReadOnly"] = (Archived.HasValue ? Archived.Value : true);
            // NOT need because in MAnage claim we show it as readonly || _Session.Claim.Archived;
            return View(new StatusHistoryService().FetchAll(ClaimID));
        }
        
        #endregion

        #region Extra Functions (for Claim actions)
        public ClaimKOModel doAddEditPopulateKO(vw_Claim_Master_User_Loc claimData)
        {
            ClaimKOModel vm = new ClaimKOModel()
            {
                CVM = claimData
                //ClaimModel = new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(claimData)
            };
            //ViewData["IsEditMode"] = (id != Defaults.Integer);
            vm.CVM.AssignedToOld = vm.CVM.AssignedTo;

            vm.Statuses = new LookupService().GetLookup(LookupService.Source.Status);
            vm.Brands = !_Session.IsOnlyVendor?new LookupService().GetLookup(LookupService.Source.BrandItems):
                //Special case for Vendor users (they must see only their Brands)
                new LookupService().GetLookup(LookupService.Source.BrandVendorItems,extras:_SessionUsr.OrgID.ToString());

            return vm;
        }
        #endregion
    }
}
namespace CPM.DAL
{
    public class ClaimKOModel
    {
        public vw_Claim_Master_User_Loc CVM { get; set; }
        public IEnumerable Statuses { get; set; }
        public IEnumerable Brands { get; set; }
    }

    public class CEKOViewModel
    {
        public ItemKOModel ClaimDetail { get; set; }

        //public string LinesOrderExtTotal { get { return Lines.Sum(l => l.OrderExtension ?? 0.00M).ToString("#0.00"); } }
        //public string OrderTotal { get { return Lines.Sum(l => l.QtyOrdered ?? 0).ToString(); } }

        public CommentKOModel Comment { get; set; }

        public FileKOModel File { get; set; }
        
        public List<vw_StatusHistory_Usr> StatusHistory { get; set; }
    }
}