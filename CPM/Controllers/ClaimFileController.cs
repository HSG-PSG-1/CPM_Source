using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CPM.DAL;
using CPM.Services;
using CPM.Helper;

namespace CPM.Controllers
{
    //[CompressFilter] - Not needed
    public partial class ClaimController : BaseController
    {
        FileIO.mode HeaderFM { get { return (IsAsync ? FileIO.mode.asyncHeader : FileIO.mode.header); } }
        FileIO.mode DetailFM { get { return (IsAsync ? FileIO.mode.asyncDetail: FileIO.mode.detail); } }
        
        #region Actions for File Header (Secured)
        [AccessClaim("ClaimID")]
        public ActionResult FilesPg(int ClaimID, string ClaimGUID, int? FileHeaderID)
        {
            ViewData["ClaimGUID"] = ClaimGUID; // Need to pass GUID
            //For separate Files page
            return View();
        }

        [CacheControl(HttpCacheability.NoCache), HttpGet]
        public PartialViewResult Files(int ClaimID, string ClaimGUID, int? FileHeaderID)
        {
            //if (_Session.Claims[ClaimGUID].Archived)
            //return PartialView(
            //    "~/Views/Claim/EditorTemplates/Files.cshtml", new FileHeaderService().Search(ClaimID, null));

            #region Set FileHeader object (if FileHeaderID != null) & set ViewData
            
            FileHeader newObj = new FileHeader();
            //Files List (Claim\1\Files) & Edit (Claim\1\Files\2)
            if (TempData["PRGModel"] != null)
                new CPM.Models.PRGModel(TempData["PRGModel"]).ExtractData<FileHeader>(ref newObj, ModelState);
            else
                newObj = new CAWFile(IsAsync).GetFileHeaderById(FileHeaderID, _Session.Claims[ClaimGUID]);
            
            #endregion

            ViewData["FileHeaderObj"] = newObj;
            //HT: CAUTION: MAKE SURE the control name differs from that of ViewData key !!!
            //http://stackoverflow.com/questions/624828/asp-net-mvc-html-dropdownlist-selectedvalue
            ViewData["FileTypes"] = new LookupService().GetLookup(LookupService.Source.FileHeader);

            return PartialView(
                "~/Views/Claim/EditorTemplates/Files.cshtml", new CAWFile(IsAsync).Search(ClaimID, null, ClaimGUID));
        }

        [SkipModelValidation]
        [AccessClaim("ClaimID")]
        [HttpPost]
        public ActionResult FileHeaderDelete(int ClaimID, string ClaimGUID, int FileHeaderID)
        {
            bool proceed = false;
            FileHeader delFH = new CAWFile(IsAsync).GetFileHeaderById(FileHeaderID, _Session.Claims[ClaimGUID]);

            #region Delete File

            //If its Async - we can delete the TEMP file, if its sync the file is not present in TEMP folder so delete is not effective
            if (FileIO.DeleteClaimFile(delFH.FileName, delFH.ClaimGUID, null, HeaderFM)){
                //HT: INFER: Delete file for Async, Sync and (existing for Async - 
                //the above delete will cause no effect coz path is diff)
                new CAWFile(IsAsync).Delete(new FileHeader() { ID = FileHeaderID, ClaimGUID = ClaimGUID });
                proceed = true;
            }
            else
                proceed = false;

            #endregion

            //Taconite XML
            return this.Content(Defaults.getTaconite(proceed,
                Defaults.getOprResult(proceed, "Unable to delete file"), "fileOprMsg"), "text/xml");
        }

        [HttpPost]
        [AccessClaim("ClaimID")]//To make sure no malicious file upload is possible in any way!
        public ActionResult Files(int ClaimID, FileHeader FileHdrObj)
        {
            ViewData["FileHeaderObj"] = FileHdrObj;

            #region New file upload
            
            if ((FileHdrObj.FileNameNEW ?? FileHdrObj.FileName) != FileHdrObj.FileName)
            {//HT Delete old\existing file? For Async need to wait until final commit
                //HT:IMP: Set Async so that now the file maps to Async file-path
                FileHdrObj.IsAsync = true;
                //FileHdrObj.ClaimGUID = _Session.Claim.ClaimGUID; // to be used further
                #region Old code (make sure the function 'ChkAndSaveClaimFile' does all of it)
                //string docName = string.Empty;
                //FileIO.result uploadResult = SaveClaimFile(Request.Files["FileNameNEW"], ref docName, ClaimID, true);

                //if (uploadResult != FileIO.result.successful)
                //    if (uploadResult == FileIO.result.duplicate)
                //        ModelState.AddModelError("FileName", "Duplicate file found");
                //    else
                //        ModelState.AddModelError("FileName", "Unable to upload file");
                #endregion
                FileHdrObj.FileName = ChkAndSaveClaimFile("FileNameNEW", ClaimID, HeaderFM, FileHdrObj.ClaimGUID);
            }

            #endregion

            if (string.IsNullOrEmpty(FileHdrObj.FileName))
                ModelState.AddModelError("FileName", "Select a file to be uploaded");

            #region Process based on ModelState
            
            if (ModelState.IsValid)
            {
                new CAWFile(IsAsync).AddEdit(FileHdrObj);//new FileHeaderService().AddEdit(FileHdrObj);
                return RedirectToAction("Files", new { ClaimGUID = FileHdrObj.ClaimGUID });
            }
            else
            {
                //http://stackoverflow.com/questions/279665/how-can-i-maintain-modelstate-with-redirecttoaction
                //TempData["ViewData"] = ViewData;//Store in temp intermediate variable
                TempData["PRGModel"] = new CPM.Models.PRGModel().SetPRGModel<FileHeader>(FileHdrObj, ModelState);
                return RedirectToAction("Files", new { ClaimGUID = FileHdrObj.ClaimGUID });
            }

            #endregion
        }

        #endregion

        #region Actions for File Detail (Secured)
        
        //Common action for List (Claim\1\FilesDetail) & Edit (Claim\1\FilesDetail\2)
        [AccessClaim("ClaimID")]
        [CacheControl(HttpCacheability.NoCache), HttpGet]
        public ActionResult FilesDetail(int ClaimID, int ClaimDetailID, string ClaimGUID, int? FileDetailID)
        {//Claim/1/FilesDetail/2?
            #region Set FileDetail object (if FileDetailID != null) & set ViewData
            
            FileDetail newObj = new FileDetail();
            if (TempData["PRGModel"] != null)
                new CPM.Models.PRGModel(TempData["PRGModel"]).ExtractData<FileDetail>(ref newObj, ModelState);//if (TempData["ViewData"] != null)ViewData = (ViewDataDictionary)TempData["ViewData"];
            else
                newObj =  new CAWdFile(IsAsync).GetFileDetailById(FileDetailID, ClaimDetailID, _Session.Claims[ClaimGUID]);            
            
            #endregion

            ViewData["FileDetailObj"] = newObj;
            //HT: CAUTION: MAKE SURE your control name differs from that of ViewData key !!!
            //http://stackoverflow.com/questions/624828/asp-net-mvc-html-dropdownlist-selectedvalue
            ViewData["FileTypes"] = new LookupService().GetLookup(LookupService.Source.FileDetail);

            return View(new CAWdFile(IsAsync).Search(ClaimID, ClaimDetailID, ClaimGUID));//.Cast<FileDetail>()
        }
                
        /* OLD REF: FilesDetailDelete
         * [SkipModelValidation]
        [AccessClaim("ClaimID")]
        public ActionResult FilesDetailDelete(int ClaimID, int FileDetailID, int ClaimDetailID)
        {
            FileDetail delFH = new CAWdFile(IsAsync).GetFileDetailById(FileDetailID, ClaimDetailID);

            #region Delete File
            
            //If its Async - we can delete the TEMP file, if its sync we obviously gotto delete it :-)
            if (FileIO.DeleteClaimFile(delFH.FileName, delFH.ClaimID, ClaimDetailID, DetailFM))
            {   //HT: INFER: Delete the file for Async, Sync and (existing for Async - 
                //the above delete will cause no effect coz path will be diff)
                new CAWdFile(IsAsync).Delete(new FileDetail() { ID = FileDetailID }, ClaimDetailID);
                return RedirectToAction("FilesDetail",new {ClaimDetailID = ClaimDetailID, ClaimGUID = ClaimGUID });
            }
            else
            {
                ModelState.AddModelError("FileName", "Unable to delete file");
                TempData["ViewData"] = ViewData;//Store in temp intermediate variable
                return RedirectToAction("FilesDetail", new { ClaimDetailID = ClaimDetailID , ClaimGUID = ClaimGUID });
            }

            #endregion
        }*/

        [SkipModelValidation]
        [AccessClaim("ClaimID")]
        [HttpPost]
        public ActionResult FilesDetailDeleteTaco(int ClaimID, int FileDetailID, int ClaimDetailID, string ClaimGUID)
        {
            bool proceed = false;
            FileDetail delFD = new CAWdFile(IsAsync).GetFileDetailById(FileDetailID, ClaimDetailID, _Session.Claims[ClaimGUID]);

            #region Delete File

            //If its Async - we can delete the TEMP file, if its sync the file is not present in TEMP folder so delete is not effective
            if (FileIO.DeleteClaimFile(delFD.FileName, delFD.ClaimGUID, ClaimDetailID, DetailFM))
            {//HT: INFER: Delete the file for Async, Sync and (existing for Async - the above delete will cause no effect coz path will be diff)
                new CAWdFile(IsAsync).Delete(new FileDetail() { ID = FileDetailID, ClaimGUID = ClaimGUID, ClaimDetailID = ClaimDetailID });
                proceed = true;
            }
            else
                proceed = false;

            #endregion

            //Taconite XML
            return this.Content(Defaults.getTaconite(proceed,
                Defaults.getOprResult(proceed, "Unable to delete file")), "text/xml");
        }

        [HttpPost]
        [AccessClaim("ClaimID")]//To make sure no malicious file upload is possible in any way!
        public ActionResult FilesDetail(int ClaimID, int ClaimDetailID, FileDetail FileDetailObj)
        {
            //FileDetailObj.ClaimGUID = ClaimGUID;
            ViewData["FileDetailObj"] = FileDetailObj;

            if ((FileDetailObj.FileNameNEW ?? FileDetailObj.FileName) != FileDetailObj.FileName)//New file uploaded
            {
                //HT:IMP: Set Async so that now the file maps to Async file-path
                FileDetailObj.IsAsync = true;
                //FileDetailObj.ClaimGUID = _Session.Claim.ClaimGUID; // to be used further
                FileDetailObj.FileName = 
                    ChkAndSaveClaimFile("FileNameNEW", ClaimID, DetailFM, FileDetailObj.ClaimGUID, FileDetailObj.ClaimDetailID); 
            }

            if (string.IsNullOrEmpty(FileDetailObj.FileName))
                ModelState.AddModelError("FileName", "Select a file to be uploaded");

            #region Process based on ModelState

            if (ModelState.IsValid)
            {
                new CAWdFile(IsAsync).AddEdit(FileDetailObj,ClaimDetailID);//new FileDetailService().AddEdit(FileDetailObj);
                return RedirectToAction("FilesDetail", new { ClaimDetailID = ClaimDetailID,
                    ClaimGUID = FileDetailObj.ClaimGUID });
            }
            else
            {
                //http://stackoverflow.com/questions/279665/how-can-i-maintain-modelstate-with-redirecttoaction
                //TempData["ViewData"] = ViewData;//Store in temp intermediate variable
                TempData["PRGModel"] = new CPM.Models.PRGModel().SetPRGModel<FileDetail>(FileDetailObj, ModelState);
                return RedirectToAction("FilesDetail", new { ClaimDetailID = ClaimDetailID, ClaimGUID = FileDetailObj.ClaimGUID});
            }

            #endregion
        }

        #endregion

        #region Extra Functions

        /// <summary>
        /// Check and Save Claim File being uploaded. Set error in ModelState if any issue
        /// </summary>
        /// <param name="hpFileKey">HttpPost file browser control Id</param>
        /// <param name="ClaimId">Claim Id</param>
        /// <param name="ClaimDetailId">Claim Detail Id</param>
        /// <param name="upMode">FileIO.mode (Async or Sync & Header  or Detail)</param>
        /// <returns>File upload name</returns>
        string ChkAndSaveClaimFile(string hpFileKey, int ClaimId, FileIO.mode upMode, string ClaimGUID, int? ClaimDetailId = null)
        {
            HttpPostedFileBase hpFile = Request.Files[hpFileKey];

            string docName = string.Empty;
            FileIO.result uploadResult = FileIO.UploadAndSave(hpFile, ref docName, ClaimGUID, ClaimDetailId, upMode);

            #region Add error in case of an Upload issue

            switch (uploadResult)
            { 
                case FileIO.result.duplicate:
                    ModelState.AddModelError("FileName", "Duplicate file found"); break;
                case FileIO.result.noextension:
                    ModelState.AddModelError("FileName", "File must have an extension"); break;
                case FileIO.result.contentLength:
                    ModelState.AddModelError("FileName", string.Format("File size cannot exceed {0}MB", Config.MaxFileSizMB)); break;
                case FileIO.result.successful: break;
                default://Any other issue
                    ModelState.AddModelError("FileName", "Unable to upload file"); break;
            }

            #endregion

            return docName;
        }

        /* Required only if we need to upload files from within Item Entry (not Files dialog)
        /// <summary>
        /// Process Serial & DOT file upload in Item entry
        /// </summary>
        /// <param name="IsSerial">True if it a Serial file else false</param>
        /// <param name="hpFileKey">HttpPost file browser control Id</param>
        /// <param name="ClaimId1">Claim Id</param>
        /// <param name="ClaimDetailID1">Claim Detail Id</param>
        /// <returns>True if operation success or can ignore & proceed</returns>
        bool ProcessDetailFiles(bool IsSerial, string hpFileKey, int ClaimId1, int ClaimDetailID1)
        {
            HttpPostedFileBase hpFile = Request.Files[hpFileKey];
            //If so - ignore and proceed
            if (hpFile == null || string.IsNullOrEmpty(hpFile.FileName) || hpFile.ContentLength < 1)
                return true;
            //Check & save file
            string docName = ChkAndSaveClaimFile(hpFileKey, ClaimId1, DetailFM, ClaimDetailID1);

            #region Configure and add Object
            FileDetail FileDetailObj = new FileDetail()
            {
                ClaimID = ClaimId1,
                ClaimDetailID = ClaimDetailID1,
                FileName = docName,
                FileType = FileDetail.GetType(IsSerial),
                //GetType = IsSerial ? (int)FileDetail.FileTypSD.Serial : (int)FileDetail.FileTypSD.DOT,
                FileTypeTitle = FileDetail.GetTypeTitle(IsSerial),
                UploadedOn = DateTime.Now,
                UserID = _SessionUsr.ID
            };

            int newID = new CAWdFile(IsAsync).AddEdit(FileDetailObj, ClaimDetailID1); 
            //if sync it shud handle: new FileDetailService().Add(FileDetailObj);
            #endregion

            return true;
        }
        */

        #endregion

        #region Extra Actions and functions to get code for file download

        //Get Header File
        [ValidateInput(false)] // SO: 2673850/validaterequest-false-doesnt-work-in-asp-net-4
        public ActionResult GetFile()
        {
            try
            {
                string code = "";
                try { code = Request.QueryString.ToString(); }catch (HttpRequestValidationException httpEx)
                { code = Request.RawUrl.Split(new char[] { '?' })[1]; }//SPECIAL CASE for some odd codes!

                string[] data = DecodeQSforFile(code);
                string filename = data[0];

                #region SPECIAL CASE for Async uploaded file
                if (string.IsNullOrEmpty(filename))
                { // Can't use HttpUtility.UrlDecode in CodeStr property 
                  //- because it'll create issues with string.format and js function calls so handle in GetFile
                    data = DecodeQSforFile(HttpUtility.UrlDecode(code));
                    filename = data[0];
                }
                #endregion
                string ClaimId = data[1];
                bool Async = bool.Parse(data[2]);//This must parse correctly
                //Send file stream for download
                return SendFile(ClaimId, null, (Async ? FileIO.mode.asyncHeader : FileIO.mode.header), filename);
            }
            catch (Exception ex) { ViewData["Message"] = "File not found"; return View("DataNotFound"); }
        }
        //Get Detail File
        [ValidateInput(false)] // SO: 2673850/validaterequest-false-doesnt-work-in-asp-net-4
        public ActionResult GetFileD()
        {
            try
            {
                string code = Request.QueryString.ToString();
                string[] data = DecodeQSforFile(code);
                string filename = data[0]; 
                
                #region SPECIAL CASE for Async uploaded file
                if (string.IsNullOrEmpty(filename))
                { // Can't use HttpUtility.UrlDecode in CodeStr property 
                    //- because it'll create issues with string.format and js function calls so handle in GetFile
                    data = DecodeQSforFile(HttpUtility.UrlDecode(code));
                    filename = data[0];
                }
                #endregion
                string ClaimId = data[1]; int ClaimDetailId = int.Parse(data[2]);
                bool Async = bool.Parse(data[3]);//This must parse correctly

                //Send file stream for download
                return SendFile(ClaimId, ClaimDetailId, (Async ? FileIO.mode.asyncDetail : FileIO.mode.detail), filename);
            }catch (Exception ex){return View();}
        }
        // Send file stream for download
        private ActionResult SendFile(string ClaimGUID, int? claimDetailId, FileIO.mode fMode, string filename)
        {
            try
            {
                string filePath = FileIO.GetClaimFilePath(ClaimGUID, claimDetailId, fMode, filename, true);

                if (System.IO.File.Exists(AppDomain.CurrentDomain.BaseDirectory + filePath))
                    /*System.IO.Path.GetFileName(filePath)*/
                    return File("~/" + filePath, "Content-Disposition: attachment;", filename);                    
                else/*Invalid or deleted file (from Log)*/
                { ViewData["Message"] = "File not found"; return View("DataNotFound"); }
                
            }
            catch (Exception ex) { return View(); }
        }

        /// <summary>
        /// Decode querystring for file download link
        /// </summary>
        /// <param name="code">string to be decoded</param>
        /// <returns>array of string</returns>
        private string[] DecodeQSforFile(string code)
        {
            if (string.IsNullOrEmpty(code)) return new string[] { };

            //code = HttpUtility.UrlDecode(HttpUtility.UrlDecode(code)); // decode URL (first is done by us and second by browser
            code = HttpUtility.UrlDecode(code); // Decoding twice creates issue for certain codes
            return Crypto.EncodeStr(code, false).Split(new char[] { FileHeader.sep[0] });
        }
                        
        #endregion

    }
}
