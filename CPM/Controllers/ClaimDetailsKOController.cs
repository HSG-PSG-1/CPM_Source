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
        #region Actions for Items (ClaimDetail) - Secure

        [AccessClaim("ClaimID")]
        [CacheControl(HttpCacheability.NoCache), HttpGet]
        public ActionResult ItemsKO(int ClaimID, string ClaimGUID, int? DetailID)
        {
            ViewData["Brands"] = new LookupService().GetLookup(LookupService.Source.BrandItems); 
            return View();
        }

        [AccessClaim("ClaimID")]
        [CacheControl(HttpCacheability.NoCache), HttpGet]
        public ActionResult ItemsKOVM(int ClaimID)
        {
            //if (!(bool)ViewData["ShowGrid"])
            //    ViewData["Defects"] = new LookupService().GetLookup(LookupService.Source.Defect);
            
            //Set Item object
            ClaimDetail newObj = new ClaimDetail() { ID = 0, _Added = true, ClaimID = ClaimID, LastModifiedBy = _SessionUsr.ID, LastModifiedDate = DateTime.Now, Archived = false };

            List<ClaimDetail> items = new List<ClaimDetail>();
            try { items = ((List<ClaimDetail>)Session["Items_Demo"]); }catch (Exception ex) { items = null; }
            
            bool sendResult = (items != null && items.Count() > 0);
            if (sendResult) Session.Remove("Items_Demo");

            //if (newObj != null && string.IsNullOrEmpty(newObj.Comment1)) newObj.Comment1 = "";
            DAL.ItemKOModel vm = new ItemKOModel()
            {
                 ItemToAdd = newObj, EmptyItem = newObj,
                AllItems = (sendResult ? items : new CAWItem(false).Search(ClaimID, null, null))
            };

            // Lookup data
            vm.Defects = new LookupService().GetLookup(LookupService.Source.Defect);
            
            vm.showGrid = true;
            return Json(vm, JsonRequestBehavior.AllowGet);
        }
        
        [HttpPost]
        public ActionResult ItemsKO(int ClaimID, [FromJson] IEnumerable<ClaimDetail> items)
        {
            List<ClaimDetail> itemList = items.ToList();

            itemList.Add(new ClaimDetail() { Description = "I came from postback refresh! (to confirm a successful postback)", ItemCode = "Server postback" });

            Session["Items_Demo"] = itemList;
            ViewData["Brands"] = new LookupService().GetLookup(LookupService.Source.BrandItems);            
            
            return View();
        }
        
        #endregion
    }
}

namespace CPM.DAL
{
    public class ItemKOModel
    {
        public ClaimDetail EmptyItem { get; set; }
        public ClaimDetail ItemToAdd { get; set; }
        public List<ClaimDetail> AllItems { get; set; }
        public IEnumerable Defects { get; set; }
        public bool showGrid { get; set; }
    }
}
