using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Reflection;
using CPM.DAL;
using CPM.Services;
using CPM.Helper;
using System.Text.RegularExpressions;

namespace CPM.DAL
{
    [Serializable]
    public partial class vw_Claim_Dashboard
    {
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
        //[DateRange(Defaults.minSQLDate.ToShortDateString(), Defaults.maxSQLDate.ToShortDateString())] - NOT needed because 
        //we use 'getValidDate' in functions interacting with DB
        public DateTime? ClaimDateTo { get; set; }
        
        public DateTime? ClaimDateTo_SQL
        {// Check and return a valid SQL date
            get
            {
                if (ClaimDateTo.HasValue) ClaimDateTo = Defaults.getValidDate(ClaimDateTo.Value);
                return ClaimDateTo;
            }
        }

        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
        public DateTime? ClaimDateFrom { get; set; }

        public DateTime? ClaimDateFrom_SQL
        {// Check and return a valid SQL date
            get
            {
                if (ClaimDateFrom.HasValue) ClaimDateFrom = Defaults.getValidDate(ClaimDateFrom.Value);
                return ClaimDateFrom;
            }
        }

        public int? ClaimNo1 { get; set; }
        public string ClaimNos { get; set; }

        public string ClaimDateTxt { get { return ClaimDate.ToString(Defaults.dtFormat, Defaults.ci); } }
        [Bindable(BindableSupport.No)]
        public string ShipToLocAndCode { get { return Common.getLocationAndCode(this.ShipToLoc, this.ShipToCode); } }
    }
}

