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

        public void BulkAddEditDel(List<FileHeader> records, Claim claimObj, bool doSubmit, CPMmodel dbcContext, bool isNewClaim)
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
            if(isNewClaim)
                FileIO.MoveFilesFolderNewClaimOrItem(claimObj.ID, claimObj.ClaimGUID);
            else
                ProcessFiles(records, claimObj.ID, claimObj.ClaimGUID);
        }

        #endregion

        #region Extra functions

        void ProcessFiles(List<FileHeader> records, int claimID, string claimGUID)
        {
            if (records == null || records.Count < 1) return;

            foreach (FileHeader item in records)
                if (item._Deleted)//Delete will always be for existing not Async (so use ClaimID & not GUID)
                    FileIO.DeleteClaimFile(claimID, "", item.FileName);

            if(records.Count > 0) //finally copy all the files from H_Temp to H
            FileIO.StripGUIDFromClaimFileName(claimID, claimGUID);
        }

        #endregion
    }
}
