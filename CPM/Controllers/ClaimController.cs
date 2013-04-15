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
    //[CompressFilter] - don't use it here
    [IsAuthorize(IsAuthorizeAttribute.Rights.NONE)]//Special case for some dirty session-abandoned pages and hacks
    public partial class ClaimController : BaseController
    {
        bool IsAsync
        {
            get { return true; }
            /* until we upgrade code in future for Sync mode where changes will take place on the go instead of waiting until final commit */
            set{;} 
        }

        #region Actions for Claim (Secured)

        [AccessClaim("ClaimID")]
        [CacheControl(HttpCacheability.NoCache), HttpGet]
        public ActionResult Manage(int ClaimID)
        {
            ViewData["oprSuccess"] = base.operationSuccess;//oprSuccess will be reset after this

            #region Add mode - add new and return it in editmode
            if (ClaimID <= Defaults.Integer)
            {// HT: CAREFUL: Add mode in which we need to add a new record
                // Also handles special case for customer to set default SP for him
                string spNameForCustomer = string.Empty;
                Claim NewClaim = new ClaimService().AddDefault(_SessionUsr.ID, _SessionUsr.OrgID, _Session.IsOnlyCustomer, ref spNameForCustomer);
                //_Session.Claim = NewClaim;
                _Session.Claims[NewClaim.ClaimGUID] = NewClaim;
                //return RedirectToAction("Manage", new { ClaimID = NewClaim.ID, ClaimGUID = NewClaim.ClaimGUID });
                doAddEditPopulate();                
                return View(ClaimService.GetVWFromClaimObj(NewClaim,  spNameForCustomer));
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
                _Session.Claims[claimObj.ClaimGUID] = claimObj;// Populate original obj

                doAddEditPopulate();
                return View(vw);
            }
            #endregion
        }

        [HttpPost]
        [AccessClaim("ClaimID")]
        public ActionResult Delete(int ClaimID, string ClaimGUID)
        {
            //http://www.joe-stevens.com/2010/02/16/creating-a-delete-link-with-mvc-using-post-to-avoid-security-issues/
            //http://stephenwalther.com/blog/archive/2009/01/21/asp.net-mvc-tip-46-ndash-donrsquot-use-delete-links-because.aspx
            //Anti-FK: http://blog.codeville.net/2008/09/01/prevent-cross-site-request-forgery-csrf-using-aspnet-mvcs-antiforgerytoken-helper/

            #region Delete claim & log activity

            new ClaimService().Delete(new Claim() { ID = ClaimID });
            //Log Activity (before directory del and sesion clearing)
            new ActivityLogService(ActivityLogService.Activity.ClaimDelete).Add(
                new ActivityHistory() { ClaimID = ClaimID, ClaimText = _Session.Claims[ClaimGUID].ClaimNo.ToString() });

            #endregion

            // Make sure the PREMANENT files are also deleted
            FileIO.EmptyDirectory(FileIO.GetClaimDirPathForDelete(ClaimID, null, null, false));
            // Reset Claim in session
            _Session.ResetClaimInSessionAndEmptyTempUpload(ClaimGUID);

            return Redirect("~/Dashboard");
        }

        [HttpPost]
        [AccessClaim("ClaimID")]
        public ActionResult Archive(int ClaimID, string ClaimGUID, bool Archive)
        {
            new ClaimService().Archive(ClaimID, Archive);// Delete claim
            //Log Activity (before directory del and sesion clearing)
            new ActivityLogService(
                Archive ? ActivityLogService.Activity.ClaimArchive : ActivityLogService.Activity.ClaimUnarchive)
                .Add(new ActivityHistory() { ClaimID = ClaimID, ClaimText = _Session.Claims[ClaimGUID].ClaimNo.ToString() });
            _Session.ResetClaimInSessionAndEmptyTempUpload(ClaimGUID);//reset after act log!
            if (Archive) return Redirect("~/Dashboard");
            else return RedirectToAction("Manage", new { ClaimID = ClaimID, ClaimGUID = ClaimGUID });
        }

        public ActionResult Cancel(int ClaimID, string ClaimGUID)
        {
            // Make sure the temp files are also deleted
            FileIO.EmptyDirectory(FileIO.GetClaimFilesTempFolder(ClaimGUID, true));
            FileIO.EmptyDirectory(FileIO.GetClaimFilesTempFolder(ClaimGUID, false));            

            _Session.ResetClaimInSessionAndEmptyTempUpload(ClaimGUID);
            return Redirect("~/Dashboard");
        }

        [HttpPost]
        [AccessClaim("ClaimID")]
        public ActionResult Manage(int ClaimID, vw_Claim_Master_User_Loc claimObj, bool isAddMode)
        {
            bool success = false;

            if (!ModelState.IsValid)//Ref: base.IsAutoPostback() || //Request.Form["chkDone"] must be present
            {
                doAddEditPopulate();
                return View(claimObj);
            }
            //HT: Note the following won't work now as we insert a record in DB then get it back in edit mode for Async edit
            //bool isAddMode = (claimObj.ID <= Defaults.Integer); 

            #region Perform operation proceed and set result

            int result = new CAWclaim(IsAsync).AddEdit(claimObj, claimObj.StatusIDold);
            success = result > 0;

            if (!success) return View(claimObj);
            else //Log Activity based on mode
            {
                claimObj.ClaimNo = result;// Set Claim #
                ActivityLogService.Activity act = isAddMode ? ActivityLogService.Activity.ClaimAdd : ActivityLogService.Activity.ClaimEdit;
                new ActivityLogService(act).Add(new ActivityHistory() { ClaimID = result, ClaimText = claimObj.ClaimNo.ToString() });
            }

            #endregion

            base.operationSuccess = success;//Set opeaon success
            _Session.ResetClaimInSessionAndEmptyTempUpload(claimObj.ClaimGUID); // reset because going back to Manage will automatically creat new session

            return RedirectToAction("Manage", new { ClaimID = result});
        }

        [AccessClaim("ClaimID")]
        [CacheControl(HttpCacheability.NoCache), HttpGet]
        public ActionResult Archived(int ClaimID)
        {
            vw_Claim_Master_User_Loc vw = new ClaimService().GetClaimById(ClaimID);

            if (vw.ID == Defaults.Integer && vw.StatusID == Defaults.Integer && vw.AssignedTo == Defaults.Integer)
            { ViewData["Message"] = "Claim not found"; return View("DataNotFound"); /* deleted claim accessed from Log*/}
                        
            //Reset the Session Claim object
            Claim claimObj = ClaimService.GetClaimObjFromVW(vw);
            //_Session.Claim = claimObj;
            _Session.Claims[claimObj.ClaimGUID] = claimObj;// Populate original obj
            
            if (vw == new ClaimService().emptyView)//Empty so invalid ClaimID - go to Home
                return RedirectToAction("List", "Dashboard");

            return View(vw);
        }

        [HttpPost]
        [AccessClaim("ClaimID")]
        public ActionResult ChangeClaimStatus(int ClaimID, int OldStatusID, int NewStatusID)
        {
            bool result = false;
            if (OldStatusID != NewStatusID)
            {
                result = new StatusHistoryService().UpdateClaimStatus(ClaimID, OldStatusID, NewStatusID);
                //Log Activity (before directory del and sesion clearing)
                new ActivityLogService(ActivityLogService.Activity.ClaimEdit)
                    .Add(new ActivityHistory() { ClaimID = ClaimID, ClaimText = ClaimID.ToString() });
            }
            //Taconite XML
            return this.Content(Defaults.getTaconiteResult(result,
                Defaults.getOprResult(result, String.Empty), "msgStatusHistory", "updateStatusHistory()"), "text/xml");
        }

        #endregion        
                
        #region Actions for Status (Secured)
       
        [AccessClaim("ClaimID")]
        [CacheControl(HttpCacheability.NoCache), HttpGet]
        public ActionResult Status(int ClaimID, bool? Archived)
        {
            //http://localhost:4915/Claim/1/Status
            ViewData["IsReadOnly"] = (Archived.HasValue? Archived.Value: true);
            // NOT need because in MAnage claim we show it as readonly || _Session.Claim.Archived;
            return View(new StatusHistoryService().FetchAll(ClaimID));
        }
        
        #endregion

        #region Extra Functions (for Claim actions)
        public void doAddEditPopulate()
        {
            //ViewData["IsEditMode"] = (id != Defaults.Integer);

            ViewData["Statuses"] = new LookupService().GetLookup(LookupService.Source.Status);
            ViewData["Brands"] = !_Session.IsOnlyVendor?new LookupService().GetLookup(LookupService.Source.BrandItems):
                //Special case for Vendor users (they must see only their Brands)
                new LookupService().GetLookup(LookupService.Source.BrandVendorItems,extras:_SessionUsr.OrgID.ToString());
        }
        #endregion
    }
}
