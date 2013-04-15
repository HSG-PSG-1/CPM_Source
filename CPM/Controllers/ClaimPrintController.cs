using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CPM.DAL;
using CPM.Services;
using CPM.Helper;
using ReportManagement;

namespace CPM.Controllers
{
    //[CompressFilter] - don't use it here
    [IsAuthorize(IsAuthorizeAttribute.Rights.NONE)]//Special case for some dirty session-abandoned pages and hacks
    public partial class ClaimController : BaseController
    {
        #region Actions for Claim (Secured)
        
        [AccessClaim("ClaimID")]
        [CacheControl(HttpCacheability.NoCache), HttpGet]
        public ActionResult Print(int ClaimID)
        {
            ClaimInternalPrint printView = new ClaimInternalPrint();

            List<Comment> comments = new List<Comment>();
            List<FileHeader> filesH = new List<FileHeader>();
            List<ClaimDetail> items = new List<ClaimDetail>();

            #region Fetch Claim data and set Viewstate
            vw_Claim_Master_User_Loc vw = new ClaimService().GetClaimByIdForPrint(ClaimID,
                ref comments, ref filesH, ref items, !_Session.IsOnlyCustomer);
            //Set data in View
            ViewData["comments"] = comments;
            ViewData["filesH"] = filesH;
            ViewData["items"] = items;

            printView.view = vw;
            printView.comments = comments;
            printView.filesH = filesH;
            printView.items = items;
            #endregion

            //Log Activity
            new ActivityLogService(ActivityLogService.Activity.ClaimPrint).
                Add(new ActivityHistory() { ClaimID = ClaimID, ClaimText = vw.ClaimNo.ToString() });

            #region Return view based on user type

            if (_Session.IsOnlyCustomer) return View("PrintCustomer", printView); //return View("NoAccess");
            else if (_Session.IsOnlyVendor) return View("PrintVendor", printView);
            else if (_Session.IsInternal) return View("PrintInternal", printView); // View(vw);
            else return View(vw);

            #endregion
        }

        [AccessClaim("ClaimID")]
        [CacheControl(HttpCacheability.NoCache), HttpGet]
        public ActionResult PrintPDF(int ClaimID)
        {

            ClaimInternalPrint printView = new ClaimInternalPrint();

            List<Comment> comments = new List<Comment>();
            List<FileHeader> filesH = new List<FileHeader>();
            List<ClaimDetail> items = new List<ClaimDetail>();

            #region Fetch Claim data and set Viewstate
            vw_Claim_Master_User_Loc vw = new ClaimService().GetClaimByIdForPrint(ClaimID,
                ref comments, ref filesH, ref items, !_Session.IsOnlyCustomer);
            //Set data in View
            ViewData["comments"] = comments;
            ViewData["filesH"] = filesH;
            ViewData["items"] = items;

            printView.view = vw;
            printView.comments = comments;
            printView.filesH = filesH;
            printView.items = items;
            #endregion

            //Log Activity
            new ActivityLogService(ActivityLogService.Activity.ClaimPrint).
                Add(new ActivityHistory() { ClaimID = ClaimID, ClaimText = vw.ClaimNo.ToString() });

            #region Return view based on user type
            /*
            if (_Session.IsOnlyCustomer) return View("PrintCustomer", vw); //return View("NoAccess");
            else if (_Session.IsOnlyVendor) return View("PrintVendor", vw);
            else if (_Session.IsInternal) return View("PrintInternal", vw); // View(vw);
            else return View(vw);
            */
            #endregion

            //return this.ViewPdf("Claim details", "PrintCustomer", "PrintVendor", printView, printView);
            string GUID = printView.view.ID.ToString();

            return new StandardPdfRenderer().BinaryPdfData(this,"ClaimPrint" + GUID, "PrintInternal", printView);
        }
        

        #endregion        

        
        

        
    }
}
