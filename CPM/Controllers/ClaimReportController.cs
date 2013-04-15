using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CPM.Services;
using CPM.DAL;
using CPM.Helper;
namespace CPM.Controllers
{
    public partial class ReportController: BaseController
    {
        #region Claim Reports

        public ActionResult ClaimData(string reportStr)
        {
            reportStr = string.IsNullOrEmpty(reportStr) ? ReportingService.Reports.ClaimStatus.ToString() : reportStr;
            //Unlike search pages, here we've to explicit empty the prev searchOpts
            searchOpts = new ReportingService().emptyClaimView;
            //Populate ddl Viewdata
            populateClaimData(searchOpts, true);
            // No need to return view - it'll fetched by ajax in partial rendering
            return View(new ClaimRpt() { Report = reportStr, SearchData = searchOpts });
        }

        #region Will need GET (for AJAX) & Post
        [CacheControl(HttpCacheability.NoCache)]//Don't mention GET or post as this is required for both!
        public PartialViewResult ClaimReport(string reportStr, int? data)
        {
            //Make sure searchOpts is assigned to set ViewState
            ReportingService.Reports report = ReportingService.Reports.ClaimStatus;// Default
            try { report = _Enums.ParseEnum<ReportingService.Reports>(reportStr); }
            catch { ;}

            ViewData["SearchData"] = TempData["SearchData"]; // Fetch and set viewstate
            //Make sure searchOpts is assigned to set ViewState
            populateClaimData(searchOpts, false);

            #region Select and render report
            switch (report)
            {
                case ReportingService.Reports.ClaimStatus:
                    return PartialView(claimRptPath + "StatusClaim.cshtml",
                        new ReportingService().GetStatusClaimCount((vw_Claim_Dashboard)searchOpts));
                case ReportingService.Reports.BrandClaim:
                    return PartialView(claimRptPath + "BrandClaim.cshtml",
                        new ReportingService().GetBrandClaimCount((vw_Claim_Dashboard)searchOpts));
                case ReportingService.Reports.CustClaim:
                    return PartialView(claimRptPath + "CustClaim.cshtml",
                        new ReportingService().GetCustClaimCount((vw_Claim_Dashboard)searchOpts));
                case ReportingService.Reports.YearlyClaim:
                    return PartialView(claimRptPath + "YearlyClaim.cshtml",
                        new ReportingService().GetYearlyClaimCount());
                case ReportingService.Reports.YearClaimItem:
                    ViewData["Yr"] = data.Value;
                    return PartialView(claimRptPath + "YearClaimItem.cshtml",
                        new ReportingService().GetYearClaimItems(data.Value));
                
                default:
                    return PartialView(claimRptPath + "StatusClaim.cshtml", new ReportingService().GetStatusClaimCount((vw_Claim_Dashboard)searchOpts));
            }

            #endregion
        }

        #endregion

        [HttpPost]
        [SkipModelValidation]//HT: Use with CAUTION only meant for POSTBACK search Action        
        public ActionResult ClaimData(vw_Claim_Dashboard searchObj, string doReset, string reportStr)
        {
            string report = string.IsNullOrEmpty(reportStr) ? ReportingService.Reports.ClaimStatus.ToString() : reportStr;
            searchOpts = (doReset == "on") ? new vw_Claim_Dashboard() : searchObj; // Set or Reset Search-options
            populateClaimData(searchOpts, true);// Populate ddl Viewdata

            TempData["SearchData"] = searchObj;// To be used by partial view
            return RedirectToAction("ClaimReport", new { reportStr = report });//Though ajaxified but DON'T return - return View();
        }

        #endregion

        #region Extra functions

        public void populateClaimData(object searchObj, bool fetchOtherData)
        {
            vw_Claim_Dashboard searchOptions = (vw_Claim_Dashboard)searchObj;// searchOpts;
            if (_Session.IsOnlyCustomer) searchOptions.CustID = _SessionUsr.OrgID;//Set the cust filter
            if (_Session.IsOnlySales) searchOptions.SalespersonID = _SessionUsr.ID;//Set the Sales filter

            if (fetchOtherData)
                ViewData["Status"] = new LookupService().GetLookup(LookupService.Source.Status);
        }

        #endregion
    }
}