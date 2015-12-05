using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic;
using System.Data.Linq.SqlClient;
using CPM.DAL;
using CPM.Helper;
using CPM.Models;

namespace CPM.Services
{
    public class DefaultDBService : _ServiceBase
    {
        public static DefaultClaim GetClaim(int userID)
        {
            return new DefaultClaim()
            {
                CustID = _Session.NewCustOrgId,
                AssignTo = Config.DefaultClaimAssigneeId,
                SalespersonID = 0,
                ShipToLocID = 0,
                StatusID = Config.DefaultClaimStatusId,
                BrandID = 0
            };
        }

        public static RoleRights GetNewRoleRight(int userID, string usrName, int sortOrdr)
        {
            return new RoleRights()
                {
                    _Added = false,
                    ID = -1,
                    Title = "[Role-1]",//Required otherwise it'll be considered as ModelState error !
                    TitleOLD = "[Role-1]",
                    LastModifiedBy = userID,
                    LastModifiedByVal = usrName,
                    LastModifiedDate = DateTime.Now,
                    RoleData = new MasterRole() { OrgTypeId = (int)OrgService.OrgType.Customer },
                    CanDelete = true,
                    SortOrder = sortOrdr
                };
        }
    }
}
