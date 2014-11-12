using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic;
using System.Data.Linq.SqlClient;
using CPM.Models;
using CPM.DAL;
using CPM.Helper;
using Webdiyer.WebControls.Mvc;

namespace CPM.Services
{
    public class ClaimService : _ServiceBase
    {
        #region Variables & Constructor
        
        public readonly vw_Claim_Master_User_Loc emptyView = new vw_Claim_Master_User_Loc() {
            ID = Defaults.Integer, ClaimDate = DateTime.Now, CustID = _SessionUsr.OrgID, CustOrg = _SessionUsr.OrgName };
        public readonly Claim emptyClaim = new Claim() { ID = Defaults.Integer, 
            aComments= new List<Comment>(), aFiles=new List<FileHeader>(), aItems  = new List<ClaimDetail>() }; // Add empty files, comments and items to ensure not null handling

        public ClaimService() : base() {;}
        public ClaimService(CPMmodel dbcExisting) : base(dbcExisting) {;}
        
        #endregion

        #region Search / Fetch

        public int GetAssignedToByClaim(int claimId)
        {// Get User to whom the claim(claimId) is Assigned
            using (dbc)
            {
                return dbc.Claims.SingleOrDefault(c => c.ID == claimId).AssignedTo;
                //(from c in dbc.Claims where c.ID == claimId select c.AssignedTo).SingleOrDefault();
            }
        }
                
        public vw_Claim_Master_User_Loc GetClaimById(int id)
        {
            using (dbc)
            {
                vw_Claim_Master_User_Loc vw_c = (from vw in dbc.vw_Claim_Master_User_Locs where vw.ID == id
                                                 select vw).SingleOrDefault<vw_Claim_Master_User_Loc>();                
                
                if (vw_c != null)
                    vw_c.StatusIDold = vw_c.StatusID;
                else
                    vw_c = emptyView;

                return vw_c;
            }
        }

        public vw_Claim_Master_User_Loc GetClaimByIdForPrint(int claimId,ref List<Comment> comments,
            ref List<FileHeader> filesH,ref List<ClaimDetail> items, bool loadComments)
        {
            using (dbc)
            {
                vw_Claim_Master_User_Loc vw_c = (from vw in dbc.vw_Claim_Master_User_Locs
                                                 where vw.ID == claimId
                                                 select vw).SingleOrDefault<vw_Claim_Master_User_Loc>();

                if (vw_c != null)
                    vw_c.StatusIDold = vw_c.StatusID;
                else
                    vw_c = emptyView;

                // Load comments
                if (loadComments) comments = new CommentService().Search(claimId, null);//Only for non-customers
                // Load Files
                filesH = new FileHeaderService().Search(claimId,null);
                items = new ClaimDetailService().Search(claimId, null);

                return vw_c;
            }
        }
                
        public static Claim GetClaimObjFromVW(vw_Claim_Master_User_Loc vw)
        {
            vw.ClaimGUID = vw.ClaimGUID??System.Guid.NewGuid().ToString();// MAke sure the GUID is set at this initial level
            return new Claim()
            {ID = vw.ID,
                AssignedTo = vw.AssignedTo, //HT? : CAUTION: Handle default Assignee !!!
                AssignToVal = vw.AssignedToVal,
                BrandID = vw.BrandID,
                ClaimDate = vw.ClaimDate,
                ClaimNo = vw.ClaimNo, //HT: Needed for Comment-AssignTo email (Auto-Generated)
                CustID = vw.CustID,
                CustRefNo = vw.CustRefNo,
                SalespersonID = vw.SalespersonID,
                ShipToLocationID = vw.ShipToLocationID,
                StatusID = vw.StatusID,
                //VendorID = vw.VendorID,
                Archived = vw.Archived, // HT: Make sure this is set!
                ClaimGUID = vw.ClaimGUID
            };
        }

        public static vw_Claim_Master_User_Loc GetVWFromClaimObj(Claim c, string SPNameForCustomer)
        {
            return new vw_Claim_Master_User_Loc()
            {
                ID = c.ID,
                AssignedTo = c.AssignedTo, //HT? : CAUTION: Handle default Assignee !!!
                //AssignedToVal = c.AssignedToVal,
                BrandID = c.BrandID,
                ClaimDate = c.ClaimDate,
                ClaimNo = c.ClaimNo, //HT: Needed for Comment-AssignTo email (Auto-Generated)
                CustID = c.CustID,
                CustRefNo = c.CustRefNo,
                SalespersonID = c.SalespersonID,
                SalespersonName = SPNameForCustomer, // HT: Need to default for customer
                ShipToLocationID = c.ShipToLocationID,
                StatusID = c.StatusID,
                //VendorID = c.VendorID,
                Archived = c.Archived, // HT: Make sure this is set!
                ClaimGUID = c.ClaimGUID // HT: Make sure this is set!
            };
        }

        #endregion

        #region Add / Edit / Delete / Archive / Add Default

        public int Add(Claim claimObj)
        {
            //Claim claimObj = GetClaimObjFromVW(vwObj);
            //triple ensure that the latest comment.PostedOn date is NOT null
            claimObj.ClaimDate = DateTime.Now;
            //Set lastmodified fields
            claimObj.LastModifiedBy = _SessionUsr.ID;
            claimObj.LastModifiedDate = DateTime.Now;

            claimObj.StatusHistories.Add(new StatusHistory()
            {
                Claim = claimObj, //ClaimID = claimObj.ID,
                LastModifiedBy = _SessionUsr.ID,
                LastModifiedDate = DateTime.Now,
                OldStatusID = Defaults.Integer,
                NewStatusID = claimObj.StatusID
            });

            dbc.Claims.InsertOnSubmit(claimObj);
            dbc.SubmitChanges();
            claimObj.ClaimNo = claimObj.ID;

            return claimObj.ID; // Return the 'newly inserted id'
        }

        public Claim AddDefault(int userID, int OrgID, bool defaultOrgSP,ref string spNameForCustomer)
        {
            DefaultClaim dDB = DefaultDBService.GetClaim(userID);
            Claim claimObj = new Claim()
            {
                AssignedTo = dDB.AssignTo,
                BrandID = dDB.BrandID,
                ClaimDate = dDB.ClaimDate,
                CustID = dDB.CustID,
                SalespersonID = dDB.SalespersonID,
                ShipToLocationID = dDB.ShipToLocID,
                StatusID = dDB.StatusID,
                ClaimGUID = System.Guid.NewGuid().ToString() // MAke sure the GUID is set at this initial level
            };

            claimObj.ID = Defaults.Integer;
            //Set lastmodified fields
            claimObj.LastModifiedBy = _SessionUsr.ID;
            claimObj.LastModifiedDate = DateTime.Now;

            #region Kept for future ref - also must be done in Add claim
            /* claimObj.StatusHistories.Add(new StatusHistory() { Claim = claimObj, //ClaimID = claimObj.ID,
                LastModifiedBy = _SessionUsr.ID, LastModifiedDate = DateTime.Now, 
                OldStatusID = Defaults.Integer, NewStatusID = claimObj.StatusID });

            dbc.Claims.InsertOnSubmit(claimObj);
            dbc.SubmitChanges(); */
            #endregion

            #region Special case for customer - pre populate SP
            if (defaultOrgSP)
            {
                try
                {
                    var spData = from i in dbc.vw_CustOrg_SalesUsers where i.ID == OrgID select i;
                    claimObj.SalespersonID = spData.ToList()[0].SalespersonId.Value;
                    spNameForCustomer = spData.ToList()[0].UserName;
                }
                catch(Exception ex){}// handles emply SP
                //OrgID  
            }
            #endregion

            return claimObj; // Return the 'newly configured claim'
        }
        
        public int AddEdit(Claim claimObj, int StatusIDold, bool doSubmit)
        {
            if (claimObj.ID <= Defaults.Integer) // Insert
                return Add(claimObj);

            // Update

            //Set lastmodified fields
            claimObj.LastModifiedBy = _SessionUsr.ID;
            claimObj.LastModifiedDate = DateTime.Now;

            dbc.Claims.Attach(claimObj);//attach the object as modified
            dbc.Refresh(System.Data.Linq.RefreshMode.KeepCurrentValues, claimObj);//Optimistic-concurrency (simplest solution)

            #region If the Status has been changed then make entry in StatusHistory
            if (claimObj.StatusID != StatusIDold)
                new StatusHistoryService(dbc).Add(new StatusHistory()
                {
                    ClaimID = claimObj.ID,
                    NewStatusID = claimObj.StatusID,
                    OldStatusID = StatusIDold                    
                }, false);
            #endregion

            if (doSubmit)    dbc.SubmitChanges();
            // Set Claim #
            claimObj.ClaimNo = claimObj.ID;

            return claimObj.ID;
        }

        public bool Archive(int claimID, bool archive)
        {
            Claim claimObj = dbc.Claims.Single(c => c.ID == claimID);
            //Set lastmodified fields
            claimObj.LastModifiedBy = _SessionUsr.ID;
            claimObj.LastModifiedDate = DateTime.Now;
            claimObj.Archived = archive;

            //dbc.Claims.Attach(claimObj);//attach the object as modified
            dbc.Refresh(System.Data.Linq.RefreshMode.KeepCurrentValues, claimObj);//Optimistic-concurrency
            dbc.SubmitChanges();

            return true;
        }
                
        public void Delete(Claim claimObj)
        {
            //HT: IMP: SP way of checking if an FK ref exists: http://stackoverflow.com/questions/5077423/sql-server-check-if-child-rows-exist
            dbc.Claims.DeleteOnSubmit(dbc.Claims.Single(c => c.ID == claimObj.ID));
            //Delete Claim Activities ???
            dbc.SubmitChanges();
        }

        #endregion

        #region Extra functions

        public bool AssignClaim(int claimId, int AssignTo)
        {
            if (claimId <= Defaults.Integer || AssignTo == Defaults.Integer)
                return false;

            else
            {
                #region Update
                Claim cObj = (from c in dbc.Claims where c.ID == claimId select c).SingleOrDefault<Claim>();

                if (cObj.ID <= Defaults.Integer) return false;

                cObj.AssignedTo = AssignTo;
                //Set lastmodified fields
                cObj.LastModifiedBy = _SessionUsr.ID;
                cObj.LastModifiedDate = DateTime.Now;

                //dbc.Claims.Attach(cObj);//attach the object as modified NOT needed as we just fetched it and dbc is ALIVE
                dbc.SubmitChanges();
                #endregion
            }

            return true;
        }

        internal bool IsClaimAccessible(int ClaimId, int UserId, int OrgId)
        {
            return (dbc.Claims.Where(c => c.CustID == OrgId && c.ID == ClaimId).Count() > 0);
        }

        #endregion
    
        public int AsyncBulkAddEditDelKO(vw_Claim_Master_User_Loc vwObj, int StatusIDold, 
            IEnumerable<ClaimDetail> items, IEnumerable<Comment> comments, IEnumerable<FileHeader> files)
        {
            Claim claimObj = ClaimService.GetClaimObjFromVW(vwObj);
            using (dbc)//Make sure this dbc is passed and persisted
            {
                bool isNewClaim = (claimObj.ID <= Defaults.Integer);
                bool doSubmit = true;
                string Progress = "";

                #region Set Transaction
                
                dbc.Connection.Open();
                //System.Data.Common.DbTransaction 
                var txn = dbc.Connection.BeginTransaction();
                dbc.Transaction = txn;
                //ExecuteReader requires the command to have a transaction when the connection assigned to the
                //command is in a pending local transaction. The Transaction property of the command has not been initialized.
                #endregion

                try
                {
                    Progress = 
                        "Claim (" + claimObj.ID + ", " + claimObj.ClaimGUID + ", " + claimObj.ClaimDate.ToString() + ")";
                    //Update claim
                    new ClaimService(dbc).AddEdit(claimObj, StatusIDold, true);//doSubmit must be TRUE
                    //IMP: Note: The above addedit will return updated ClaimObj which will have Claim Id

                    Progress = "Comments";//Process comments
                    if(comments != null && comments.Count() > 0)
                        new CommentService(dbc).BulkAddEditDel(comments.ToList(), claimObj.ID, doSubmit);
                    Progress = "HeaderFiles";//Process files (header) and files (make sure Header files are processed before detail)
                    if (files != null && files.Count() > 0)
                        new FileHeaderService(dbc).BulkAddEditDel(files.ToList(), claimObj, doSubmit, dbc, isNewClaim);
                    Progress = "Claimdetails";//Process items (and internally also process files(details)
                    if (items != null && items.Count() > 0)
                        new ClaimDetailService(dbc).BulkAddEditDel(items.ToList(), claimObj, doSubmit, isNewClaim, dbc);
                    
                    // No need to cleanup the GUID folder or similar because now we rename
                    /*NOTE: For Async the Details files will have to be handled internally in the above function
                    if (claimObj.ID.ToString() != claimObj.ClaimGUID &&
                        !string.IsNullOrEmpty(claimObj.ClaimGUID))//ensure there's NO confusion
                        FileIO.EmptyDirectory(System.IO.Path.Combine(Config.UploadPath, claimObj.ClaimGUID.ToString()));
                    */
                    if (!doSubmit) dbc.SubmitChanges();//Make a FINAL submit instead of periodic updates
                    txn.Commit();//Commit
                }
                #region  Rollback if error
                catch (Exception ex)
                {
                    txn.Rollback();
                    Exception exMore = new Exception(ex.Message + " After " + Progress);
                    // Make sure the temp files are also deleted
                    _Session.ResetClaimInSessionAndEmptyTempUpload(claimObj.ID, claimObj.ClaimGUID);
                    throw exMore;
                }
                finally
                {
                    if (dbc.Transaction != null)
                        dbc.Transaction.Dispose();
                    dbc.Transaction = null;
                }
                #endregion
            }

            #region Check and send email to the final Claim Assignee!

            if (Config.NofityAssignToEveryTime && claimObj.ID > Defaults.Integer && (claimObj.AssignedTo != _SessionUsr.ID))//Make sure "_Session.Claim" is available
            {
                string UserEmail = new UserService().GetUserEmailByID(claimObj.AssignedTo);
                MailManager.AssignToMail(claimObj.ClaimNo.ToString(), claimObj.AssignToComment, claimObj.ID, UserEmail, (_SessionUsr.UserName), false);
            }

            #endregion

            return claimObj.ID;//Return updated claimobj
        }
    }

    public class StatusHistoryService : _ServiceBase
    {
        #region Variables & Constructor
        public StatusHistoryService() : base() {;}
        public StatusHistoryService(CPMmodel dbcExisting) : base(dbcExisting) { ;}
        #endregion

        #region Search / Fetch
        public List<vw_StatusHistory_Usr> FetchAll(int ClaimID)
        {
            // Fetch all status history records for a Claim
            IQueryable<vw_StatusHistory_Usr> vw = from vw_s in dbc.vw_StatusHistory_Usrs where vw_s.ClaimID == ClaimID select vw_s;

            List<vw_StatusHistory_Usr> records = vw.ToList();
            
            if (records == null || records.Count == 0)
                return new List<vw_StatusHistory_Usr>();
            else
                return records;
        }
        #endregion

        #region Add / Edit / Delete
        public void Add(StatusHistory sObj, bool doSubmitChanges)
        {// Add status history record

            //Set last modified fields
            sObj.LastModifiedBy = _SessionUsr.ID;
            sObj.LastModifiedDate = DateTime.Now;

            dbc.StatusHistories.InsertOnSubmit(sObj);

            if(doSubmitChanges) dbc.SubmitChanges();
        }

        public bool UpdateClaimStatus(int ClaimID1, int OldStatusID, int NewStatusID)
        {
            //int AssignTo = Defaults.Integer;
            if (ClaimID1 <= Defaults.Integer || NewStatusID == Defaults.Integer || OldStatusID == NewStatusID)
                return false;

            else // Update
            {
                Claim cObj = (from c in dbc.Claims where c.ID == ClaimID1 select c).SingleOrDefault<Claim>();
                if (cObj.ID <= Defaults.Integer) return false;
                cObj.StatusID = NewStatusID;cObj.LastModifiedBy = _SessionUsr.ID;cObj.LastModifiedDate = DateTime.Now;

                Add(new StatusHistory()
                {
                    ClaimID = ClaimID1,
                    NewStatusID = NewStatusID,
                    OldStatusID = OldStatusID
                }, true);
            }
            
            return true;
        }
        #endregion
    }
}
