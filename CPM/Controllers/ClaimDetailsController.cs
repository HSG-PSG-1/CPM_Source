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
        public ActionResult Items(int ClaimID, string ClaimGUID, int? DetailID)
        {
            //ViewData["Brands"] = new LookupService().GetLookup(LookupService.Source.BrandItems);
            //ViewData["ClaimGUID"] = ClaimGUID;
            return View();
        }
                
        public ItemKOModel GetItemKOModel(int ClaimID, string ClaimGUID)
        {
           //Set Item object
            ClaimDetail newObj = new ClaimDetail() { ID = 0, _Added = true, ClaimID = ClaimID, LastModifiedBy = _SessionUsr.ID, LastModifiedDate = DateTime.Now, Archived = false, aDFilesJSON = "" };

            ItemKOModel vm = new ItemKOModel()
            {
                 ItemToAdd = newObj, EmptyItem = newObj,
                 AllItems = new ClaimDetailService().Search(ClaimID, null)
            };

            // Lookup data
            vm.Defects = new LookupService().GetLookup(LookupService.Source.Defect);
            
            vm.showGrid = true;
            return vm;
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
