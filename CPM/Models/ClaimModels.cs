using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using System.Text.RegularExpressions;
using CPM.Services;
using CPM.Helper;

namespace CPM.DAL
{
    [Serializable]
    public abstract class Opr
    {
        #region Variables & Properties

        public const string sep = ";";

        public bool _Added { get; set;}
        public bool _Edited { get; set; }
        public bool _Deleted { get; set; }
        // common property required for all the Claim and its child objects
        public string ClaimGUID { get; set; }
        public bool Archived { get; set; } 
        #endregion

        // Set some required fields to proceed (mostly overridded in child class)
        public void setOpr(int ID)
        {// Default settings
            this._Added = (ID <= Defaults.Integer) && !(this._Deleted);
            this._Edited = (!this._Added) && !(this._Deleted);            
        }        
    }

    #region Claim Model (& vw_Claim_Master_User_Loc)

    [Serializable, System.Xml.Serialization.XmlRoot(ElementName = "Claim",IsNullable=true)]    
    public partial class Claim
    {
        #region Extra Variables & Properties

        public List<Comment> aComments { get; set; }
        public List<ClaimDetail> aItems { get; set; }
        public List<FileHeader> aFiles { get; set; }

        /*public bool AssignToChanged { get; set; }*/
        public string AssignToVal { get; set; }
        public string AssignToComment { get; set; }

        public string ClaimGUID { get; set; } // common property required for all the Claim and its child objects
        
        #endregion
    }
    
    [MetadataType(typeof(vw_Claim_Master_User_LocMetadata))]
    public partial class vw_Claim_Master_User_Loc
    {
        #region Extra Variables & Properties

        public int AssignedToOld { get; set; }
        public int StatusIDold { get; set; }
        public string ClaimGUID { get; set; }
        public string LocationAndCode { get { return Common.getLocationAndCode(this.Location, this.LocationCode); } }
        
        #endregion
    }

    public class vw_Claim_Master_User_LocMetadata
    {
        [DisplayName("Claim #")]
        [Required(ErrorMessage = "Claim #" + Defaults.RequiredMsgAppend)]
        public int ClaimNo { get; set; }

        [DisplayName("Status")]
        [Required(ErrorMessage = "Status" + Defaults.RequiredMsgAppend)]
        public int StatusID { get; set; }

        [DisplayName("Customer")]
        [Required(ErrorMessage = "Customer" + Defaults.RequiredMsgAppend)]
        public int CustID { get; set; }

        [DisplayName("Brand")]
        [Required(ErrorMessage = "Brand" + Defaults.RequiredMsgAppend)]
        public int BrandID { get; set; }

        [DisplayName("Claim Date")]
        [Required(ErrorMessage = "Claim Date" + Defaults.RequiredMsgAppend)]
        //[Range(typeof(DateTime), System.Data.SqlTypes.SqlDateTime.MinValue.ToString(), System.Data.SqlTypes.SqlDateTime.MaxValue.ToString())]//SO: 1406046
        [Range(typeof(DateTime), "1-Jan-1753", "31-Dec-9999")]
        public int ClaimDate { get; set; }

        [DisplayName("Customer Ref #")]
        [StringLength(30, ErrorMessage = Defaults.MaxLengthMsg)]
        public int CustRefNo { get; set; }

        [DisplayName("Location")]
        [Required(ErrorMessage = "Location" + Defaults.RequiredMsgAppend)]
        public int ShipToLocationID { get; set; }

        [DisplayName("Salesperson")]
        [Required(ErrorMessage = "Salesperson" + Defaults.RequiredMsgAppend)]
        public int SalespersonID { get; set; }
    }
    
    #endregion

    #region ClaimDetail Model

    [Serializable]
    [MetadataType(typeof(ClaimDetailMetadata))]
    public partial class ClaimDetail : Opr
    {
        #region Extra Variables & Properties

        public string ItemCode { get; set; }
        public string Defect { get; set; }
        public List<FileDetail> aDFiles { get; set; }
        [System.Xml.Serialization.XmlIgnore]//[System.Runtime.Serialization.IgnoreDataMember]//[NonSerialized(), FromJson]
        public string aDFilesJSON { get; set; }

        //HT: To save from divide by zero
        public decimal TDOriginal1 { get { return (TDOriginal > 0) ? TDOriginal : 1; } }
        
        public decimal RemainingTread1 { get { return TDRemaining * 100 / TDOriginal1; } }
        public decimal CreditAmt1 { get { return RemainingTread1 * CurrentPrice / 100; } }  public string CreditAmt1Str { get { return CreditAmt1.ToString("#0.00"); } }
        public decimal InvoiceAmt1 { get { return RemainingTread1 * CurrentCost / 100; } }  public string InvoiceAmt1Str { get { return InvoiceAmt1.ToString("#0.00"); } }

        #endregion

        /* Set some required fields to proceed
        public ClaimDetail setProp()
        {
            if (!_Deleted)
            {//set necessary fields for Add & Edit
                this.LastModifiedDate = DateTime.Now;
                this.RemainingTread = this.RemainingTread1;
                this.CreditAmt = this.CreditAmt1;
                this.InvoiceAmt = this.InvoiceAmt1;                
            }

            return this;
        }

        /// <summary>
        /// Add, Edit or Delete
        /// </summary>
        /// <param name="aComments"> List of object</param>
        /// <returns>Updated list of objects</returns>
        public List<ClaimDetail> doOpr(List<ClaimDetail> aItems)
        {
            if (aItems == null) aItems = new List<ClaimDetail>();//When there're NO records

            int index = aItems.FindIndex(p => p.ID == this.ID);//SO: 361921/list-manipulation-in-c-using-linq
            List<FileDetail> dFiles = index > -1?aItems[index].aDFiles: null;//HT: SPECIAL CASE: Store to keep them intact
            base.setOpr(ID);//Set Add or Edit

            #region Set data as per Operation

            if (_Deleted)//Deleted =================
            {
                if (ID < 0) aItems.RemoveAt(index);//remove newly added
                else aItems[index]._Deleted = true;
            }
            else if (_Edited)//Edited=================
            {
                aItems[index] = this;
            }
            else //Added(or Newly added is edited)================
            {
                #region New record: we assign -ve ClaimId to avoid conflicts
                if (index < 0)
                {
                    ID = (aItems.Count > 0) ? (aItems.Min(c => c.ID) - 1) : Defaults.Integer - 1;
                    while (ID >= 0) { ID = (ID > 0 ? 0 : ID) - 1; }//Make it < 0
                    aItems.Add(this);
                }
                #endregion
                else //Newly added is edited(we still maintain the flag until final commit)
                    aItems[index] = this;
            }

            #endregion

            if (index > -1 && !_Deleted)//HT: SPECIAL CASE: Put the files back into the object
                aItems[index].aDFiles = dFiles;

            return aItems;
        }
        /// <summary>
        /// Add items to list
        /// </summary>
        /// <param name="parent">Session Claim value</param>
        /// <param name="records">Items</param>
        /// <returns>Updated Claim variable</returns>
        public static Claim lstAsync(Claim parent, List<ClaimDetail> records)
        {
            if (parent == null || records == null || records.Count < 1) return parent;

            parent.aItems.AddRange(records);
            return parent;
        }
        */
    }

    public class ClaimDetailMetadata
    {
        [DisplayName("Item Code")]
        [Required(ErrorMessage = "Select an Item.")]
        [Range(1, int.MaxValue, ErrorMessage = "Invalid Item")]// SO: 3345348
        public int ItemID { get; set; }

        [DisplayName("Remaining TD (32nds)")]
        [Required(ErrorMessage = "Remaining TD (32nds)" + Defaults.RequiredMsgAppend)]
        public int TDRemaining { get; set; }

        [DisplayName("Original TD (32nds)")]
        [Required(ErrorMessage = "Original TD (32nds)" + Defaults.RequiredMsgAppend)]
        public int TDOriginal { get; set; }

        [DisplayName("Defect")]
        [Required(ErrorMessage = "Nature of Defect" + Defaults.RequiredMsgAppend)]
        public int NatureOfDefect { get; set; }

        [DisplayName("Current Price")]
        [Required(ErrorMessage = "Current Price" + Defaults.RequiredMsgAppend)]
        public int CurrentPrice { get; set; }

        [DisplayName("CurrentCost")]
        [Required(ErrorMessage = "Current Cost" + Defaults.RequiredMsgAppend)]
        public int CurrentCost { get; set; }

        [StringLength(250, ErrorMessage = Defaults.MaxLengthMsg)]
        public string Description { get; set; }

        [StringLength(250, ErrorMessage = Defaults.MaxLengthMsg)]
        public string Note { get; set; }
    }
    
    #endregion

    #region Comment Model
    
    [MetadataType(typeof(CommentMetadata))]    
    public partial class Comment : Opr
    {
        #region Extra Variables & Properties
        public string CommentBy { get; set; }
        #endregion
    }

    public class CommentMetadata
    {
        [DisplayName("Comment")]
        [Required(ErrorMessage = "Comment" + Defaults.RequiredMsgAppend)]
        // Based on : http://www.w3schools.com/SQl/sql_datatypes.asp
        [StringLength(4000, ErrorMessage = Defaults.MaxLengthMsg)]
        public string Comment1 { get; set; }

        [DisplayName("Comment By")]
        public string CommentBy { get; set; }
    }
    
    #endregion

    #region FileHeader Model
    
    [MetadataType(typeof(FileHeaderMetadata))]
    public partial class FileHeader : Opr
    {
        #region Extra Variables & Properties

        /*string _ClaimGUID;
        public string ClaimGUID
        {
            get
            { // Return _ClaimGUID only for Async entries
                return string.IsNullOrEmpty(_ClaimGUID) ? ClaimID.ToString() : _ClaimGUID;
            }
            set { _ClaimGUID = value; }
        }*/

        public string UploadedBy { get; set; }
        public string FileNameNEW { get; set; }
        public string FileTypeTitle { get; set; }
        public string CodeStr
        {
            get
            {
                if (_Added) //string.Empty;
                    return HttpUtility.UrlEncode(CPM.Helper.Crypto.EncodeStr(FileName + sep + ClaimID.ToString() + sep + ClaimGUID, true));
                else
                    return HttpUtility.UrlEncode(CPM.Helper.Crypto.EncodeStr(FileName + sep + ClaimID.ToString(), true));
            }
        } // Can't use HttpUtility.UrlDecode - because it'll create issues with string.format and js function calls so handle in GetFile
        
        public string FilePath { //HT: Usage: <a href='<%= Url.Content("~/" + item.FilePath) %>' target="_blank">
            get
            {
                return FileIO.GetClaimFilePath(ClaimID, ClaimGUID, FileName, webURL: true);
            }
        }

        #endregion
    }

    public class FileHeaderMetadata
    {
        [DisplayName("File")]
        /* HT: DON'T - we've handled it from within the controller along with a special case of Update
         [Required(ErrorMessage = "Select a file to be uploaded")] */
        public string FileName { get; set; }

        [DisplayName("Uploaded By")]        
        public string UploadedBy { get; set; }

        [DisplayName("Type")]
        public string FileTypeTitle { get; set; }

        [Required(ErrorMessage = "File Type" + Defaults.RequiredMsgAppend)]
        public string FileType { get; set; }

        [StringLength(250, ErrorMessage = Defaults.MaxLengthMsg)]
        public string Comment { get; set; }
    }
    
    #endregion

    #region FileDetail Model
    [Serializable]
    [MetadataType(typeof(FileDetailMetadata))]    
    public partial class FileDetail : Opr
    {
        #region Extra Variables & Properties
        
        public string UploadedBy { get; set; }
        public string FileNameNEW { get; set; }
        public string FileTypeTitle { get; set; }

        public string CodeStr
        {
            get
            {
                if (_Added) //string.Empty;
                    return HttpUtility.UrlEncode(CPM.Helper.Crypto.EncodeStr(FileName + sep + ClaimDetailID.ToString() + sep + ClaimGUID, true));
                else
                    return HttpUtility.UrlEncode(CPM.Helper.Crypto.EncodeStr(FileName + sep + ClaimDetailID.ToString(), true));
            }
        } // Can't use HttpUtility.UrlDecode - because it'll create issues with string.format and js function calls so handle in GetFile

        public string FilePath
        { //HT: Usage: <a href='<%= Url.Content("~/" + item.FilePath) %>' target="_blank">
            get
            {
                return FileIO.GetClaimFilePath(ClaimID, ClaimGUID, FileName, ClaimDetailID, true);
            }
        }
        
        #endregion
    }

    public class FileDetailMetadata
    {
        [DisplayName("File")]
        /* HT: DON'T - we've handled it from within the controller along with a special case of Update
         [Required(ErrorMessage = "Select a file to be uploaded")] */
        public string FileName { get; set; }

        [DisplayName("Uploaded By")]
        public string UploadedBy { get; set; }

        [DisplayName("Type")]
        public string FileTypeTitle { get; set; }

        [Required(ErrorMessage = "File Type" + Defaults.RequiredMsgAppend)]
        public string FileType { get; set; }

        [StringLength(250, ErrorMessage = Defaults.MaxLengthMsg)]
        public string Comment { get; set; }
    }
    
    #endregion
}
