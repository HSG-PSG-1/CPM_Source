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
    public class ReportingService : _ServiceBase
    {
        #region Variables and Constructor

        public const string countStr = " ({0})"; // append to the grouped field for legend display

        public const string activitySortOn = " Activity ";
        public vw_ActivityLog emptyActView = new vw_ActivityLog() { ID = Defaults.Integer };

        public const string claimSortOn = " ID ";
        public vw_Claim_Dashboard emptyClaimView = new vw_Claim_Dashboard() { ID = Defaults.Integer };

        public const string userSortOn = " ID ";
        public vw_Users_Role_Org emptyUserView = new vw_Users_Role_Org() { ID = Defaults.Integer };

        public ReportingService() : base() { ;}
        public ReportingService(CPMmodel dbcExisting) : base(dbcExisting) { ; }

        public enum Reports
        { 
            ActivityCount,
            UserwiseActivity,
            MonthlyUserActivity,
            RolewiseUser,
            OrgTypewiseUser,
            ClaimStatus,
            BrandClaim,
            CustClaim,
            YearlyClaim,
            YearClaimItem
        }

        #endregion

        #region Search / Fetch

        public List<vw_ActivityLog> SearchActivity(string orderBy, vw_ActivityLog alog)
        {
            orderBy = string.IsNullOrEmpty(orderBy) ? activitySortOn : orderBy;

            using (dbc)
            {
                IQueryable<vw_ActivityLog> actHistoryQuery = (from vw_u in dbc.vw_ActivityLogs orderby vw_u.ActivityID select vw_u);
                //Get filters - if any
                actHistoryQuery = ActivityLogService.PrepareQuery(actHistoryQuery, alog);
                // Sort and return list
                return actHistoryQuery.OrderBy(orderBy).ToList();
            }
        }

        public List<vw_Users_Role_Org> SearchUser(string orderBy, vw_Users_Role_Org usr)
        {
            orderBy = string.IsNullOrEmpty(orderBy) ? userSortOn : orderBy;

            using (dbc)
            {
                IQueryable<vw_Users_Role_Org> userQuery = (from vw_u in dbc.vw_Users_Role_Orgs select vw_u);
                //Get filters - if any
                userQuery = UserService.PrepareQuery(userQuery, usr);
                // Apply Sorting, Pagination and return PagedList
                return userQuery.OrderBy(orderBy).ToList();
            }
        }

        public List<vw_Claim_Dashboard> SearchClaim(string orderBy, vw_Claim_Dashboard das)
        {
            orderBy = string.IsNullOrEmpty(orderBy) ? userSortOn : orderBy;

            using (dbc)
            {
                IQueryable<vw_Claim_Dashboard> dasQ = (from vw_u in dbc.vw_Claim_Dashboards select vw_u);
                //Get filters - if any
                dasQ = DashboardService.PrepareQuery(dasQ, das);
                // Apply Sorting, Pagination and return PagedList
                return dasQ.OrderBy(orderBy).ToList();
            }
        }

        #endregion

        #region Activity Reports

        /* Group by sample
         * var result = from i in
               (from uh in db.UserHistories
                where uh.User.UserID == UserID && uh.CRMEntityID == (int)entity
                select new { uh.ActionID, uh.ActionType, uh.ObjectID, uh.Date })
             group i by new { i.ActionID, i.ActionType, i.ObjectID } into g
             select new { g.Key.ActionID, g.Key.ActionType, g.Key.ObjectID, g.Max(uh=>uh.Date) }; 
        */
        public List<ActivityRpt.ActivityCount> GetActivityCount(vw_ActivityLog alog)
        {
            List<vw_ActivityLog> actList = SearchActivity(activitySortOn, alog);
            List<ActivityRpt.ActivityCount> actCountLst = new List<ActivityRpt.ActivityCount>(actList.Count);
            //Get grouped data
            var grouped = from al in actList
                          group al by new {al.ActivityID, al.Activity} into g                          
                          orderby g.Key.Activity
                          select new 
                          {ActivityID = g.Key.ActivityID, Activity= g.Key.Activity, Count = g.Count() };
            //Populate ActivityCount list
            foreach (var act in grouped)
                actCountLst.Add(new ActivityRpt.ActivityCount() 
                { ActivityID = act.ActivityID, Activity = act.Activity, Count = act.Count });

            return actCountLst;
        }

        public List<ActivityRpt.UserActivity> GetUserwiseActivity(vw_ActivityLog alog)
        {
            List<vw_ActivityLog> actList = SearchActivity(activitySortOn, alog);
            List<ActivityRpt.UserActivity> usrActLst = new List<ActivityRpt.UserActivity>(actList.Count);
            //Get grouped data
            var grouped = from al in actList
                          group al by new { al.UserID, al.UserText } into g
                          orderby g.Key.UserText
                          select new { UserID = g.Key.UserID, Email = g.Key.UserText, Count = g.Count() };
            //Populate ActivityCount list
            foreach (var act in grouped)
                usrActLst.Add(new ActivityRpt.UserActivity() 
                { UserID = act.UserID, Email = act.Email, Count = act.Count });

            return usrActLst;
        }

        public List<ActivityRpt.MonthlyUserAct> GetMonthlyUserActivity(vw_ActivityLog alog)
        {//Ref: http://stackoverflow.com/questions/482912/sql-group-by-year-month-week-day-hour-sql-vs-procedural-performance
            List<vw_ActivityLog> actList = SearchActivity(activitySortOn, alog);
            List<ActivityRpt.MonthlyUserAct> actCountLst = new List<ActivityRpt.MonthlyUserAct>(actList.Count);
            //Get grouped data
            var grouped = from al in actList
                          // Ref: http://stackoverflow.com/questions/2105208/linq-group-by-month-question
                          group al by new { al.ActDateTime.Year, al.ActDateTime.Month } into g
                          orderby g.Key.Year, g.Key.Month
                          select new { YrMonth = new DateTime(g.Key.Year, g.Key.Month, 1), Count = g.Count() };
            //Populate ActivityCount list
            foreach (var act in grouped)
                actCountLst.Add(new ActivityRpt.MonthlyUserAct() { YrMonth = act.YrMonth, Count = act.Count });

            return actCountLst; // append  + string.Format(countStr, g.Count()) before rendering view
        }

        #endregion

        #region User Reports

        public List<UserRpt.UserRole> GetRoleUserCount(vw_Users_Role_Org usr)
        {
            List<vw_Users_Role_Org> usrList = SearchUser(userSortOn, usr);
            List<UserRpt.UserRole> usrCountLst = new List<UserRpt.UserRole>(usrList.Count);
            //Get grouped data
            var grouped = from ul in usrList
                          group ul by new { ul.RoleID, ul.RoleName } into g
                          orderby g.Key.RoleName
                          select new { RoleID = g.Key.RoleID, Role = g.Key.RoleName, Count = g.Count() };
            //Populate UserCount list
            foreach (var u in grouped)
                usrCountLst.Add(new UserRpt.UserRole() { RoleID = u.RoleID, Role = u.Role, Count = u.Count });

            return usrCountLst;
        }

        public List<UserRpt.UserOrgType> GetOrgTypeUserCount(vw_Users_Role_Org usr)
        {
            List<vw_Users_Role_Org> usrList = SearchUser(userSortOn, usr);
            List<UserRpt.UserOrgType> usrCountLst = new List<UserRpt.UserOrgType>(usrList.Count);
            //Get grouped data
            var grouped = from ul in usrList
                          group ul by new { ul.OrgTypeId, ul.OrgType} into g
                          orderby g.Key.OrgType
                          select new { OrgTypeID = g.Key.OrgTypeId, OrgType = g.Key.OrgType, Count = g.Count() };
            //Populate UserCount list
            foreach (var u in grouped)
                usrCountLst.Add(new UserRpt.UserOrgType() { OrgTypeID = u.OrgTypeID.Value, OrgType = u.OrgType, Count = u.Count });

            return usrCountLst;
        }

        #endregion

        #region Claim Reports

        public List<ClaimRpt.StatuswiseClaim> GetStatusClaimCount(vw_Claim_Dashboard das)
        {
            List<vw_Claim_Dashboard> dasList = SearchClaim(claimSortOn, das);
            List<ClaimRpt.StatuswiseClaim> dasCountLst = new List<ClaimRpt.StatuswiseClaim>(dasList.Count);
            //Get grouped data
            var grouped = from ul in dasList
                          group ul by new { ul.StatusID, ul.Status } into g
                          orderby g.Key.Status
                          select new { StatusID = g.Key.StatusID, Status = g.Key.Status, Count = g.Count() };
            //Populate UserCount list
            foreach (var u in grouped)
                dasCountLst.Add(new ClaimRpt.StatuswiseClaim() { StatusID = u.StatusID, Status = u.Status, Count = u.Count });

            return dasCountLst;
        }

        public List<ClaimRpt.BrandwiseClaim> GetBrandClaimCount(vw_Claim_Dashboard das)
        {
            List<vw_Claim_Dashboard> dasList = SearchClaim(claimSortOn, das);
            List<ClaimRpt.BrandwiseClaim> dasCountLst = new List<ClaimRpt.BrandwiseClaim>(dasList.Count);
            //Get grouped data
            var grouped = from dl in dasList
                          group dl by new { dl.BrandID, dl.BrandName } into g
                          orderby g.Key.BrandName
                          select new { BrandID = g.Key.BrandID, Brand = g.Key.BrandName, Count = g.Count() };
            //Populate UserCount list
            foreach (var u in grouped)
                dasCountLst.Add(new ClaimRpt.BrandwiseClaim() { BrandID = u.BrandID, Brand = u.Brand, Count = u.Count });

            return dasCountLst;
        }

        public List<ClaimRpt.CustwiseClaim> GetCustClaimCount(vw_Claim_Dashboard das)
        {
            List<vw_Claim_Dashboard> dasList = SearchClaim(claimSortOn, das);
            List<ClaimRpt.CustwiseClaim> dasCountLst = new List<ClaimRpt.CustwiseClaim>(dasList.Count);
            //Get grouped data
            var grouped = from dl in dasList
                          group dl by new { dl.CustID, dl.CustOrg } into g
                          orderby g.Key.CustOrg
                          select new { CustID = g.Key.CustID, Cust = g.Key.CustOrg, Count = g.Count() };
            //Populate UserCount list
            foreach (var u in grouped)
                dasCountLst.Add(new ClaimRpt.CustwiseClaim() { CustID = u.CustID, Cust = u.Cust, Count = u.Count });

            return dasCountLst;
        }

        public List<ClaimRpt.YearlyClaim> GetYearlyClaimCount()
        {
            List<ClaimRpt.YearlyClaim> claimCountLst = new List<ClaimRpt.YearlyClaim>();
            //Get grouped data
            var grouped = from yc in dbc.vw_Yr_Claims_Items
                          group yc by new { yc.Yr } into g
                          orderby g.Key.Yr
                          select new { Year = g.Key.Yr, Count = g.Count() };
            //Populate UserCount list
            foreach (var d in grouped)
                claimCountLst.Add(new ClaimRpt.YearlyClaim() { Yr = d.Year.Value, Count = d.Count });

            return claimCountLst;
        }

        public List<ClaimRpt.YearClaimItem> GetYearClaimItems(int Year)
        {
            List<ClaimRpt.YearClaimItem> claimCountLst = new List<ClaimRpt.YearClaimItem>();
            //Get grouped data
            var grouped = from yc in dbc.vw_Yr_Claims_Items
                          where yc.Yr == Year
                          group yc by new { yc.Yr, yc.ClaimId, yc.ClaimNo } into g
                          orderby g.Sum(i => i.Items ?? 0) descending, g.Key.ClaimNo
                          select new { ClaimId = g.Key.ClaimId, ClaimNo = g.Key.ClaimNo, Count = g.Sum(i => i.Items??0) };
            //Populate UserCount list
            foreach (var d in grouped)
                claimCountLst.Add(new ClaimRpt.YearClaimItem() { ClaimId = d.ClaimId, ClaimNo = d.ClaimNo, Count = d.Count });

            return claimCountLst;
        }

        #endregion
    }
}
