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
    public class FileDetailService : _ServiceBase
    {
        #region Variables & Constructor

        public FileDetailService() : base() {;}
        public FileDetailService(CPMmodel dbcExisting) : base(dbcExisting) { ;}

        public readonly FileDetail newObj = new FileDetail() { ID = Defaults.Integer };

        #endregion

        #region Search / Fetch

        public List<FileDetail> Search(int claimID, int claimDetailID)
        {
            //using (dbc)//HT: DON'T coz we're sending IQueryable
            IQueryable<FileDetail> cQuery = from f in dbc.FileDetails

                                            #region LEFT OUTER JOINs

                                            //LEFT OUTER JOIN For User
                                            join u in dbc.Users on new { UserID = f.UserID } equals
                                            new { UserID = u.ID } into u_join
                                            from u in u_join.DefaultIfEmpty()
                                            //LEFT OUTER JOIN For User
                                            join t in dbc.MasterFileTypeDetails on
                                         new { TypID = f.FileType } equals new { TypID = t.ID } into t_join
                                            from t in t_join.DefaultIfEmpty()
                                            
                                            #endregion

                                            where (f.ClaimID == claimID && f.ClaimDetailID == claimDetailID)
                                            orderby f.UploadedOn descending
                                            select Transform(f, u.Name, t.Title);

            return cQuery.ToList<FileDetail>();
        }

        FileDetail Transform(FileDetail f, string fileDetailBy, string fileTypeTitle)
        {
            return f.Set(f1 => { f1.UploadedBy = fileDetailBy; f1.FileTypeTitle = fileTypeTitle; f1.ClaimGUID = f1.ClaimID.ToString(); });
        }
                
        public FileDetail GetFileDetailById(int id)
        {
            using (dbc)
            {
                FileDetail cmt = (from f in dbc.FileDetails where f.ID == id select f).SingleOrDefault<FileDetail>();
                //cmt.Claim = new Claim();//HT: So that it doesn't complain NULL later
                return cmt;
            }
        }

        #endregion

        #region Add / Edit / Delete
                
        public int Add(FileDetail fileDetailObj, bool doSubmit)
        {
            //Set lastmodified fields
            fileDetailObj.LastModifiedBy = _SessionUsr.ID;            
            fileDetailObj.LastModifiedDate = DateTime.Now;            

            dbc.FileDetails.InsertOnSubmit(fileDetailObj);
            if(doSubmit) dbc.SubmitChanges();

            return fileDetailObj.ID; // Return the 'newly inserted id'
        }
                
        public int AddEdit(FileDetail fileDetailObj, bool doSubmit)
        {
            fileDetailObj.UploadedOn = Defaults.getValidDate(fileDetailObj.UploadedOn); // special case to ensure valid SQLDate
            if (fileDetailObj.ID <= Defaults.Integer) // Insert
                return Add(fileDetailObj, doSubmit);

            else
            {
                #region Update
                //Set lastmodified fields
                fileDetailObj.LastModifiedBy = _SessionUsr.ID;                
                fileDetailObj.LastModifiedDate = DateTime.Now;                

                dbc.FileDetails.Attach(fileDetailObj);//attach the object as modified
                dbc.Refresh(System.Data.Linq.RefreshMode.KeepCurrentValues, fileDetailObj);//Optimistic-concurrency (simplest solution)
                #endregion

                if(doSubmit) dbc.SubmitChanges();
            }

            return fileDetailObj.ID;
        }
                
        public void Delete(FileDetail fileDetailObj, bool doSubmit)
        {
            dbc.FileDetails.DeleteOnSubmit(dbc.FileDetails.Single(f => f.ID == fileDetailObj.ID));
            if(doSubmit) dbc.SubmitChanges();
        }

        public void BulkAddEditDel(List<FileDetail> records, Claim claimObj, int oldclaimDetailId, int claimDetailId, bool doSubmit,
            CPMmodel dbcContext, bool isNewClaim)
        {
            //using{dbc}, try-catch and transaction must be handled in callee function
            foreach (FileDetail item in records)
            {
                #region Perform Db operations
                item.LastModifiedBy = _SessionUsr.ID;
                item.LastModifiedDate = DateTime.Now;
                item.UploadedOn = DateTime.Now;// double ensure dates are not null !
                //HT: MUST: MAke sure this is set (i.e. the newly added Claimetail ID or it'll give FK err)
                item.ClaimDetailID = claimDetailId;
                item.ClaimID = claimObj.ID;

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
                {//Special case: Call the econd overload which has "doSubmit" parameter
                    new ActivityLogService(ActivityLogService.Activity.ClaimFileUpload, dbcContext).Add(
                        new ActivityHistory()
                        {
                            FileName = item.FileName,
                            ClaimID = item.ClaimID,
                            ClaimDetailID = item.ClaimDetailID,
                            ClaimText = claimObj.ClaimNo.ToString()
                        }, doSubmit);
                }
                #endregion
            }
            if (doSubmit) dbc.SubmitChanges(); //Make a FINAL submit instead of periodic updates
            //Move Item detail files
            if (isNewClaim)
                FileIO.MoveFilesFolderNewClaimOrItem(claimObj.ID, claimObj.ClaimGUID, oldclaimDetailId, claimDetailId);
            else
                ProcessFiles(records, claimObj.ID, claimObj.ClaimGUID, oldclaimDetailId, claimDetailId);
        }

        #endregion

        #region Extra functions

        void ProcessFiles(List<FileDetail> records, int claimID, string claimGUID, int oldclaimDetailId, int claimDetailId)
        {
            if (records == null || records.Count < 1) return;

            foreach (FileDetail item in records)
                if (item._Deleted)//Delete will always be for existing not Async(so use ClaimID & not GUID)
                    FileIO.DeleteClaimFile(claimID, "", item.FileName, claimDetailId);//HT:CAUTION: Don't use the item.ID

            if (records.Count > 0)//finally copy all the files from D_Temp to D
            FileIO.StripGUIDFromClaimFileName(claimID, claimGUID, oldclaimDetailId, claimDetailId);
        }

        #endregion
    }
}
