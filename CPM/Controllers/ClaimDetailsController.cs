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
        #region Actions for Items (ClaimDetail) - Secure

        [AccessClaim("ClaimID")]
        [CacheControl(HttpCacheability.NoCache), HttpGet]
        public ActionResult Items(int ClaimID, string ClaimGUID, int? DetailID)
        {//url: /Claim/1/Detail/2?

            ClaimDetail newObj = new ClaimDetail();
            if (TempData["PRGModel"] != null)
                new CPM.Models.PRGModel(TempData["PRGModel"]).ExtractData<ClaimDetail>(ref newObj, ModelState);
            //if (TempData["ViewData"] != null)ViewData = (ViewDataDictionary)TempData["ViewData"]; //SPECIAL: required for when redirected from Invalid Add attempt
            else
            {//HT:  set the DetailObj ONLY if its NOT a validation postback
                //Set ClaimDetail object
                newObj = new CAWItem(IsAsync).GetClaimDetailById(DetailID, _Session.Claims[ClaimGUID]);                
            }

            #region set viewdata for showGrid, return Single Item or List of Item
            ViewData["DetailObj"] = newObj;
            ViewData["ShowGrid"] = (DetailID == null);//Useful in View
            ViewData["ClaimGUID"] = ClaimGUID;//Useful in View

            if (!(bool)ViewData["ShowGrid"])
                ViewData["Defects"] = new LookupService().GetLookup(LookupService.Source.Defect);
            //For future -- GetGroupedDefectList(new LookupService().GetLookup(LookupService.Source.DefectGrouped));
            else
                return View(new CAWItem(IsAsync).Search(ClaimID, ClaimGUID, null));// Mind it - we're NOT setting ClaimGUID (in view it is expected as a ViewData child property
            
            #endregion

            return View(new List<ClaimDetail>()); //Grid is hidden
        }

        [SkipModelValidation]
        [AccessClaim("ClaimID")]
        [HttpPost]
        public ActionResult ItemDelete(int ClaimID, int DetailID, string ClaimGUID)
        {
            new CAWItem(IsAsync).Delete(new ClaimDetail() { ID = DetailID, ClaimGUID = ClaimGUID });
            //Taconite XML
            return this.Content(Defaults.getTaconite(true, Defaults.getOprResult(true, ""), "itmOprMsg", true), "text/xml");             
        }

        [HttpPost]
        public ActionResult Items(int ClaimID, ClaimDetail DetailObj)
        {//Make sure ClaimGUID is set & maintained!
            ViewData["DetailObj"] = DetailObj;

            #region Process based on ModelState

            if (ModelState.IsValid)
            {
                int id = DetailObj.ID;
                int newId = new CAWItem(IsAsync).AddEdit(DetailObj);

                #region Required if we need to support file upload from within Item Entry
                /* Disabled for now as per William's suggestion - 07-Sep-2012
                if (id <= Defaults.Integer)
                {//For now this is available only for Add mode because
                 //for Edit mode we're not sure how many and which type of files are uploaded by the user
                    ProcessDetailFiles(true, "FileSerial", ClaimID, newId);
                    ProcessDetailFiles(false, "FileDOT", ClaimID, newId);
                }
                */
                #endregion

                return RedirectToAction("Items", new { ClaimGUID = DetailObj.ClaimGUID });
            }
            else
            {
                //http://stackoverflow.com/questions/279665/how-can-i-maintain-modelstate-with-redirecttoaction
                //TempData["ViewData"] = ViewData;//Store in temp intermediate variable
                TempData["PRGModel"] = new CPM.Models.PRGModel().SetPRGModel<ClaimDetail>(DetailObj, ModelState);
                return RedirectToAction("Items", new { DetailID = DetailObj.ID.ToString(), ClaimGUID = DetailObj.ClaimGUID });
            }

            #endregion
        }

        [CacheControl(HttpCacheability.NoCache), HttpGet]
        [AccessClaim("ClaimID")]
        public ActionResult ItemsArchived(int ClaimID, string ClaimGUID, int? DetailID)
        {
            ViewData["ClaimGUID"] = ClaimGUID;//To be used in archived view
            return View(new ClaimDetailService().Search(ClaimID, null));
        }

        /* HT: IMP for future usage f Grouped Dropdown
         public IEnumerable<GroupDropListItem> GetGroupedDefectList(System.Collections.IEnumerable defectsColl)
        {// DON'T forget to SORT by Category !!!
            List<MasterDefect> defects = defectsColl.Cast<MasterDefect>().ToList();
            List<GroupDropListItem> items = new List<GroupDropListItem>();

            string oldCategory = " ";//To avoid empty == empty
            foreach (MasterDefect defect in defects)
            {
                if (oldCategory != (defect.Category??"").Trim())
                {//Process parent
                    GroupDropListItem parent = new GroupDropListItem();
                    parent.Name = defect.Category;
                    parent.Items = new List<OptionItem>();//DON'T forget to initialize or it'll return exception while adding
                    //fetch and add child records
                    List<MasterDefect> children = (from d in defects where d.Category == defect.Category 
                                                   orderby d.SortOrder select d).ToList();
                    foreach (MasterDefect child in children)
                        parent.Items.Add(new OptionItem() { Text = child.Title, Value = child.ID.ToString() });
                    //finally add parent to the items list
                    items.Add(parent);
                    //set existing category to skip other records
                    oldCategory = defect.Category;
                }
            }

            return items;
        }
        */
        #endregion
    }
}
