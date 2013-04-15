using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic;
using System.Data.Linq.SqlClient;
using CPM.DAL;
using CPM.Helper;
using Webdiyer.WebControls.Mvc;

namespace CPM.Services
{
    public class OrgService : _ServiceBase
    {
        #region Variables

        public const string sortOn = "Name";

        public enum OrgType
        {//HT:CAUTION: Make sure this is NOT changes because its mapped to a VIEW
            Customer = 1,
            Internal,
            Vendor
        }

        #endregion

        #region Search / Fetch

        public IQueryable GetOrgs(object OrgTyp, string term)
        {
            //using (dbc) HT: DON'T coz dbc will be accessed from VIEW
            OrgType enumObj = _Enums.ParseEnum<OrgType>(OrgTyp);

            term = (term ?? "%").ToLower();

            switch (enumObj)
            {
                case OrgType.Customer:
                    return from o in dbc.MasterOrgs
                           where (o.OrgTypeId == (int)OrgType.Customer && 
                                  o.Name.ToLower().Contains(term))
                                orderby o.Name
                   //HT: Kept for future
                   //select new { id = o.ID.ToString(), value = o.Code + "(" + o.Name + ")", label = o.Code + "(" + o.Name + ")" }; 
                           select new { id = o.ID, value = o.Name };                     
                case OrgType.Internal:
                    return from o in dbc.MasterOrgs
                           where (o.OrgTypeId == (int)OrgType.Internal && 
                                  o.Name.ToLower().Contains(term))
                                       orderby o.Name
                           select new { id = o.ID, value = o.Name };                     
                case OrgType.Vendor:
                    return from o in dbc.MasterOrgs
                           where (o.OrgTypeId == (int)OrgType.Vendor && 
                                  o.Name.ToLower().Contains(term))
                            orderby o.Name
                           select new { id = o.ID, value = o.Name };                     
            }               

            return null;

        }

        public IQueryable GetOrgsByRoleId(int RoleId, string term)
        {
            return from o in dbc.vw_MasterOrg_Roles
                   where (o.RoleId == RoleId && o.Name.ToLower().Contains(term))
                   orderby o.Name
                   select new { id = o.ID, value = o.Name, OrgTypeId = o.OrgTypeId };
        }

        #endregion
    }
}
