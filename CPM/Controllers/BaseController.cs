using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CPM.Helper;

namespace CPM.Controllers
{
    [CompressFilter]
    //[HandleError] - handled in Application_Error
    public class BaseController : Controller
    {
        #region Variables & properties
        //HT: Make sure these two variables are populated in child controller
        public int gridPageSize = 2;
        private string sortOn = "";
        public Filters.list filter = Filters.list._None;
        private object searchObj = Filters.empty;
               
         /// <summary>
        ///  Get sort expression from querysting
        /// </summary>
        public string sortExpr
        {
            get
            {
                string prevSort = QryString.OldSort;// _Session.NewSort;                
                //string sidx = QryString.sidx, sord = QryString.sord ?? QryString.asc;
                string sidx = QryString.NewSort;

                if (string.IsNullOrEmpty(sidx)) { return sortOn; } //_Session.OldSort = "";

                return sidx //+ " " + sord /* Append prev sort if its not the null*/
                    + (string.IsNullOrEmpty(prevSort) || prevSort.Contains(sidx + " ")
                    ? "" : ("," + prevSort));
            }
        }

        /// <summary>
        /// Stores the search filters. Make sure this is accessed as GET to set the ViewState
        /// </summary>
        public object searchOpts
        {
            set
            {
                if (filter == Filters.list._None)
                    System.Web.HttpContext.Current.Session["SearchOpts"] = value;
                else
                    _Session.Search[filter] = value;

                ViewData["SearchData"] = value;
            }
            get
            {/* Set in Viewdata for filter-controls to be populated! */
                object opts;
                if (filter == Filters.list._None) opts = (System.Web.HttpContext.Current.Session["SearchOpts"]) ?? searchObj;
                else opts = _Session.Search[filter];

//                ViewData["SearchData"] = opts;
                return opts;
            }
        }

        /// <summary>
        /// TempData["oprSuccess"] - will be reset upon first access
        /// </summary>
        public object operationSuccess
        {
            set { TempData["oprSuccess"] = value; }
            get
            {/* Set in Viewdata for filter-controls to be populated! */
                object opr = TempData["oprSuccess"];
                TempData["oprSuccess"] = null;//reset to avoid reusage
                return opr;
            }
        }

        #endregion

        public BaseController() { ;}

        public BaseController(int gridPgSize, string defaultSort, object searchObject)
        {
            gridPageSize = gridPgSize;
            sortOn = defaultSort;
            searchObj = searchObject;
            //DON'T reset-_Session.PrevSort = "";
        }

        public BaseController(int gridPgSize, string defaultSort, Filters.list searchfilter)
        {
            gridPageSize = gridPgSize;
            sortOn = defaultSort;
            filter = searchfilter;
            //DON'T reset-_Session.PrevSort = "";
        }

        #region Extra Functions

        /// <summary>
        /// Make sure the View has a checkbox "chkDone"
        /// </summary>
        /// <returns>True if chkDone="On" & also does ModelState.Clear(). Else false</returns>
        public bool IsAutoPostback()
        {
            if (Request.Form["chkDone"] != "on")
            {
                ModelState.Clear();//Clear errors if 
                return true;
            }
            else
                return false;
        }

        /// <summary>
        /// Default search opts will be taken from searchObj (initialized in constructor) and so will be the default sort
        /// </summary>
        /// <param name="index">page index</param>
        public object SetSearchOpts(int index)
        {
            if (index == 0 && sortExpr == sortOn)//Initialize only the first time (not a paging/sort)
                searchOpts = searchObj;

            return searchOpts;//Make sure searchOpts GET is accessed so that ViewState data is set
        }

        /// <summary>
        /// Called from partial result returning actions to set TempDate from viewstate, sort & index
        /// </summary>
        /// <param name="index">Nullable page index</param>
        public void SetTempDataSort(ref int? index)
        {
            ViewData["SearchData"] = TempData["SearchData"]; // Fetch and set viewstate
            bool isSort = (index == null);// we return page index null when we sort
            ViewData["SetPrevSort"] = isSort;
            //if (!isSort) _Session.NewSort = _Session.OldSort;//special case
            index = index ?? 0;
        }

        #endregion
    }
}
