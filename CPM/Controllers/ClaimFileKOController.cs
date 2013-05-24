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
        //Files List (Claim\1\Files) & Edit (Claim\1\Files\2)
        [CacheControl(HttpCacheability.NoCache), HttpGet]
        public ActionResult FilesKO(int ClaimID, string ClaimGUID) // PartialViewResultViewResultBase 
        {
            ViewData["Archived"] = false;
            ViewData["ClaimGUID"] = ClaimGUID;
            return View();
        }

        [CacheControl(HttpCacheability.NoCache), HttpGet]
        public JsonResult FilesKOVM(int ClaimID, string ClaimGUID, int? FileID) // PartialViewResultViewResultBase 
        {
            //Set File object
            FileHeader newObj = new FileHeader() { ID = -1, _Added = true, ClaimID = ClaimID, ClaimGUID = ClaimGUID,
                UploadedBy = _SessionUsr.Email, LastModifiedBy = _SessionUsr.ID, LastModifiedDate = DateTime.Now, UploadedOn = DateTime.Now,
                UserID = _SessionUsr.ID, Archived = false, FileName="", FileNameNEW="" };

            List<FileHeader> files = new List<FileHeader>();
            try { files = ((List<FileHeader>)Session["Files_Demo"]); }
            catch (Exception ex) { files = null; }
            bool sendResult = (files != null && files.Count() > 0);
            if (sendResult) 
                Session.Remove("Files_Demo");

            //if (newObj != null && string.IsNullOrEmpty(newObj.File1)) newObj.File1 = "";
            DAL.FileKOModel vm = new FileKOModel()
            {
                FileToAdd = newObj, EmptyFileHeader = newObj,
                AllFiles = (sendResult? files : new CAWFile(false).Search(ClaimID, null, ClaimGUID))
            };
            // Lookup data
            vm.FileTypes = new LookupService().GetLookup(LookupService.Source.FileHeader);

            return Json(vm, JsonRequestBehavior.AllowGet);
        }
        
        [HttpPost]
        public ActionResult FilesKO(int ClaimID, [FromJson] IEnumerable<FileHeader> files) // IEnumerable
        {
            List<FileHeader> fileList = files.ToList();

            fileList.Add(new FileHeader()
            { Comment = "I came from postback refresh! (to confirm a successful postback)", UploadedBy = "Server postback" });

            Session["Files_Demo"] = fileList;

            return View();// RedirectToAction("FilesKO");//new FileKOModel()
        }

        [HttpPost]
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
        //Files List (Claim\1\Files) & Edit (Claim\1\Files\2)
        [CacheControl(HttpCacheability.NoCache), HttpGet]
        public ActionResult FilesDetailKO(int ClaimID, int ClaimDetailID, string ClaimGUID) // PartialViewResultViewResultBase 
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

            List<FileDetail> files = new List<FileDetail>();
            try { files = ((List<FileDetail>)Session["FilesDetail_Demo"]); }
            catch (Exception ex) { files = null; }
            bool sendResult = (files != null && files.Count() > 0);
            if (sendResult)
                Session.Remove("FilesDetail_Demo");

            //if (newObj != null && string.IsNullOrEmpty(newObj.File1)) newObj.File1 = "";
            DAL.FileDetailKOModel vm = new FileDetailKOModel()
            {
                FileDetailToAdd = newObj, EmptyFileDetail = newObj,
                AllFiles = (sendResult ? files : new CAWdFile(false).Search(ClaimID, ClaimDetailID, ClaimGUID))
            };
            // Lookup data
            vm.FileDetailTypes = new LookupService().GetLookup(LookupService.Source.FileDetail);

            return Json(vm, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult FilesDetailKO(int ClaimID, [FromJson] IEnumerable<FileDetail> files) // IEnumerable
        {
            List<FileDetail> fileList = files.ToList();

            fileList.Add(new FileDetail() { Comment = "I came from postback refresh! (to confirm a successful postback)", UploadedBy = "Server postback" });

            Session["FileDetails_Demo"] = fileList;

            return View();// RedirectToAction("FilesKO");//new FileKOModel()
        }

        [HttpPost]
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
                "fileDUploadResponse('" + FileDetailObj.FileName + "'," + success.ToString().ToLower() + "," + FileDetailObj.ID + ")"), "text/xml");
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
                if (FileIO.DeleteClaimFile(delFD.FileName, ClaimGUID, delFD.ClaimDetailID, FileIO.mode.asyncHeader))
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