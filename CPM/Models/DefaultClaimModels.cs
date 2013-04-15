using System;
using System.Data.Linq.Mapping;

namespace CPM.Models
{
    [Table]
    //HT: Required to bind extra properties while Dynamic 
    //LINQ: http://geekswithblogs.net/michelotti/archive/2008/04/20/121437.aspx
    public partial class DefaultClaim
    {
        public int CustID { get; set; }
        public int BrandID { get; set; }
        public int SalespersonID { get; set; }
        public int AssignTo { get; set; }
        public int ShipToLocID { get; set; }
        public int StatusID { get; set; }        
        public DateTime ClaimDate { get { return DateTime.Now; } }        
    }
}
