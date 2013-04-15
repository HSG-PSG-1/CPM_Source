using System;
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
       public ActionResult CommentsKO1()
        {
            //InitializeViewBag("Better list");
            var model = new CommentKOModel
            {
                 CommentToAdd = new Comment() { ClaimID=-1, ClaimGUID="TEMP01"},
                AllComments = new List<Comment> { 
                new Comment() { ClaimID=-1, ClaimGUID="TEMP01", CommentBy = "Hemant", ID = 1, LastModifiedBy = 1, LastModifiedDate = DateTime.Today, Comment1 = "First comment", Archived= false },
                new Comment() { ClaimID=-1, ClaimGUID="TEMP02", CommentBy = "Hemant", ID = 1, LastModifiedBy = 1, LastModifiedDate = DateTime.Today, Comment1 = "Second comment", Archived= false },
                new Comment(){ ClaimID=-1, ClaimGUID="TEMP03", CommentBy = "Hemant", ID = 1, LastModifiedBy = 1, LastModifiedDate = DateTime.Today, Comment1 = "Third comment", Archived= false },
                }
            };
            return View(model);
        }

        public ActionResult AddItem(CommentKOModel model)
        {
            model.AddComment();
            return Json(model);
        }

        public ActionResult RemoveSelected(CommentKOModel model)
        {
            model.RemoveSelected(model.CommentToRemove);
            return Json(model);
        }

        #region Actions for Comments - Secure
                
        //Comments List (Claim\1\Comments) & Edit (Claim\1\Comments\2)
        [CacheControl(HttpCacheability.NoCache), HttpGet]
        public ActionResult CommentsKO(int ClaimID, string ClaimGUID, int? CommentID) // PartialViewResultViewResultBase 
        {
            if (_Session.IsOnlyCustomer) return PartialView();//Customer doesn't have access to Comments

            //Set Comment object
            Claim claimObj = _Session.Claims[ClaimGUID];
            Comment newObj = new Comment();

            if (TempData["PRGModel"] != null)
                new CPM.Models.PRGModel(TempData["PRGModel"]).ExtractData<Comment>(ref newObj, ModelState);
            //if (TempData["ViewData"] != null)ViewData = (ViewDataDictionary)TempData["ViewData"]; //SPECIAL: required for when redirected from Invalid Add attempt
            else
                newObj = new CAWcomment(IsAsync).GetCommentById(CommentID, claimObj);

            DAL.CommentKOModel vm = new CommentKOModel()
            {
                CommentToAdd = newObj,
                AllComments = new CAWcomment(false).Search(ClaimID, null, ClaimGUID)
            };

            ViewData["commentObj"] = newObj;
            //ViewData["Users"] = new LookupService().GetLookup(LookupService.Source.User);
            ViewData["claimObj"] = claimObj; // For AssignTo & AssignToVal (in future for more properties)

            return View(vm); 
            /*
            PartialView("~/Views/Claim/EditorTemplates/Comments.cshtml",
                 new CAWcomment(IsAsync).Search(ClaimID, null, ClaimGUID));//.Cast<Comment>()); - NOT needed 
            */
        }

        [CacheControl(HttpCacheability.NoCache), HttpGet]
        public JsonResult CommentsKOVM(int ClaimID, string ClaimGUID, int? CommentID) // PartialViewResultViewResultBase 
        {
            if (_Session.IsOnlyCustomer) return Json(null);//Customer doesn't have access to Comments

            //Set Comment object
            Claim claimObj = _Session.Claims[ClaimGUID];
            Comment newObj = new Comment();

            if (TempData["PRGModel"] != null)
                new CPM.Models.PRGModel(TempData["PRGModel"]).ExtractData<Comment>(ref newObj, ModelState);
            //if (TempData["ViewData"] != null)ViewData = (ViewDataDictionary)TempData["ViewData"]; //SPECIAL: required for when redirected from Invalid Add attempt
            else
                newObj = new CAWcomment(IsAsync).GetCommentById(CommentID, claimObj);

            //if (newObj != null && string.IsNullOrEmpty(newObj.Comment1)) newObj.Comment1 = "";
            DAL.CommentKOModel vm = new CommentKOModel()
            {
                CommentToAdd = newObj,
                AllComments = new CAWcomment(false).Search(ClaimID, null, ClaimGUID)
            };

            return Json(vm, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult CommentKODelete(int ClaimID, string ClaimGUID, int CommentID)
        {
           // new CAWcomment(false).Delete(new Comment() { ID = CommentID, ClaimID = ClaimID, ClaimGUID = ClaimGUID });
            //Taconite XML
            return this.Content(Defaults.getTaconite(true, Defaults.getOprResult(true, ""), "cmtOprMsg"), "text/xml");
        }

        [HttpPost]
        public ActionResult CommentsKO(/*int ClaimID,*/ [FromJson] IEnumerable<Comment> comments)
        {
            /*
            ViewData["commentObj"] = CommentObj;

            if (string.IsNullOrEmpty(CommentObj.Comment1))//because Comment1 is added by us to prevent conflict with the comment class
                ModelState.AddModelError("Comment1", "Comment required");
            */
            #region Process based on ModelState
            /*if (ModelState.IsValid)
            {
                bool changeAssignTo = (Request.Form["AssignTo"] != Request.Form["AssignToOLD"]);
                // Add new comment and also send flag to indicate if AssignTo was changed
                new CAWcomment(IsAsync).AddEdit(CommentObj, changeAssignTo,
                    int.Parse(Request.Form["AssignTo"]), Request.Form["AssignToVal"]);
                //Don, return to default action
                return RedirectToAction("Comments", new { ClaimGUID = CommentObj.ClaimGUID });
            }
            else
            {
                //http://stackoverflow.com/questions/279665/how-can-i-maintain-modelstate-with-redirecttoaction
                //TempData["ViewData"] = ViewData;//Store in temp intermediate variable
                TempData["PRGModel"] = new CPM.Models.PRGModel().SetPRGModel<Comment>(CommentObj, ModelState);
                return RedirectToAction("Comments", new { ClaimGUID = CommentObj.ClaimGUID });
            }*/
            #endregion

            return View(new CommentKOModel());
        }

        #endregion
    }
}

namespace CPM.DAL
{
    public class CommentKOModel
    {
        public Comment CommentToAdd { get; set; }
        public Comment CommentToRemove { get; set; }
        public List<Comment> AllComments { get; set; }        

        public void AddComment()
        {
            if (CommentToAdd != null && !AllComments.Contains(CommentToAdd))
                AllComments.Add(CommentToAdd);
            CommentToAdd = null;
        }

        public void RemoveSelected(Comment cmtObj)
        {
            AllComments.Remove(cmtObj);            
        }
    }
}