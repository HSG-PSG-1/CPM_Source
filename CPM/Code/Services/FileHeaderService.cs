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
    public class FileHeaderService : _ServiceBase
    {
        #region Variables & Constructor
        
        public readonly FileHeader newObj = new FileHeader() { ID = Defaults.Integer };

        public FileHeaderService() : base() {;}
        public FileHeaderService(CPMmodel dbcExisting) : base(dbcExisting) { ;}
        
        #endregion

        #region Search / Fetch

        public List<FileHeader> Search(int claimID, int? userID)
        {
            //using (dbc)//HT: DON'T coz we're sending IQueryable
            IQueryable<FileHeader> cQuery = from f in dbc.FileHeaders                                            
                                            
                                            #region LEFT OUTER JOINs
                                                
                                                //LEFT OUTER JOIN For User
                                            join u in dbc.Users on new { UserID = f.UserID } equals
                                            new { UserID = u.ID } into u_join
                                            from u in u_join.DefaultIfEmpty()

                                            //LEFT OUTER JOIN For User
                                            join t in dbc.MasterFileTypeHeaders on
                                         new { TypID = f.FileType } equals new { TypID = t.ID } into t_join
                                            from t in t_join.DefaultIfEmpty()
                                                
                                                #endregion

                                            where f.ClaimID == claimID
                                            orderby f.UploadedOn descending
                                            select Transform(f, u.Name, t.Title, f.ClaimID);

            return cQuery.ToList<FileHeader>();
        }

        FileHeader Transform(FileHeader f, string fileHeaderBy, string fileTypeTitle, int claimID)
        {
            /*also set .Claim = null to avoid issues during serialization but persist ClaimID as ClaimID1 */
            return f.Set(f1 =>
            {
                f1.UploadedBy = fileHeaderBy; f1.FileTypeTitle = fileTypeTitle; f1.ClaimGUID = f1.ClaimID.ToString(); 
                /*f1.Claim = null; f1.ClaimID1 = claimID;
                 NOT needed because we've set the Association Access to Internal in the dbml*/});
        }
                
        public FileHeader GetFileHeaderById(int id)
        {
            using (dbc)
            {
                FileHeader cmt = (from f in dbc.FileHeaders where f.ID == id select f).SingleOrDefault<FileHeader>();
                //cmt.Claim = new Claim();//HT: So that it doesn't complain NULL later
                return cmt;
            }
        }

        #endregion
                
        #region Add / Edit / Delete & Bulk

        public int Add(FileHeader fileHeaderObj, bool doSubmit)
        {
            //Set lastmodified fields
            fileHeaderObj.LastModifiedBy = _SessionUsr.ID;
            fileHeaderObj.LastModifiedDate = DateTime.Now;
            
            dbc.FileHeaders.InsertOnSubmit(fileHeaderObj);
            if (doSubmit) dbc.SubmitChanges();

            return fileHeaderObj.ID; // Return the 'newly inserted id'
        }
                
        public int AddEdit(FileHeader fileHeaderObj, bool doSubmit)
        {
            fileHeaderObj.UploadedOn = Defaults.getValidDate(fileHeaderObj.UploadedOn); // special case to ensure valid SQLDate
            if (fileHeaderObj.ID <= Defaults.Integer) // Insert
                return Add(fileHeaderObj, doSubmit);

            else
            {
                #region Update
                //Set lastmodified fields
                fileHeaderObj.LastModifiedBy = _SessionUsr.ID;                
                fileHeaderObj.LastModifiedDate = DateTime.Now;
                
                dbc.FileHeaders.Attach(fileHeaderObj);//attach the object as modified
                dbc.Refresh(System.Data.Linq.RefreshMode.KeepCurrentValues, fileHeaderObj);//Optimistic-concurrency (simplest solution)
                #endregion

                if (doSubmit) //Make a FINAL submit instead of periodic updates
                   dbc.SubmitChanges();
            }

            return fileHeaderObj.ID;
        }
                
        public void Delete(FileHeader fileHeaderObj, bool doSubmit)
        {
            dbc.FileHeaders.DeleteOnSubmit(dbc.FileHeaders.Single(f => f.ID == fileHeaderObj.ID));
            if (doSubmit) dbc.SubmitChanges();
        }

        public void BulkAddEditDel(List<FileHeader> records, Claim claimObj, bool doSubmit, CPMmodel dbcContext)
        {
            //OLD: if (claimID <= Defaults.Integer) return; //Can't move forward if its a new Claim entry
            #region NOTE
            /* Perform Bulk Add, Edit & Del based on Object properties set in VIEW
             * MEANT ONLY FOR ASYNC BULK OPERATIONS
             * Handle transaction, error and final commit in Caller */
            #endregion

            //using{dbc}, try-catch and transaction must be handled in callee function
            foreach (FileHeader item in records)
            {
                #region Perform Db operations
                item.ClaimID = claimObj.ID;
                item.LastModifiedBy = _SessionUsr.ID;
                item.LastModifiedDate = DateTime.Now;
                item.UploadedOn = DateTime.Now; // double ensure dates are not null !

                //Special case handling for IE with KO - null becomes "null"
                if (item.Comment == "null") item.Comment = "";

                if (item._Deleted)
                    Delete(item, false);
                else if (item._Edited)//Make sure Delete is LAST
                    AddEdit(item, false);
                else if (item._Added)
                    Add(item, false);
                #endregion

                #region Log Activity (finally when the uploaded file data is entered in the DB
                if (item._Added || item._Edited)
                {
                    //Special case: Call the econd overload which has "doSubmit" parameter
                    new ActivityLogService(ActivityLogService.Activity.ClaimFileUpload, dbcContext).Add(
                    new ActivityHistory() { FileName = item.FileName, ClaimID = claimObj.ID, ClaimText = claimObj.ClaimNo.ToString() },
                    doSubmit);
                }
                #endregion
            }
            if (doSubmit) dbc.SubmitChanges();//Make a FINAL submit instead of periodic updates
            //Move header files
            ProcessFiles(records, claimObj.ID, claimObj.ClaimGUID);
        }

        #endregion

        #region Extra functions

        void ProcessFiles(List<FileHeader> records, int claimID, string ClaimGUID)
        {
            if (records == null || records.Count < 1) return;

            foreach (FileHeader item in records)
                if (item._Deleted)//Delete will always be for existing not Async (so use ClaimID)
                    FileIO.DeleteClaimFile(item.FileName, item.ClaimID, null, FileIO.mode.header);

            if(records.Count > 0) //finally copy all the files from H_Temp to H
            FileIO.MoveAsyncClaimFiles(claimID, ClaimGUID, null, null, true);
        }

        #endregion
    }

    public class CAWFile : CAWBase
    {
        #region Variables & Constructor
        public CAWFile(bool Async) : base(Async) { ;}
        #endregion
        
        #region Search / Fetch

        public List<FileHeader> Search(int claimID, int? userID, string claimGUID)
        {
            List<FileHeader> result = new List<FileHeader>();
            Claim clmObj = _Session.Claims[claimGUID];
            int sessionFileHeaderCount = clmObj.aFiles.Count;

            if (!IsAsync || sessionFileHeaderCount < 1)
            {// Sync or first time Async
                result = new FileHeaderService().Search(claimID, userID);
                clmObj.aFiles.AddRange(result);
                _Session.Claims[claimGUID] = clmObj;
            }

            return IsAsync ? clmObj.aFiles : result;//_Session.Claims[claimGUID]
        }

        public FileHeader GetFileHeaderById(int? id, Claim claimobj)
        {
            // IMP: It cn have -ve values for newly inserted records which are in session
            FileHeader newObj = new FileHeaderService().newObj;

            if (id.HasValue)
            {
                if (IsAsync) newObj = (claimobj.aFiles.SingleOrDefault(c => c.ID == id.Value)) ?? newObj;//_Session.Claims[claimobj.ClaimGUID]
                else newObj = new FileHeaderService().GetFileHeaderById(id.Value);
            }

            //HT: Make sure ClaimId is assigned after the above Claim obj is created
            newObj.ClaimID = claimobj.ID;
            newObj.ClaimGUID = claimobj.ClaimGUID;
            newObj.Archived = claimobj.Archived;

            return newObj;
        }

        #endregion

        #region Add / Edit / Delete

        public int AddEdit(FileHeader fileHeaderObj)
        {
            if (IsAsync)// Do Async add/edit
            {
                Claim claimObj = _Session.Claims[fileHeaderObj.ClaimGUID];
                claimObj.aFiles = fileHeaderObj.setProp(IsAsync).doOpr(claimObj.aFiles);
                _Session.Claims[fileHeaderObj.ClaimGUID] = claimObj;
                return fileHeaderObj.ID;
            }   
            else
                return new FileHeaderService().AddEdit(fileHeaderObj.setProp(IsAsync), true);
        }

        public void Delete(FileHeader fileHeaderObj)
        {
            fileHeaderObj._Deleted = true; fileHeaderObj._Added = fileHeaderObj._Edited = false;

            if (IsAsync)// Do delete
            {
                Claim data = _Session.Claims[fileHeaderObj.ClaimGUID];
                data.aFiles = fileHeaderObj.doOpr(data.aFiles);
                _Session.Claims[fileHeaderObj.ClaimGUID] = data;
            }
            else
                new FileHeaderService().Delete(fileHeaderObj, true);
        }

        #endregion
    }
}
