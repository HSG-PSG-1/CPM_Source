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
            ViewData["ClaimGUID"] = System.Guid.NewGuid();
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
            #region Process based on ModelState
            /*if (ModelState.IsValid)
            {
                bool changeAssignTo = (Request.Form["AssignTo"] != Request.Form["AssignToOLD"]);
                // Add new file and also send flag to indicate if AssignTo was changed
                new CAWfile(IsAsync).AddEdit(FileObj, changeAssignTo,
                    int.Parse(Request.Form["AssignTo"]), Request.Form["AssignToVal"]);
                //Don, return to default action
                return RedirectToAction("Files", new { ClaimGUID = FileObj.ClaimGUID });
            }
            else
            {
                //http://stackoverflow.com/questions/279665/how-can-i-maintain-modelstate-with-redirecttoaction
                //TempData["ViewData"] = ViewData;//Store in temp intermediate variable
                TempData["PRGModel"] = new CPM.Models.PRGModel().SetPRGModel<File>(FileObj, ModelState);
                return RedirectToAction("Files", new { ClaimGUID = FileObj.ClaimGUID });
            }*/
            #endregion

            List<FileHeader> fileList = files.ToList();


            fileList.Add(new FileHeader()
            { Comment = "I came from postback refresh! (to confirm a successful postback)", UploadedBy = "Server postback" });

            Session["Files_Demo"] = fileList;

            return View();// RedirectToAction("FilesKO");//new FileKOModel()
        }

        [HttpPost]
        public ActionResult FilePostKO(int ClaimID, string ClaimGUID, FileHeader fh)
        { 
            HttpPostedFileBase hpFile = Request.Files["FileNameNEW"];
            bool success = true;
            string result = "Uploaded " + hpFile.FileName + "("+ hpFile.ContentLength + ")";

            //Taconite XML
            return this.Content(Defaults.getTaconite(success,
                Defaults.getOprResult(success, "Unable to upload file"), "fileOprMsg"), "text/xml");
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
            #region Process based on ModelState
            /*if (ModelState.IsValid)
            {
                bool changeAssignTo = (Request.Form["AssignTo"] != Request.Form["AssignToOLD"]);
                // Add new file and also send flag to indicate if AssignTo was changed
                new CAWfile(IsAsync).AddEdit(FileObj, changeAssignTo,
                    int.Parse(Request.Form["AssignTo"]), Request.Form["AssignToVal"]);
                //Don, return to default action
                return RedirectToAction("Files", new { ClaimGUID = FileObj.ClaimGUID });
            }
            else
            {
                //http://stackoverflow.com/questions/279665/how-can-i-maintain-modelstate-with-redirecttoaction
                //TempData["ViewData"] = ViewData;//Store in temp intermediate variable
                TempData["PRGModel"] = new CPM.Models.PRGModel().SetPRGModel<File>(FileObj, ModelState);
                return RedirectToAction("Files", new { ClaimGUID = FileObj.ClaimGUID });
            }*/
            #endregion

            List<FileDetail> fileList = files.ToList();


            fileList.Add(new FileDetail() { Comment = "I came from postback refresh! (to confirm a successful postback)", UploadedBy = "Server postback" });

            Session["FileDetails_Demo"] = fileList;

            return View();// RedirectToAction("FilesKO");//new FileKOModel()
        }

        [HttpPost]
        public ActionResult FileDetailPostKO(int ClaimID, string ClaimGUID, FileHeader fh)
        {
            HttpPostedFileBase hpFile = Request.Files["FileDetailNameNEW"];
            bool success = true;
            string result = "Uploaded " + hpFile.FileName + "(" + hpFile.ContentLength + ")";

            //Taconite XML
            return this.Content(Defaults.getTaconite(success,
                Defaults.getOprResult(success, "Unable to upload file"), "fileDetailOprMsg"), "text/xml");
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