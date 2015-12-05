using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic;
using System.Data.Linq.SqlClient;
using CPM.DAL;
using CPM.Models;
using CPM.Helper;
using Webdiyer.WebControls.Mvc;

namespace CPM.Services
{
    public class SecurityService : _ServiceBase
    {
        #region Variables

        public const string sortOn = "SortOrder";

        public enum Roles
        {//HT:CAUTION: Must match with that of the existing DB
            Internal = 1,
            Customer,
            Admin,
            Sales,
            Vendor
        }

        #endregion

        #region Search / Fetch

        public List<RoleRights> FetchAll()
        {// Fetch All for a particular Master table (this.masterType)
            using (dbc)
            {
                var rQuery = (from r in dbc.MasterRoles
                              //LEFT OUTER JOIN For UserEmail
                              join u in dbc.Users on new { LastModifiedBy = r.LastModifiedBy } equals
                              new { LastModifiedBy = u.ID } into u_join
                              from u in u_join.DefaultIfEmpty()
                              orderby r.SortOrder
                              select new { r, u.Name });

                //Create type 'RoleRights' based on MasterRoles
                List<RoleRights> roleRightData = new List<RoleRights>();
                int sortOrder = 0;
                foreach (var data in rQuery)
                {
                    #region Configure and add RoleRights
                    roleRightData.Add(new RoleRights()
                    {
                        ID = data.r.ID,
                        Title = data.r.Title,
                        TitleOLD = data.r.Title,//DON'T forget
                        SortOrder = data.r.SortOrder,                        
                        LastModifiedBy = data.r.LastModifiedBy,
                        LastModifiedDate = data.r.LastModifiedDate,
                        LastModifiedByVal = data.Name,

                        #region Configure RoleData
                        RoleData = new MasterRole()
                        {
                            ManageRole = data.r.ManageRole,
                            ManageSetting = data.r.ManageSetting,
                            ManageMaster = data.r.ManageMaster,
                            ManageUser = data.r.ManageUser,
                            DeleteClaim = data.r.DeleteClaim,
                            ViewActivity = data.r.ViewActivity,
                            ArchiveClaim = data.r.ArchiveClaim,
                            OrgTypeId = data.r.OrgTypeId
                        },
                        #endregion

                        CanDelete = data.r.CanDelete

                    });
                    #endregion

                    if (sortOrder < data.r.SortOrder) sortOrder = data.r.SortOrder;
                }

                //Add an empty record for Add new
                roleRightData.Add(DefaultDBService.GetNewRoleRight(Defaults.Integer, _SessionUsr.UserName, sortOrder));
                return roleRightData;
            }
        }

        public IQueryable GetRoles()
        {
            //using (dbc) HT: DON'T coz dbc will be accessed from VIEW
            {
                IQueryable RolesQ = from o in dbc.MasterRoles
                                    orderby o.SortOrder, o.Title
                                    select new { ID = o.ID, TEXT = o.Title };

                return RolesQ;
            }
        }

        public IEnumerable GetRolesCached()
        {
            object cachedData = _SessionLookup.UserRoles;
            if (cachedData == null || ((IEnumerable)cachedData).AsQueryable().Count() < 1)
            {
                IEnumerable data = GetRoles();
                _SessionLookup.UserRoles = data;
                return data;
            }
            else
                return ((IEnumerable)cachedData);
        }

        public MasterRole GetRoleObj(RoleRights rrObj)
        {
            return new MasterRole()
            {
                ID = rrObj.ID,
                Title = rrObj.Title,
                SortOrder = rrObj.SortOrder,
                LastModifiedBy = _SessionUsr.ID,
                LastModifiedDate = DateTime.Now,
                ManageRole = rrObj.RoleData.ManageRole,
                DeleteClaim = rrObj.RoleData.DeleteClaim,
                ManageUser = rrObj.RoleData.ManageUser,
                ManageMaster = rrObj.RoleData.ManageMaster,
                ViewActivity = rrObj.RoleData.ViewActivity,
                ManageSetting = rrObj.RoleData.ManageSetting,
                ArchiveClaim = rrObj.RoleData.ArchiveClaim,
                CanDelete = rrObj.CanDelete,
                OrgTypeId = rrObj.RoleData.OrgTypeId
            };
        }

        #endregion

        #region Add / Edit / Delete & Bulk

        public void Add(RoleRights rrObj)
        {
            rrObj.CanDelete = true;//double-make-sure that new record is not marked as undeletable
            MasterRole rObj = GetRoleObj(rrObj);
            dbc.MasterRoles.InsertOnSubmit(rObj);
            //dbc.SubmitChanges();
        }
                
        public void Update(RoleRights rrObj)
        {
            if (rrObj.ID <= Defaults.Integer) // Insert
                return;//HT:SPECIAL CASE: W've handled Add separately so we skip //Add(rrObj);

            else // Update
            {
                MasterRole rObj = GetRoleObj(rrObj);
                dbc.MasterRoles.Attach(rObj);
                dbc.Refresh(System.Data.Linq.RefreshMode.KeepCurrentValues, rObj);
                //Optimistic-concurrency (simplest solution)
                //dbc.SubmitChanges();
            }
        }                
                        
        public void Delete(RoleRights rObj)
        {
            dbc.MasterRoles.DeleteOnSubmit(dbc.MasterRoles.Single(r => r.CanDelete && r.ID == rObj.ID));
            //dbc.SubmitChanges();
        }
                
        public void BulkAddEditDel(List<RoleRights> items)
        {
            // Cleanup newly added & deleted records
            items.RemoveAll(i => i.ID == Defaults.Integer && i._Deleted);

            using (dbc)
            {
                dbc.Connection.Open();
                
                var txn = dbc.Connection.BeginTransaction();
                dbc.Transaction = txn;
                //Execution requires the command to have a transaction when the connection assigned to the
                //command is in a pending local transaction. The Transaction property of the command has not been initialized.

                try
                {
                    foreach (RoleRights item in items)
                    {
                        item.LastModifiedBy = _SessionUsr.ID;
                        item.LastModifiedDate = DateTime.Now;

                        if (item._Added && !item._Deleted) // Because we're NOT removing the deleted items
                            Add(item);
                        else if (item._Deleted && item.CanDelete)//double check the can-delete flag
                            Delete(item);//Make sure Ref check brfore Delete is done
                        else //if (item._Updated)//Make sure update is LAST
                            Update(item);
                    }
                    dbc.SubmitChanges();//Make a FINAL submit instead of periodic updates
                    txn.Commit();//Commit
                }
                #region  Rollback if error
                catch (Exception ex)
                {
                    txn.Rollback();
                    throw ex;
                }
                finally
                {
                    if (dbc.Transaction != null)
                        dbc.Transaction.Dispose();
                    dbc.Transaction = null;
                }
                #endregion
            }
        }

        #endregion

        #region Extra functions

        public bool IsTitleDuplicate(string title)
        {
            return (dbc.MasterRoles.Where(m => m.Title.ToUpper() == title.ToUpper()).Count() > 0);
        }
        
        public override bool IsReferred(Object oObj)
        {
            RoleRights rrObj = (RoleRights)oObj;            
            return !rrObj.CanDelete || (dbc.Users.Where(u => u.RoleID == rrObj.ID).Count() > 0);
        }

        #endregion

    }
}