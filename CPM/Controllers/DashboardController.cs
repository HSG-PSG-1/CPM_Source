using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CPM.DAL;
using CPM.Services;
using CPM.Helper;
//using StackExchange.Profiling;

namespace CPM.Controllers
{
    //[CompressFilter] - DON'T
    [IsAuthorize(IsAuthorizeAttribute.Rights.NONE)]//Special case for some dirty session-abandoned pages and hacks
    public class DashboardController : BaseController
    {
        string view = _Session.IsOnlyCustomer ? "ListCustomer" : "ListInternal";

        public DashboardController() : //HT: Make sure this is initialized with default constructor values!
            base(Config.DashboardPageSize, DashboardService.sortOn, Filters.list.Dashboard) { ;}

        #region List Grid Excel
        //[CompressFilter] - DON'T
        public ActionResult List(int? index, string qData)
        {
            index = index ?? 0;
            //_Session.NewSort = DashboardService.sortOn1; _Session.OldSort = string.Empty;//Initialize (only once)
            //base.SetSearchOpts(index.Value);
            //Special case: Set the filter back if it existed so that if the user "re-visits" the page he gets the previous filter (unless reset or logged off)
            searchOpts = _Session.Search[Filters.list.Dashboard];

            populateData(true);
            
            // No need to return view - it'll fetched by ajax in partial rendering
            return View();
        }

        #region Will need GET (for AJAX) & Post
        [CacheControl(HttpCacheability.NoCache)]//Don't mention GET or post as this is required for both!
        public PartialViewResult ClaimList(int? index, string qData)
        {
            base.SetTempDataSort(ref index);// Set TempDate, Sort & index
            //Make sure searchOpts is assigned to set ViewState
            populateData(false);
            
            //var profiler = MiniProfiler.Current; // it's ok if this is null

            List<vw_Claim_Dashboard> result = new List<vw_Claim_Dashboard>();
            //using (profiler.Step("Fetch Dashboard Data"))
            {
                result = new DashboardService().Search(sortExpr, index, gridPageSize, (vw_Claim_Dashboard)searchOpts, false, _Session.IsOnlyCustomer);
            }
                return PartialView("~/Views/Dashboard/EditorTemplates/" +
                    (_Session.IsOnlyCustomer ? "ListCustomer.cshtml" : "ListInternal.cshtml"), result);
            
        }
        #endregion

        [HttpPost]
        [SkipModelValidation]//HT: Use with CAUTION only meant for POSTBACK search Action
        public ActionResult List(vw_Claim_Dashboard searchObj, string doReset, string qData)
        {
            searchOpts = (doReset == "on") ? new vw_Claim_Dashboard() : searchObj; // Set or Reset Search-options
            populateData(true);// Populate ddl Viewdata
            //AT PRESENT ONLY 'RESET' & 'SEARCH' come here so need to reset
            //_Session.NewSort = _Session.OldSort = string.Empty;//Set sort variables
            
            TempData["SearchData"] = searchObj;// To be used by partial view
            return RedirectToAction("ClaimList"); //Though ajaxified but DON'T return - return View();
        }

        public ActionResult Excel()
        {
            //HttpContext context = ControllerContext.HttpContext.CurrentHandler;
            //Essense of : http://stephenwalther.com/blog/archive/2008/06/16/asp-net-mvc-tip-2-create-a-custom-action-result-that-returns-microsoft-excel-documents.aspx
            this.Response.Clear();
            this.Response.AddHeader("content-disposition", "attachment;filename=" + "Dashboard_" + _SessionUsr.ID + ".xls");
            this.Response.Charset = "";
            this.Response.Cache.SetCacheability(HttpCacheability.NoCache);
            this.Response.ContentType = "application/vnd.ms-excel";

            //DON'T do the following
            //this.Response.Write(content);
            //this.Response.End();

            populateData(false);

            return View(new DashboardService().Search(sortExpr, 1, gridPageSize, (vw_Claim_Dashboard)searchOpts, true, _Session.IsOnlyCustomer));
        }

        public ActionResult ExcelPDF()
        {   
            populateData(false);
            List<vw_Claim_Dashboard> printView = new DashboardService().Search(sortExpr, 1, gridPageSize, (vw_Claim_Dashboard)searchOpts, true, _Session.IsOnlyCustomer);
            
            string GUID = _SessionUsr.ID.ToString();
            return new ReportManagement.StandardPdfRenderer().BinaryPdfData(this, "Dashboard" + GUID, "Excel", printView);
        }
        #endregion

        #region Dialog Actions
        [AccessClaim("ClaimID")]
        [CacheControl(HttpCacheability.NoCache), HttpGet]
        public ActionResult Comments(int ClaimID)
        {
            return View(new CommentService().Search(ClaimID, null));
        }

        [AccessClaim("ClaimID")]
        [CacheControl(HttpCacheability.NoCache), HttpGet]
        public ActionResult Files(int ClaimID)
        {
            return View(new FileHeaderService().Search(ClaimID, null));
        }

        [CacheControl(HttpCacheability.NoCache), HttpGet]
        public ActionResult Status(int ClaimID, bool Archived)
        {//Redirect to Claim\X\Status?Archived = true (ref: http://forums.asp.net/t/1202550.aspx/1)
            return RedirectToAction("Status", "Claim", new { ClaimID = ClaimID, Archived = Archived.ToString()});
            //?Archived=" + Archived.ToString()
        }
        #endregion

        #region Extra Functions

        public void populateData(bool fetchOtherData)
        {
            //using (MiniProfiler.Current.Step("Populate lookup Data"))
            {
                vw_Claim_Dashboard searchOptions = (vw_Claim_Dashboard)(searchOpts);
                if (_Session.IsOnlyCustomer) searchOptions.CustID = _SessionUsr.OrgID;//Set the cust filter
                if (_Session.IsOnlyVendor) searchOptions.VendorID = _SessionUsr.OrgID;//Set the Vendor filter
                if (_Session.IsOnlySales) searchOptions.SalespersonID = _SessionUsr.ID;//Set the Sales filter

                if (fetchOtherData)
                    ViewData["Status"] = new LookupService().GetLookup(LookupService.Source.Status);
            }
        }

        #endregion
    }
}
