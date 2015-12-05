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
    public partial class DashboardController : BaseController
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
            searchOpts = _Session.Search[Filters.list.Dashboard];//new vw_Claim_Dashboard();

            populateData(true);
            ViewData["gridPageSize"] = gridPageSize; // Required to adjust pagesize for grid

            // No need to return view - it'll fetched by ajax in partial rendering
            return View();
        }

        #region Will need GET (for AJAX) & Post
        
        [CacheControl(HttpCacheability.NoCache)]//Don't mention GET or post as this is required for both!
        public ContentResult ClaimListKO(int? index, string qData, bool? fetchAll)
        {
            base.SetTempDataSort(ref index);// Set TempDate, Sort & index
            //Make sure searchOpts is assigned to set ViewState
            vw_Claim_Dashboard oldSearchOpts = (vw_Claim_Dashboard)searchOpts;
            searchOpts = new vw_Claim_Dashboard() { Archived = oldSearchOpts.Archived };// CAUTION: otehrwise archived saved search will show null records
            populateData(false);

            index = (index > 0) ? index + 1 : index; // paging starts with 2

            var result = from vw_u in new DashboardService().SearchKO(
                sortExpr, index, gridPageSize * 2, (vw_Claim_Dashboard)searchOpts, fetchAll ?? false, _Session.IsOnlyCustomer)
                         select new
                         {
                             ID = vw_u.ID,
                             CNo = vw_u.ClaimNo,
                             StatusID = vw_u.StatusID,
                             AsgnTo = vw_u.AssignToName,
                             CustRef = vw_u.CustRefNo,
                             Brand = vw_u.BrandName,
                             CustOrg = vw_u.CustOrg,
                             SP = vw_u.Salesperson,
                             CDate = vw_u.ClaimDateOnly,
                             Archvd = vw_u.Archived,
                             Status = vw_u.Status,
                             Cmts = vw_u.CommentsExist,
                             Files = vw_u.FilesHExist,
                             CDtTxt = vw_u.ClaimDateTxt,//ClaimDate.ToString(Defaults.dtFormat, Defaults.ci),
                             ShpLocCode = vw_u.ShipToLocAndCode
                         };

            //return Json(new { records = result, search = oldSearchOpts }, JsonRequestBehavior.AllowGet);
            
            System.Web.Script.Serialization.JavaScriptSerializer jsSerializer =
                new System.Web.Script.Serialization.JavaScriptSerializer { MaxJsonLength = Int32.MaxValue }; //Json(new { records = result, search = oldSearchOpts }, JsonRequestBehavior.AllowGet);
            var jsonDataSet = new ContentResult
            {
                Content = jsSerializer.Serialize(new { records = result, search = oldSearchOpts }),
                ContentType = "application/json",
                //ContentEncoding = 
            };
            // MVC 4 : jsonResult.MaxJsonLength = int.MaxValue;
            return jsonDataSet;
        }

        [CacheControl(HttpCacheability.NoCache)]//Don't mention GET or post as this is required for both!
        public ContentResult ClaimListArchivedKO(bool? archived)
        {
            vw_Claim_Dashboard oldSearchOpts = (vw_Claim_Dashboard)searchOpts;
            bool oldSessionVal = oldSearchOpts.Archived;
            oldSearchOpts.Archived = archived.HasValue ? archived.Value : false;

            var result = from vw_u in new DashboardService().SearchKO(
                sortExpr, 0, gridPageSize * 2, oldSearchOpts, true, _Session.IsOnlyCustomer)
                         select new
                         {
                             ID = vw_u.ID,
                             CNo = vw_u.ClaimNo,
                             StatusID = vw_u.StatusID,
                             AsgnTo = vw_u.AssignToName,
                             CustRef = vw_u.CustRefNo,
                             Brand = vw_u.BrandName,
                             CustOrg = vw_u.CustOrg,
                             SP = vw_u.Salesperson,
                             CDate = vw_u.ClaimDateOnly,
                             Archvd = vw_u.Archived,
                             Status = vw_u.Status,
                             Cmts = vw_u.CommentsExist,
                             Files = vw_u.FilesHExist,
                             CDtTxt = vw_u.ClaimDateTxt,//ClaimDate.ToString(Defaults.dtFormat, Defaults.ci),
                             ShpLocCode = vw_u.ShipToLocAndCode                             
                         };

            //searchOpts1.Archived = oldSessionVal;//reset
            //return Json(result, JsonRequestBehavior.AllowGet);
            System.Web.Script.Serialization.JavaScriptSerializer jsSerializer = 
                new System.Web.Script.Serialization.JavaScriptSerializer { MaxJsonLength = Int32.MaxValue }; //Json(new { records = result, search = oldSearchOpts }, JsonRequestBehavior.AllowGet);
            var jsonDataSet = new ContentResult
            {
                Content = jsSerializer.Serialize(new { records = result, search = oldSearchOpts }),
                ContentType = "application/json",
                //ContentEncoding = 
            };
            // MVC 4 : jsonResult.MaxJsonLength = int.MaxValue;
            return jsonDataSet;
        }

        [HttpPost]
        [SkipModelValidation]//HT: Use with CAUTION only meant for POSTBACK search Action
        public ActionResult ClaimListKO(vw_Claim_Dashboard searchObj, string doReset, string qData, bool? fetchAll)
        {
            searchOpts = (doReset == "on") ? new vw_Claim_Dashboard() : searchObj; // Set or Reset Search-options
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
        [SkipModelValidation]
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
            var result = new DashboardService().Search(sortExpr, 1, gridPageSize, (vw_Claim_Dashboard)searchOpts, true, _Session.IsOnlyCustomer);

            searchOpts = new vw_Claim_Dashboard();
            populateData(false);

            return View("Excel", result);
        }

        [HttpGet]
        public ActionResult Excel(string dummy)
        { // special case handling for sessiontimeout while loading excel download or user somehow trying to access the excel directly. SO : 16658020
            return RedirectToAction("List", "Dashboard");
        }

        public ActionResult ExcelPDF()
        {   
            populateData(false);
            List<vw_Claim_Dashboard> printView = new DashboardService().Search(sortExpr, 1, gridPageSize, (vw_Claim_Dashboard)searchOpts, true, _Session.IsOnlyCustomer);
            
            string GUID = _SessionUsr.ID.ToString();
            return new ReportManagement.StandardPdfRenderer().BinaryPdfData(this, "Dashboard" + GUID, "Excel", printView);
        }

        [OutputCacheAttribute(VaryByParam = "*", Duration = 0, NoStore = true)] // disable caching SO : 12948156
        public ActionResult ClaimWithDetails()
        {
            /*if (_Session.IsInternal && _SessionUsr.RoleID == (int)SecurityService.Roles.Admin)
            {
                ViewData["Message"] = "You do not have access to Claim with details report.";
                return RedirectToAction("NoAccess", "Common");
            }*/

            //HttpContext context = ControllerContext.HttpContext.CurrentHandler;
            //Essense of : http://stephenwalther.com/blog/archive/2008/06/16/asp-net-mvc-tip-2-create-a-custom-action-result-that-returns-microsoft-excel-documents.aspx
            this.Response.Clear();
            this.Response.AddHeader("content-disposition", "attachment;filename=" + "ClaimWithDetails.xls");
            this.Response.Charset = "";
            this.Response.Cache.SetCacheability(HttpCacheability.NoCache);
            this.Response.ContentType = "application/vnd.ms-excel";

            //DON'T do the following
            //this.Response.Write(content);
            //this.Response.End();

            var result = new DashboardService().ClaimWithDetails();
            return View("ClaimWithDetails", result);
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
