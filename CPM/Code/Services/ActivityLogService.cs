using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Linq;
using System.Linq.Dynamic;
using System.Data.Linq.SqlClient;
using CPM.DAL;
using CPM.Helper;
using Webdiyer.WebControls.Mvc;

namespace CPM.Services
{
    public class ActivityLogService : _ServiceBase
    {
        #region Variables and Constructor
        
        public const string sortOn = " ActDateTime DESC, UserText, Activity ", sortOn1 = " ActDateTime DESC"; // Default for secondary sort;

        public vw_ActivityLog emptyView = new vw_ActivityLog() { ID = Defaults.Integer };        
        Activity Act = Activity.Login;
        Users Usr = new Users();

        public enum Activity
        {//Make sure this is Text/value(with ID) pair matches the MasterAcivity table

            //Try this while getting text if not possible in DB: 
            //http://stackoverflow.com/questions/272633/add-spaces-before-capital-letters
            Login=1,//Make sure the value matches with Activity Log
            Logout,
            ClaimAdd,
            ClaimEdit,
            ClaimDelete,
            ClaimFileUpload,
            ClaimPrint,
            UserAdd,
            UserEdit,
            UserDelete,
            MasterManage,
            RoleManage,
            ClaimArchive,
            ClaimUnarchive
        }

        public ActivityLogService(Activity activityToLog /*,vw_Users_Role_Org sessionUser*/)
        {
            Act = activityToLog;

            Usr.ID = _SessionUsr.ID; 
            Usr.Email = _SessionUsr.Email;
            Usr.Name = _SessionUsr.UserName;
        }

        public ActivityLogService(Activity activityToLog /*,vw_Users_Role_Org sessionUser*/, CPMmodel dbcExisting)
            : base(dbcExisting)
        {
            Act = activityToLog;

            Usr.ID = _SessionUsr.ID;
            Usr.Email = _SessionUsr.Email;
            Usr.Name = _SessionUsr.UserName;
        }

        #endregion

        #region Search / Fetch
        
        public PagedList<vw_ActivityLog> Search(string orderBy, int? pgIndex, int pageSize, vw_ActivityLog alog)
        {
            orderBy = string.IsNullOrEmpty(orderBy) ? sortOn : orderBy;

            using (dbc)
            {
                IQueryable<vw_ActivityLog> actHistoryQuery =  (from vw_u in dbc.vw_ActivityLogs orderby vw_u.ActivityID  select vw_u);
                //Get filters - if any
                actHistoryQuery = PrepareQuery(actHistoryQuery, alog);
                // Apply Sorting, Pagination and return PagedList
                return actHistoryQuery.OrderBy(orderBy).ToPagedList(pgIndex ?? 1, pageSize);
            }
        }

        public static IQueryable<vw_ActivityLog> PrepareQuery(IQueryable<vw_ActivityLog> actHistoryQuery, vw_ActivityLog alog)
        {
            #region Append WHERE clause if applicable

            // Append filter for User Text or ID
            if (!string.IsNullOrEmpty(alog.UserText))
                actHistoryQuery = actHistoryQuery.Where(o => SqlMethods.Like(o.UserText.ToUpper(), alog.UserText.ToUpper()));
            else if
                (alog.UserID > 0) actHistoryQuery = actHistoryQuery.Where(o => o.UserID == alog.UserID);
            // Append filter for Activity or ID
            if (!string.IsNullOrEmpty(alog.Activity))
                actHistoryQuery = actHistoryQuery.Where(o => SqlMethods.Like(o.Activity.ToUpper(), alog.Activity.ToUpper()));
            else if
                (alog.ActivityID > 0) actHistoryQuery = actHistoryQuery.Where(o => o.ActivityID == alog.ActivityID);
            // Append filter for Claim
            if (!string.IsNullOrEmpty(alog.ClaimText))
                actHistoryQuery = actHistoryQuery.Where(o => Defaults.stringToStrList(alog.ClaimText).Contains(o.ClaimText));
            // Append filter for File
            if (!string.IsNullOrEmpty(alog.FileName))
                actHistoryQuery = actHistoryQuery.Where(o => SqlMethods.Like(o.FileName.ToUpper(), alog.FileName.ToUpper()));

            //Apply date filter (http://www.filamentgroup.com/lab/date_range_picker_using_jquery_ui_16_and_jquery_ui_css_framework/)
            if (alog.ActDateFrom.HasValue)
                actHistoryQuery = actHistoryQuery.Where(o => o.ActDateTime.Date >= alog.ActDateFrom_SQL.Value.Date);
            if (alog.ActDateTo.HasValue)
                actHistoryQuery = actHistoryQuery.Where(o => o.ActDateTime.Date <= alog.ActDateTo_SQL.Value.Date);

            #endregion

            return actHistoryQuery;
        }

        public System.Collections.IEnumerable GetActivities()
        {
            //using (dbc) HT: DON'T coz dbc will be accessed from VIEW
            {
                var ActQ = from o in dbc.MasterActivities
                           orderby o.SortOrder, o.Title
                           select new { ID = o.ID, TEXT = o.Title };

                return ActQ;
            }
        }

        #endregion

        #region Add / Edit / Delete

        public int Add()
        {//By Default we submit data
            return Add(new ActivityHistory() { ActivityID = (int)this.Act }, true);
        }

        public int Add(ActivityHistory aHisObj, bool doSubmit = true)
        {
            aHisObj.ActivityID = (int)Act;
            //Set lastmodified fields
            aHisObj.UserID = Usr.ID;
            aHisObj.UserText = Usr.Name;
            aHisObj.ActDateTime = DateTime.Now;

            dbc.ActivityHistories.InsertOnSubmit(aHisObj);
            if(doSubmit)   dbc.SubmitChanges();

            return aHisObj.ID; // Return the 'newly inserted id'
        }

        public bool Delete(ActivityHistory aHisObj)
        {
            dbc.Users.DeleteOnSubmit(dbc.Users.Single(c => c.ID == aHisObj.ID));
            dbc.SubmitChanges();
            
            return true;
        }

        #endregion

        #region DeleteAllClaimActivities --  For now, we're NOT deleting any Activity Log records (kept for future)
        /*public bool DeleteAllClaimActivities(int ClaimID)
        {
            //Unable to create FK for vw_ActivityLog so will have to delete records explicitly
            var acts = dbc.ActivityHistories.Select(x => x).Where(a => a.ClaimID == ClaimID);
            dbc.ActivityHistories.DeleteAllOnSubmit<ActivityHistory>(acts);

            return true;
        } */
        #endregion

        #region Extra functions

        #endregion
    }
}
