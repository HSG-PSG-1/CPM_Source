using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CPM.Services;
using CPM.Helper;

namespace CPM.DAL
{
    #region Base classes for Report

    public abstract class RptBase
    {
        public object SearchData { get; set; }
        public IEnumerable<object> ReportData { get; set; }
        public string Report { get; set; }
    }
    public abstract class RptModelBase {
        public string Txt { get; set; }
        public int Count { get; set; }
        public virtual string DspText { get{return Txt + " (" + Count.ToString() + ")";} }
    }

    #endregion

    public class ActivityRpt : RptBase
    {
        public class ActivityCount : RptModelBase
        {
            public int ActivityID { get; set; }
            public string Activity { get { return Txt; } set { Txt = value; } }
        }

        public class UserActivity : RptModelBase
        {
            public int UserID { get; set; }
            public string Email { get { return Txt; } set { Txt = value; } }            
        }

        public class MonthlyUserAct : RptModelBase
        {
            public DateTime YrMonth { get; set; }            
            public override string DspText { get { return YrMonth.ToString("MMM yy") + " (" + Count.ToString() + ")"; }}
        }
    }

    public class UserRpt : RptBase
    {
        public class UserRole : RptModelBase
        {            
            public int RoleID { get; set; }
            public string Role { get { return Txt; } set { Txt = value; } }
        }

        public class UserOrgType : RptModelBase
        {
            public int OrgTypeID { get; set; }
            public string OrgType { get { return Txt; } set { Txt = value; } }
        }
    }

    public class ClaimRpt : RptBase
    {
        public class StatuswiseClaim : RptModelBase
        {
            public int StatusID { get; set; }
            public string Status { get { return Txt; } set { Txt = value; } }
        }

        public class BrandwiseClaim : RptModelBase
        {
            public int BrandID { get; set; }
            public string Brand { get { return Txt; } set { Txt = value; } }
            
        }

        public class CustwiseClaim : RptModelBase
        {
            public int CustID { get; set; }
            public string Cust { get { return Txt; } set { Txt = value; } }
        }

        public class YearlyClaim : RptModelBase
        {
            public int Yr { get; set; }            
            public override string DspText { get { return Yr.ToString() + " (" + Count.ToString() + ")"; } }
        }
        public class YearClaimItem : RptModelBase
        {
            public int ClaimId { get; set; }
            public int ClaimNo { get; set; }
            public override string DspText { get { return ClaimNo.ToString() + " (" + Count.ToString() + ")"; } }
        }
        
    }
}