using System;
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
    public class MasterService : _ServiceBase
    {
        #region Variables & Constructor
        public const string sortOn = "SortOrder";
        private const string selectSQL =
            "SELECT m.*, u.[Name] FROM Master{0} as m LEFT OUTER JOIN Users as u ON m.LastModifiedBy = u.ID ORDER BY {1} sortOrder"; 
        // {1} is for special sorting required by MasterDefect
        private const string deleteSQL = "DELETE FROM Master{0} WHERE ID = {1}";
        
        //[TypeConverter(typeof(EnumToStringUsingDescription))]
        //Pending R&D for better Enum toString: http://stackoverflow.com/questions/796607/how-do-i-override-tostring-in-c-enums
        [Serializable]
        public enum Table : int
        {
            //Activity,
            Claim_Status,
            File_Type_Detail,
            File_Type_Header,
            Defect
        }

        Table masterType;

        public MasterService(Table? _masterType){
            if (_masterType != null) masterType = _masterType.Value;
        }

        #endregion

        #region Search / Fetch

        public List<Master> FetchAll()
        {
            // Fetch All for a particular Master table (this.masterType)
            //http://marlongrech.wordpress.com/2008/03/01/how-to-get-data-dynamically-from-you-linq-to-sql-data-context/

            string masterDefectSort = "";
            using (dbc)
            {
                IEnumerable<Master> mQuery = dbc.ExecuteQuery<Master>(
                    string.Format(selectSQL, masterType.ToString().Replace("_", ""), masterDefectSort));
                //Be careful with the table name !!!

                #region Not needed but kept for column ref
                /*IEnumerable<Master> result = (from m in mQuery
                                              select
                                                 new Master()
                                                 {
                                                     ID = m.ID,
                                                     Title = m.Title,
                                                     TitleOLD = m.Title,
                                                     SortOrder = m.SortOrder,
                                                     LastModifiedBy = m.LastModifiedBy,
                                                     LastModifiedByVal = m.LastModifiedByVal,
                                                     LastModifiedDate = m.LastModifiedDate
                                                 });*/
                #endregion

                List<Master> result = mQuery.ToList<Master>();
                //TitleOLD will be populated because of [Column(Name = "Title")]

                #region Add an empty record for Add new
                result.Add(new Master()
                {
                    _Added = false,
                    ID = -1,
                    Title = "[Title-1]",//Required otherwise it'll be considered as ModelState error !
                    SortOrder = result.Count+1,//To make sure the js sort doesn't mess up like 0
                    LastModifiedBy = Defaults.Integer,//Make sure this is set to 0
                    LastModifiedByVal = _SessionUsr.UserName,
                    LastModifiedDate = DateTime.Now,
                    CanDelete = true
                });
                #endregion

                return result;
                #region L2S for JOIN (kept for future ref)
                /*
                 from r in dbc.MasterRole
join u in dbc.Users on new { LastModifiedBy = r.LastModifiedBy } equals new { LastModifiedBy = u.ID } into u_join
from u in u_join.DefaultIfEmpty()
select new {
  r.ID,
  r.Title,
  r.SortOrder,
  r.LastModifiedBy,
  r.LastModifiedDate,
  FirstName = u.FirstName
}

                */
                #endregion
            }
        }

        public List<Master> FetchAllCached()
        {
            object cachedData = _SessionLookup.MasterData[masterType];
            if (cachedData == null || ((List<Master>)cachedData).Count < 1)
            { 
                List<Master> data = FetchAll();
                _SessionLookup.MasterData[masterType] = data;
                return data;
            }
            else
                return ((List<Master>)cachedData);
        }

        #endregion

        #region Add / Edit / Delete & Bulk

        public void Add(Master mObj)
        {
            mObj.CanDelete = true;//double-make-sure that new record is not marked as undeletable
            object updateObj = GetTableObj(mObj);

            #region Insert On Submit
            switch (masterType)
            {
                //case Table.Role:dbc.MasterRoles.InsertOnSubmit((MasterRole)updateObj); break;
                //new MasterRole(){ID=mObj.ID, Title=mObj.Title, SortOrder=mObj.SortOrder, 
                //LastModifiedBy=mObj.LastModifiedBy, LastModifiedDate=mObj.LastModifiedDate});                        
                case Table.Claim_Status: dbc.MasterClaimStatus.InsertOnSubmit((MasterClaimStatus)updateObj); break;
                case Table.Defect: dbc.MasterDefects.InsertOnSubmit((MasterDefect)updateObj); break;
                case Table.File_Type_Detail: dbc.MasterFileTypeDetails.InsertOnSubmit((MasterFileTypeDetail)updateObj); break;
                case Table.File_Type_Header: dbc.MasterFileTypeHeaders.InsertOnSubmit((MasterFileTypeHeader)updateObj); break;
                //case Table.Activity: dbc.MasterActivities.InsertOnSubmit((MasterActivity)updateObj); break;
            }
            #endregion

            //dbc.SubmitChanges();
        }
                
        public void Update(Master mObj)
        {
            //Set lastmodified fields
            mObj.LastModifiedBy = _SessionUsr.ID;
            mObj.LastModifiedDate = DateTime.Now;

            if (mObj.ID <= Defaults.Integer) // Insert
                return;//HT:SPECIAL CASE: W've handled Add separately so we skip //AddMasterEntry(mObj);

            else // Update
            {
                object updateObj = GetTableObj(mObj);

                #region Attach the object as modified
                switch (masterType)
                {
                    //case Table.Role: dbc.MasterRoles.Attach((MasterRole)updateObj); break;
                    case Table.Claim_Status: dbc.MasterClaimStatus.Attach((MasterClaimStatus)updateObj); break;
                    case Table.Defect: dbc.MasterDefects.Attach((MasterDefect)updateObj); break;
                    case Table.File_Type_Detail: dbc.MasterFileTypeDetails.Attach((MasterFileTypeDetail)updateObj); break;
                    case Table.File_Type_Header: dbc.MasterFileTypeHeaders.Attach((MasterFileTypeHeader)updateObj); break;
                    //case Table.Activity: dbc.MasterActivities.Attach((MasterActivity)updateObj); break;
                    //dbc.MasterActivities.Attach(new MasterActivity(){ID=mObj.ID, Title=mObj.Title, SortOrder=mObj.SortOrder, 
                    //LastModifiedBy=mObj.LastModifiedBy, LastModifiedDate=mObj.LastModifiedDate});
                }
                #endregion

                dbc.Refresh(System.Data.Linq.RefreshMode.KeepCurrentValues, updateObj);//Optimistic-concurrency (simplest solution)
                
                //dbc.SubmitChanges();
            }
        }
                
        public void Delete(Master mObj)
        {
            //HT: CAUTION: Make sure references are checked
            #region Attach the object as modified
            switch (masterType)
            {
                case Table.Claim_Status: dbc.MasterClaimStatus.DeleteOnSubmit(dbc.MasterClaimStatus.Single
                    (c =>c.CanDelete && c.ID == mObj.ID)); break;
                case Table.Defect: dbc.MasterDefects.DeleteOnSubmit(dbc.MasterDefects.Single
                    (c => c.CanDelete && c.ID == mObj.ID)); break;
                case Table.File_Type_Detail: dbc.MasterFileTypeDetails.DeleteOnSubmit(dbc.MasterFileTypeDetails.Single
                    (c => c.CanDelete && c.ID == mObj.ID)); break;
                case Table.File_Type_Header: dbc.MasterFileTypeHeaders.DeleteOnSubmit(dbc.MasterFileTypeHeaders.Single
                    (c => c.CanDelete && c.ID == mObj.ID)); break;                
                //case Table.Activity: dbc.MasterActivities.DeleteOnSubmit(dbc.MasterActivities.Single
                //    (c =>c.CanDelete && c.ID == mObj.ID)); break;
            }
            #endregion

            //dbc.SubmitChanges();
        }

        public void BulkAddEditDel(List<Master> items)
        {
            // Cleanup newly added & deleted records
            items.RemoveAll(i => (i._Added || i.ID < Defaults.Integer) && i._Deleted); // Remove newly inserted records
            using (dbc)
            {
                dbc.Connection.Open();
                
                var txn = dbc.Connection.BeginTransaction();
                dbc.Transaction = txn;
                //Execution requires the command to have a transaction when the connection assigned to the
                //command is in a pending local transaction. The Transaction property of the command has not been initialized.

                try
                {
                    foreach (Master item in items)
                    {
                        #region Perform Db operations
                        item.LastModifiedBy = _SessionUsr.ID;
                        item.LastModifiedDate = DateTime.Now;

                        if (item._Added && !item._Deleted) // Because we're NOT removing the deleted items
                            Add(item);
                        else if (item._Deleted && item.CanDelete)//double check the can-delete flag
                            Delete(item);//Make sure Ref check brfore Delete is done
                        else if (item._Updated)//Make sure update is LAST
                            Update(item);
                        #endregion
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
                    //Invalidate cache entry
                    _SessionLookup.MasterData.Remove(masterType);
                }
                #endregion
            }
        }

        #endregion

        #region Extra functions

        /// <summary>
        /// Returns specific Master object wrapped as object as per - masterType
        /// </summary>
        /// <param name="mObj">Master object</param>
        /// <returns>Specific Master object wrapped as object</returns>
        public object GetTableObj(Master mObj)
        {
            switch (masterType)
            {
                case Table.Claim_Status:
                    return new MasterClaimStatus()
                    {
                        ID = mObj.ID,
                        Title = mObj.Title,
                        SortOrder = mObj.SortOrder,
                        LastModifiedBy = mObj.LastModifiedBy,
                        LastModifiedDate = mObj.LastModifiedDate,
                        CanDelete = mObj.CanDelete
                    };
                case Table.Defect:
                    return new MasterDefect()
                    {
                        ID = mObj.ID,
                        Title = mObj.Title,
                        //HT:Special field
                        //Category = mObj.Category,
                        SortOrder = mObj.SortOrder,
                        LastModifiedBy = mObj.LastModifiedBy,
                        LastModifiedDate = mObj.LastModifiedDate,
                        CanDelete = mObj.CanDelete
                    };
                case Table.File_Type_Detail:
                    return new MasterFileTypeDetail()
                    {
                        ID = mObj.ID,
                        Title = mObj.Title,
                        SortOrder = mObj.SortOrder,
                        LastModifiedBy = mObj.LastModifiedBy,
                        LastModifiedDate = mObj.LastModifiedDate,
                        CanDelete = mObj.CanDelete
                    };
                case Table.File_Type_Header:
                    return new MasterFileTypeHeader()
                    {
                        ID = mObj.ID,
                        Title = mObj.Title,
                        SortOrder = mObj.SortOrder,
                        LastModifiedBy = mObj.LastModifiedBy,
                        LastModifiedDate = mObj.LastModifiedDate,
                        CanDelete = mObj.CanDelete
                    };
                #region Role & Activity - kept for future
                /*
                 case Table.Role:
                    return new MasterRole()
                    {
                        ID = mObj.ID,
                        Title = mObj.Title,
                        SortOrder = mObj.SortOrder,
                        LastModifiedBy = mObj.LastModifiedBy,
                        LastModifiedDate = mObj.LastModifiedDate,
                        CanDelete = mObj.CanDelete
                    };                 
                  case Table.Activity:
                    return new MasterActivity()
                    {
                        ID = mObj.ID,
                        Title = mObj.Title,
                        SortOrder = mObj.SortOrder,
                        LastModifiedBy = mObj.LastModifiedBy,
                        LastModifiedDate = mObj.LastModifiedDate,
                        CanDelete = mObj.CanDelete
                    };*/
                #endregion
                default: return null;
            }
        }

        public static string formatTitle(string title)
        {
            return title.Replace("_", " ");
        }

        public bool IsTitleDuplicate(string title)
        {// Check if Title is Duplicate (NOTE - here we still don't know which master object has been invoked)
            
            //HT: Special case for Role
            if (!_Session.MasterTbl.HasValue)   return new SecurityService().IsTitleDuplicate(title);

            switch (_Session.MasterTbl)//masterType - can't use it because its not available while validation
            {
                //case Table.Role: return (dbc.MasterRoles.Where(m => m.Title.ToUpper() == title.ToUpper()).Count() > 0);
                case Table.Claim_Status: return (dbc.MasterClaimStatus.Where(m => m.Title.ToUpper() == title.ToUpper()).Count() > 0);
                case Table.Defect: return (dbc.MasterDefects.Where(m => m.Title.ToUpper() == title.ToUpper()).Count() > 0);
                case Table.File_Type_Detail: return (dbc.MasterFileTypeDetails.Where(m => m.Title.ToUpper() == title.ToUpper()).Count() > 0);
                case Table.File_Type_Header: return (dbc.MasterFileTypeHeaders.Where(m => m.Title.ToUpper() == title.ToUpper()).Count() > 0);
                //case Table.Activity: return (dbc.MasterActivities.Where(m => m.Title.ToUpper() == title.ToUpper()).Count() > 0);

                default: return false;
            }
        }

        public override bool IsReferred(Object oObj)
        {//This can be expensive so make sure it is done only once (i.e. at present it is handled in controller)
            Master mObj = (Master)oObj;

            if (!mObj.CanDelete) return true;//If the 

            bool referred = false;
            switch (masterType)
            {
                //case Table.Role: dbc.MasterRoles.DeleteOnSubmit(dbc.MasterRoles.Single(c => c.ID == mObj.ID)); break;
                case Table.Claim_Status:
                    #region Check Status ref in Claim

                    referred = (dbc.Claims.Where(m => m.StatusID == mObj.ID).Count() > 0);
                    if (!referred)//Check Status ref in StatusHistory
                        referred = (dbc.StatusHistories.Where(m => m.NewStatusID == mObj.ID || m.OldStatusID == mObj.ID).Count() > 0);
                    return referred;

                    #endregion

                case Table.Defect: //Check Defect ref in ClaimDetail
                    return (dbc.ClaimDetails.Where(m => m.NatureOfDefect == mObj.ID).Count() > 0);
                case Table.File_Type_Detail: //Check File_Type_Detail ref in FileDetail.FileType
                    return (dbc.FileDetails.Where(m => m.FileType == mObj.ID).Count() > 0);
                case Table.File_Type_Header: //Check File_Type_Header ref in FileHeader.FileType
                    return (dbc.FileHeaders.Where(m => m.FileType == mObj.ID).Count() > 0);
                
                //For future
                //case Table.Activity://Check Activity ref in ActivityHistory
                //    return (dbc.ActivityHistories.Where(m => m.ActivityID == mObj.ID).Count() > 0);
            }
            return false;
        }        

        #endregion
    }
}
