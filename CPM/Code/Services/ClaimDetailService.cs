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
    public class ClaimDetailService : _ServiceBase
    {
        #region Variables & Constructor
        
        public ClaimDetailService() : base() {;}
        public ClaimDetailService(CPMmodel dbcExisting) : base(dbcExisting) { ;}
        public readonly ClaimDetail newObj = new ClaimDetail();

        #endregion

        #region Search / Fetch

        public List<ClaimDetail> Search(int claimID, int? userID)
        {
            //using (dbc)//HT: DON'T coz we're sending IQueryable
            var cQuery = from c in dbc.vw_ClaimDetail_Item_Defects
                         where c.ClaimID == claimID
                         orderby c.ID // Don't use ItemNumber because it might scramble the sequence
                         select c;

            #region Well, we've to fetch it all because for Async mode we'll need ALL Items
            //HT: IMP: Finally got to convert Unknown type to strong type !
            //http://stackoverflow.com/questions/1458737/linq2sql-explicit-construction-of-entity-type-some-type-in-query-is-not-allo
            IEnumerable<ClaimDetail> cd = cQuery.AsEnumerable().Select(c => new ClaimDetail
                {
                    ID = c.ID,
                    ClaimID = c.ClaimID,
                    ItemID = c.ItemID,
                    Type = c.Type,
                    Description = c.Description,
                    DOT = c.DOT,
                    NatureOfDefect = c.NatureOfDefect,
                    Note = c.Note,
                    Ply = c.Ply,
                    RemainingTread = c.RemainingTread,
                    Serial = c.Serial,
                    Size = c.Size,
                    TDOriginal = c.TDOriginal,
                    TDRemaining = c.TDRemaining,
                    CreditAmt = c.CreditAmt,
                    InvoiceAmt = c.InvoiceAmt,
                    CurrentCost = c.CurrentCost,
                    CurrentPrice = c.CurrentPrice,
                    ItemCode = c.ItemCode,
                    Defect = c.Defect,
                    LastModifiedBy = c.LastModifiedBy,
                    LastModifiedDate = c.LastModifiedDate
                });
            #endregion

            return cd.ToList();
        }
                
        public ClaimDetail GetClaimDetailById(int id)
        {
            if(id <= Defaults.Integer) 
                return newObj;

            using (dbc)
            {
                ClaimDetail itm = (from cd in dbc.ClaimDetails
                                  join i in dbc.MasterInventories on new { ItemID = cd.ItemID } equals new { ItemID = i.ID }
                                  join d in dbc.MasterDefects on new { NatureOfDefect = cd.NatureOfDefect } equals new { NatureOfDefect = d.ID }
                                  where cd.ID == id 
                                  select Transform(cd, i.ItemNo, d.Title)).SingleOrDefault<ClaimDetail>();

                if (itm == null) return newObj;
                //itm.Claim = new Claim();//HT: So that it doesn't complain NULL later
                return itm;
            }
        }

        ClaimDetail Transform(ClaimDetail c, string ItemCode, string Defect)
        {
            return c.Set(c1 => { c1.ItemCode = ItemCode; c1.Defect = Defect; });
        }

        #endregion
                
        #region Add / Edit / Delete & Bulk

        public int Add(ClaimDetail detailObj, bool doSubmit)
        {
            //Set lastmodified fields
            detailObj.LastModifiedBy = _SessionUsr.ID;
            detailObj.LastModifiedDate = DateTime.Now;

            dbc.ClaimDetails.InsertOnSubmit(detailObj);
            if(doSubmit) dbc.SubmitChanges();

            return detailObj.ID; // Return the 'newly inserted id'
        }
                
        public int AddEdit(ClaimDetail detailObj, bool doSubmit)
        {
            if (detailObj.ID <= Defaults.Integer) // Insert
                return Add(detailObj,doSubmit);

            else
            {
                #region Update
                //Set lastmodified fields
                detailObj.LastModifiedBy = _SessionUsr.ID;
                detailObj.LastModifiedDate = DateTime.Now;

                dbc.ClaimDetails.Attach(detailObj);//attach the object as modified
                dbc.Refresh(System.Data.Linq.RefreshMode.KeepCurrentValues, detailObj);//Optimistic-concurrency (simplest solution)
                #endregion

                if (doSubmit) dbc.SubmitChanges();
            }

            return detailObj.ID;
        }
                
        public void Delete(ClaimDetail detailObj, bool doSubmit)
        {
            dbc.ClaimDetails.DeleteOnSubmit(dbc.ClaimDetails.Single(c => c.ID == detailObj.ID));
            if(doSubmit) dbc.SubmitChanges();
        }

        public void BulkAddEditDel(List<ClaimDetail> records, Claim claimObj, bool doSubmit, bool isNewClaim, CPMmodel dbcContext)
        {
            //using{dbc}, try-catch and transaction must be handled in callee function
            foreach (ClaimDetail item in records)
            {
                #region Perform DB operations
                item.ClaimID = claimObj.ID; //Required when adding new Claim
                item.LastModifiedBy = _SessionUsr.ID;
                item.LastModifiedDate = DateTime.Now;
                int oldClaimDetailId = item.ID;//store old id

                //Special case handling for IE with KO - null becomes "null"
                if (item.Note == "null") item.Note = "";
                if (item.Serial == "null") item.Serial = "";
                if (item.DOT == "null") item.DOT = "";

                if (item._Deleted)
                {
                    Delete(item, false);
                    // Make sure the Existing as well as Uploaded (Async) files are deleted
                    FileIO.DeleteDirectory(FileIO.GetClaimFilesDirectory(claimObj.ID, claimObj.ClaimGUID, item.ID));                    
                }
                else if (item._Edited)
                    AddEdit(item, false);
                else if (item._Added)
                    Add(item, false);
                
                #endregion

                if (doSubmit) //Make a FINAL submit here instead of periodic updates
                    dbc.SubmitChanges();

                // Finally, If Item is NOT deleted then process its Files
                if (!item._Deleted && item.aDFiles != null && item.aDFiles.Count() > 0) // Make sure item is not deleted
                    new FileDetailService(dbc).BulkAddEditDel(item.aDFiles, claimObj, oldClaimDetailId, item.ID, doSubmit, dbcContext, (isNewClaim|| item._Added));
            }
            
        }

        #endregion
    }
}
