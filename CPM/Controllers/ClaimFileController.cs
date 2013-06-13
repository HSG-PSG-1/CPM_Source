using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CPM.DAL;
using CPM.Services;
using CPM.Helper;

namespace CPM.Controllers
{ // http://knockoutmvc.com/Home/QuickStart

    //[CompressFilter] - don't use it here
    //[IsAuthorize(IsAuthorizeAttribute.Rights.NONE)]//Special case for some dirty session-abandoned pages and hacks
    public partial class ClaimController : BaseController
    {
        bool IsAsync
        {
            get { return true; }
            /* until we upgrade code in future for Sync mode where changes will take place on the go instead of waiting until final commit */
            set { ;}
        }

        FileIO.mode HeaderFM { get { return (IsAsync ? FileIO.mode.asyncHeader : FileIO.mode.header); } }
        FileIO.mode DetailFM { get { return (IsAsync ? FileIO.mode.asyncDetail : FileIO.mode.detail); } }

        #region File Header Actions

        //Files List (Claim\1\Files) & Edit (Claim\1\Files\2)
        [CacheControl(HttpCacheability.NoCache), HttpGet]
        public ActionResult Files(int ClaimID, string ClaimGUID) // PartialViewResultViewResultBase 
        {
            ViewData["Archived"] = false;
            ViewData["ClaimGUID"] = ClaimGUID;
            return View();
        }

        [CacheControl(HttpCacheability.NoCache), HttpGet]
        public JsonResult FilesKOVM(int ClaimID, string ClaimGUID, int? FileID)
        {
            //Set File object
            FileHeader newObj = new FileHeader() { ID = -1, _Added = true, ClaimID = ClaimID, ClaimGUID = ClaimGUID,
                UploadedBy = _SessionUsr.Email, LastModifiedBy = _SessionUsr.ID, LastModifiedDate = DateTime.Now, UploadedOn = DateTime.Now,
                UserID = _SessionUsr.ID, Archived = false, FileName="", FileNameNEW="" };

            List<FileHeader> files = new List<FileHeader>();
            DAL.FileKOModel vm = new FileKOModel()
            {
                FileToAdd = newObj, EmptyFileHeader = newObj,
                AllFiles = (new FileHeaderService().Search(ClaimID, null))
            };
            // Lookup data
            vm.FileTypes = new LookupService().GetLookup(LookupService.Source.FileHeader);

            return Json(vm, JsonRequestBehavior.AllowGet);
        }
                
        [HttpPost]
        [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")] //SO: 2570051/error-returning-ajax-in-ie7
        public ActionResult FilePostKO(int ClaimID, string ClaimGUID, FileHeader FileHdrObj)
        { 
            HttpPostedFileBase hpFile = Request.Files["FileNameNEW"];
            bool success = true;
            string result = "";// "Uploaded " + hpFile.FileName + "(" + hpFile.ContentLength + ")";

            #region New file upload

            if ((FileHdrObj.FileNameNEW ?? FileHdrObj.FileName) != null)
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
                success = (ModelState["FileName"].Errors.Count() < 1);
            }

            #endregion
            result = !success ? ("Unable to upload file - " + ModelState["FileName"].Errors[0].ErrorMessage) : "";

            //Taconite XML
            return this.Content(Defaults.getTaconiteResult(success,
                Defaults.getOprResult(success, result), "fileOprMsg",
                "fileUploadResponse('" + FileHdrObj.CodeStr + "'," + success.ToString().ToLower() + "," + FileHdrObj.ID + ")"), "text/xml");
        }

        [SkipModelValidation]
        [AccessClaim("ClaimID")]
        [HttpPost]
        public ActionResult FileHeaderKODelete(int ClaimID, string ClaimGUID,[FromJson] FileHeader delFH)
        {//Call this ONLY when you need to actually delete the file
            bool proceed = false;
            if (delFH != null)
            {
                #region Delete File

                //If its Async - we can delete the TEMP file, if its sync the file is not present in TEMP folder so delete is not effective
                // HT: infer: send async because the file resides in the temp folder
                if (FileIO.DeleteClaimFile(delFH.FileName, ClaimGUID, null, FileIO.mode.asyncHeader))
                {
                    //HT: INFER: Delete file for Async, Sync and (existing for Async - 
                    //the above delete will cause no effect coz path is diff)
                    //new CAWFile(IsAsync).Delete(new FileHeader() { ID = FileHeaderID, ClaimGUID = ClaimGUID });
                    proceed = true;
                }
                else
                    proceed = false;

                #endregion
            }
            //Taconite XML
            return this.Content(Defaults.getTaconite(proceed,
                Defaults.getOprResult(proceed, "Unable to delete file"), "fileOprMsg"), "text/xml");
        }

        #endregion

        #region File Detail Actions

        [CacheControl(HttpCacheability.NoCache), HttpGet]
        public ActionResult FilesDetailArchived(int ClaimID, int ClaimDetailID, string ClaimGUID) // PartialViewResultViewResultBase 
        {
            ViewData["Archived"] = true;
            ViewData["ClaimDetailID"] = ClaimDetailID;
            ViewData["ClaimGUID"] = ClaimGUID;
            return View(new FileDetailService().Search(ClaimID, ClaimDetailID));
        }        

        //Files List (Claim\1\Files) & Edit (Claim\1\Files\2)
        [CacheControl(HttpCacheability.NoCache), HttpGet]
        public ActionResult FilesDetail(int ClaimID, int ClaimDetailID, string ClaimGUID) // PartialViewResultViewResultBase 
        {
            ViewData["Archived"] = false;
            ViewData["ClaimDetailID"] = ClaimDetailID;
            ViewData["ClaimGUID"] = ClaimGUID;
            return View();
        }

        [CacheControl(HttpCacheability.NoCache), HttpGet]
        public JsonResult FilesDetailKOVM(int ClaimID, int ClaimDetailID, string ClaimGUID)
        {
            //Set File object
            FileDetail newObj = new FileDetail()
            {
                ID = -1,
                _Added = true,
                ClaimID = ClaimID,
                ClaimDetailID = ClaimDetailID,
                ClaimGUID = ClaimGUID,
                UploadedBy = _SessionUsr.Email,
                LastModifiedBy = _SessionUsr.ID,
                LastModifiedDate = DateTime.Now,
                UploadedOn = DateTime.Now,
                UserID = _SessionUsr.ID,
                Archived = false,
                FileName = "",
                FileNameNEW = ""
            };

            DAL.FileDetailKOModel vm = new FileDetailKOModel()
            {
                FileDetailToAdd = newObj, EmptyFileDetail = newObj,
                AllFiles = new FileDetailService().Search(ClaimID, ClaimDetailID)
            };
            // Lookup data
            vm.FileDetailTypes = new LookupService().GetLookup(LookupService.Source.FileDetail);

            return Json(vm, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult FilesDetail(int ClaimID, [FromJson] IEnumerable<FileDetail> files) // IEnumerable
        {
            List<FileDetail> fileList = files.ToList();

            fileList.Add(new FileDetail() { Comment = "I came from postback refresh! (to confirm a successful postback)", UploadedBy = "Server postback" });

            Session["FileDetails_Demo"] = fileList;

            return View();// RedirectToAction("Files");//new FileKOModel()
        }

        [HttpPost]
        [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")] //SO: 2570051/error-returning-ajax-in-ie7
        public ActionResult FileDetailPostKO(int ClaimID, int ClaimDetailD, string ClaimGUID, FileDetail FileDetailObj)
        {
            HttpPostedFileBase hpFile = Request.Files["FileDetailNameNEW"];
            bool success = true;
            string result = "";// "Uploaded " + hpFile.FileName + "(" + hpFile.ContentLength + ")";

            #region New file upload

            if ((FileDetailObj.FileNameNEW ?? FileDetailObj.FileName) != null)
            {//HT Delete old\existing file? For Async need to wait until final commit
                //HT:IMP: Set Async so that now the file maps to Async file-path
                FileDetailObj.IsAsync = true;
                //FileDetailObj.ClaimGUID = _Session.Claim.ClaimGUID; // to be used further
                FileDetailObj.FileName = ChkAndSaveClaimFile("FileDetailNameNEW", ClaimID, DetailFM, FileDetailObj.ClaimGUID, ClaimDetailD);
                success = (ModelState["FileName"].Errors.Count() < 1);
            }

            #endregion

            result = !success ? ("Unable to upload file - " + ModelState["FileName"].Errors[0].ErrorMessage) : "";

            //Taconite XML
            return this.Content(Defaults.getTaconiteResult(success,
                Defaults.getOprResult(success, result), "fileDetailOprMsg",
                "fileDUploadResponse('" + FileDetailObj.CodeStr + "'," + success.ToString().ToLower() + "," + FileDetailObj.ID + ")"), "text/xml");
        }

        [SkipModelValidation]
        [AccessClaim("ClaimID")]
        [HttpPost]
        public ActionResult FileDetailKODelete(int ClaimID, string ClaimGUID, [FromJson] FileDetail delFD)
        {//Call this ONLY when you need to actually delete the file
            bool proceed = false;
            if (delFD != null)
            {
                #region Delete File

                //If its Async - we can delete the TEMP file, if its sync the file is not present in TEMP folder so delete is not effective
                // HT: infer: send async because the file resides in the temp folder
                if (FileIO.DeleteClaimFile(delFD.FileName, ClaimGUID, delFD.ClaimDetailID, FileIO.mode.asyncDetail))
                {
                    //HT: INFER: Delete file for Async, Sync and (existing for Async - 
                    //the above delete will cause no effect coz path is diff)
                    //new CAWFile(IsAsync).Delete(new FileHeader() { ID = FileHeaderID, ClaimGUID = ClaimGUID });
                    proceed = true;
                }
                else
                    proceed = false;

                #endregion
            }
            //Taconite XML
            return this.Content(Defaults.getTaconite(proceed,
                Defaults.getOprResult(proceed, "Unable to delete file"), "fileDetailOprMsg"), "text/xml");
        }
        
        #endregion

        #region Extra Actions and functions to get code for file download

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

        //Get Header File
        [ValidateInput(false)] // SO: 2673850/validaterequest-false-doesnt-work-in-asp-net-4
        public ActionResult GetFile()
        {
            try
            {
                string code = "";
                try { code = Request.QueryString.ToString(); }
                catch (HttpRequestValidationException httpEx)
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
            }
            catch (Exception ex) { return View(); }
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

namespace CPM.DAL
{
    public class FileKOModel
    {
        public FileHeader EmptyFileHeader { get; set; }
        public FileHeader FileToAdd { get; set; }
        public List<FileHeader> AllFiles { get; set; }
        public IEnumerable FileTypes { get; set; }
    }

    public class FileDetailKOModel
    {
        public FileDetail EmptyFileDetail { get; set; }
        public FileDetail FileDetailToAdd { get; set; }
        public List<FileDetail> AllFiles { get; set; }
        public IEnumerable FileDetailTypes { get; set; }
    }
}