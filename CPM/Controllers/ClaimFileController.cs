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
        #region File Header Actions
                
        [CacheControl(HttpCacheability.NoCache), HttpGet]
        public FileKOModel GetFileKOModel(int ClaimID, string ClaimGUID)
        {
            //Set File object
            FileHeader newObj = new FileHeader() { ID = -1, _Added = true, ClaimID = ClaimID, ClaimGUID = ClaimGUID,
                UploadedBy = _SessionUsr.Email, LastModifiedBy = _SessionUsr.ID, LastModifiedDate = DateTime.Now, UploadedOn = DateTime.Now,
                UserID = _SessionUsr.ID, Archived = false, FileName="", FileNameNEW="" };

            List<FileHeader> files = new List<FileHeader>();
            FileKOModel vm = new FileKOModel()
            {
                FileToAdd = newObj, EmptyFileHeader = newObj,
                AllFiles = (new FileHeaderService().Search(ClaimID, null))
            };
            // Lookup data
            vm.FileTypes = new LookupService().GetLookup(LookupService.Source.FileHeader);

            return vm;
        }
                
        [HttpPost]
        [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")] //SO: 2570051/error-returning-ajax-in-ie7
        public ActionResult FilePostKO(int ClaimID, /* string ClaimGUID */ FileHeader FileHdrObj)
        { 
            HttpPostedFileBase hpFile = Request.Files["FileNameNEW"];
            bool success = true;
            string result = "";// "Uploaded " + hpFile.FileName + "(" + hpFile.ContentLength + ")";

            #region New file upload

            if ((FileHdrObj.FileNameNEW ?? FileHdrObj.FileName) != null)
            {
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
                FileHdrObj.FileName = System.IO.Path.GetFileName(FileHdrObj.FileName); // Ensure its file name and not path!
                ChkAndSaveClaimFile("FileNameNEW", ClaimID, FileHdrObj.ClaimGUID);
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
                proceed = FileIO.DeleteClaimFile(ClaimID, ClaimGUID, delFH.FileName);
            
            //Taconite XML
            return this.Content(Defaults.getTaconiteRemoveTR(proceed,
                Defaults.getOprResult(proceed, "Unable to delete file"), "fileOprMsg"), "text/xml");
        }

        [SkipModelValidation]
        [HttpPost]
        public ActionResult Upload(int ClaimID, HttpPostedFileBase file, [FromJson] FileHeader FileHdrObj)
        {
            //string savePath = Server.MapPath(@"~\Content\" + fileUp.FileName); fileUp.SaveAs(savePath);

            //HttpPostedFileBase fileUp = Request.Files["FileNameNEW"];
            bool success = true;
            string result = "";// "Uploaded " + hpFile.FileName + "(" + hpFile.ContentLength + ")";

            #region New file upload

            if ((FileHdrObj.FileNameNEW ?? FileHdrObj.FileName) != null)
            {
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
                FileHdrObj.FileName = System.IO.Path.GetFileName(FileHdrObj.FileName); // Ensure its file name and not path!
                ChkAndSaveClaimFile("FileNameNEW", ClaimID, FileHdrObj.ClaimGUID);
                success = ((ModelState["FileName"]?? new ModelState()).Errors.Count() < 1); // We won't have (initially) - ModelState["FileName"]
            }

            #endregion
            result = !success ? ("Unable to upload file - " + ModelState["FileName"].Errors[0].ErrorMessage) : "";

            string sepr = "~~~";
            return Content((success ? "1" : "0") + sepr + Defaults.getOprResult(success, result) + sepr + FileHdrObj.ID + sepr + FileHdrObj.CodeStr); 
            //Url.Content(@"~\Content\" + fileUp.FileName));
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
                ChkAndSaveClaimFile("FileDetailNameNEW", ClaimID, ClaimGUID, ClaimDetailD);
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
                if (FileIO.DeleteClaimFile(ClaimID, ClaimGUID, delFD.FileName, delFD.ClaimDetailID))
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
            return this.Content(Defaults.getTaconiteRemoveTR(proceed,
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
        void ChkAndSaveClaimFile(string hpFileKey, int ClaimId, string ClaimGUID, int? ClaimDetailId = null)
        {
            HttpPostedFileBase hpFile = Request.Files[hpFileKey];

            FileIO.result uploadResult = FileIO.UploadAndSave(hpFile, ClaimId, ClaimGUID, ClaimDetailId);

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
                { code = Request.RawUrl.Split(new string[] { "GetFile?" }, StringSplitOptions.RemoveEmptyEntries)[1]; }//SPECIAL CASE for some odd codes!

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
                int ClaimID = int.Parse(data[1]);
                string ClaimGUID = (ClaimID > 0 && data.Length < 3)? "" : data[2];//This must parse correctly (if ID > 0 means existingso no need for GUID)
                
                //Send file stream for download
                return SendFile(ClaimID,ClaimGUID, filename);
            }
            catch (Exception ex) { ViewData["Message"] = "File not found"; return View("DataNotFound"); }
        }
        //Get Detail File
        [ValidateInput(false)] // SO: 2673850/validaterequest-false-doesnt-work-in-asp-net-4
        public ActionResult GetFileD(int ClaimID)
        {
            try
            {
                string code = "";
                try { code = Request.QueryString.ToString(); }
                catch (HttpRequestValidationException httpEx)
                { code = Request.RawUrl.Split(new string[] { "GetFileD?" }, StringSplitOptions.RemoveEmptyEntries)[1]; }//SPECIAL CASE for some odd codes!

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
                int ClaimDetailID = int.Parse(data[1]);//int ClaimID = int.Parse(data[1]); 
                string ClaimGUID = (ClaimID > 0 && data.Length < 3)? "" : data[2];//This must parse correctly (if ID > 0 means existingso no need for GUID)

                //Send file stream for download
                return SendFile(ClaimID,ClaimGUID, filename, ClaimDetailID);
            }
            catch (Exception ex) { return View(); }
        }
        // Send file stream for download
        private ActionResult SendFile(int claimID, string claimGUID, string filename, int? claimDetailId = null)
        {
            try
            {
                string filePath = FileIO.GetClaimFilePath(claimID, claimGUID, filename, claimDetailId, false);

                if (System.IO.File.Exists(filePath))//AppDomain.CurrentDomain.BaseDirectory 
                    /*System.IO.Path.GetFileName(filePath)//return File("~/" + filePath, "Content-Disposition: attachment;", filename);*/
                    return File(filePath, "Content-Disposition: attachment;", filename);
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
            // IMP: Make sure to encode & decode URL otherwise browser will try to do it and might parse wrong
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