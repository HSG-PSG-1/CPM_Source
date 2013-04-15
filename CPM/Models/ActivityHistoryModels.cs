using System;
using System.ComponentModel.DataAnnotations;
using CPM.Helper;

namespace CPM.DAL
{
    [Serializable]
    public partial class vw_ActivityLog
    {
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}",ApplyFormatInEditMode = true)]
        //[DateRange(Defaults.minSQLDate.ToShortDateString(), Defaults.maxSQLDate.ToShortDateString())] - NOT needed because 
        //we use 'getValidDate' in functions interacting with DB
        public DateTime? ActDateTo { get; set; }
        
        public DateTime? ActDateTo_SQL
        {// Check and return a valid SQL date
            get
            {
                if (ActDateTo.HasValue) ActDateTo = Defaults.getValidDate(ActDateTo.Value);
                return ActDateTo;
            }
        }
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
        public DateTime? ActDateFrom { get; set; }
        
        public DateTime? ActDateFrom_SQL
        {// Check and return a valid SQL date
            get
            {
                if (ActDateFrom.HasValue) ActDateFrom = Defaults.getValidDate(ActDateFrom.Value);
                return ActDateFrom;
            }
        }
    }
}

