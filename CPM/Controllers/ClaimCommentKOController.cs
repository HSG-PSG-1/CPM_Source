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
        //Comments List (Claim\1\Comments) & Edit (Claim\1\Comments\2)
        [CacheControl(HttpCacheability.NoCache), HttpGet]
        public ActionResult CommentsKO(int ClaimID, string ClaimGUID) // PartialViewResultViewResultBase 
        {
            if (_Session.IsOnlyCustomer) return PartialView();//Customer doesn't have access to Comments

            #region NOT required

            /*Set Comment object
            Claim claimObj = _Session.Claims[ClaimGUID];
            Comment newObj = new Comment() { _Added=true, ClaimID = ClaimID, ClaimGUID = ClaimGUID, CommentBy = _SessionUsr.Email, LastModifiedBy = _SessionUsr.ID, LastModifiedDate = DateTime.Now, PostedOn = DateTime.Now, UserID = _SessionUsr.ID, Archived = false };

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
            */
            #endregion
            ViewData["ClaimGUID"] = ClaimGUID;
            return View();
            /*
            PartialView("~/Views/Claim/EditorTemplates/Comments.cshtml",
                 new CAWcomment(IsAsync).Search(ClaimID, null, ClaimGUID));//.Cast<Comment>()); - NOT needed 
            */
        }

        [CacheControl(HttpCacheability.NoCache), HttpGet]
        public JsonResult CommentsKOVM(int ClaimID, string ClaimGUID, int AssignedTo) // PartialViewResultViewResultBase 
        {
            if (_Session.IsOnlyCustomer) return Json(null);//Customer doesn't have access to Comments

            //Set Comment object
            Comment newObj = new Comment() { ID = -1, _Added = true, ClaimID = ClaimID, ClaimGUID = ClaimGUID, CommentBy = _SessionUsr.Email, LastModifiedBy = _SessionUsr.ID, LastModifiedDate = DateTime.Now, PostedOn = DateTime.Now, UserID = _SessionUsr.ID, Archived = false };

            List<Comment> comments = new List<Comment>();
            try { comments = ((List<Comment>)Session["Comments_Demo"]); }
            catch (Exception ex) { comments = null; }
            bool sendResult = (comments != null && comments.Count() > 0);
            if (sendResult) 
                Session.Remove("Comments_Demo");

            //if (newObj != null && string.IsNullOrEmpty(newObj.Comment1)) newObj.Comment1 = "";
            DAL.CommentKOModel vm = new CommentKOModel()
            {
                CommentToAdd = newObj, EmptyComment = newObj, 
                AllComments = (sendResult? comments : new CAWcomment(false).Search(ClaimID, null, ClaimGUID)),
                AssignedTo = AssignedTo
            };

            vm.Users = new LookupService().GetLookup(LookupService.Source.User);

            return Json(vm, JsonRequestBehavior.AllowGet);
        }

        /*[HttpPost]
        public ActionResult CommentKODelete(int ClaimID, string ClaimGUID, int CommentID)
        {
           // new CAWcomment(false).Delete(new Comment() { ID = CommentID, ClaimID = ClaimID, ClaimGUID = ClaimGUID });
            //Taconite XML
            return this.Content(Defaults.getTaconite(true, Defaults.getOprResult(true, ""), "cmtOprMsg"), "text/xml");
        }*/

        [HttpPost]
        public ActionResult CommentsKO(int? ClaimID, [FromJson] IEnumerable<Comment> comments) // IEnumerable
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

            List<Comment> commentList = comments.ToList();


            commentList.Add(new Comment()
            { Comment1 = "I came from postback refresh! (to confirm a successful postback)", CommentBy = "Server postback" });

            Session["Comments_Demo"] = commentList;

            return View();// RedirectToAction("CommentsKO");//new CommentKOModel()
        }

        [HttpPost]
        public JsonResult CommentsKOEmail(int ClaimID, string ClaimGUID, int AssignedTo, int ClaimNo, [FromJson] Comment CommentObj)
        //int AssignedToOLD,
        {            
            bool sendMail = (ClaimID > Defaults.Integer && AssignedTo != _SessionUsr.ID);// No need to send mail if its current user
            try
            {
                #region Check and send email
                if (sendMail)
                {// No need to send mail if its current user
                    string UserEmail = new UserService().GetUserEmailByID(AssignedTo);
                    MailManager.AssignToMail(ClaimNo.ToString(), CommentObj.Comment1, ClaimID, UserEmail, (_SessionUsr.UserName), true);
                }
                #endregion
            }
            catch (Exception ex) { sendMail = false; }
            return Json(sendMail, JsonRequestBehavior.AllowGet); ;// RedirectToAction("CommentsKO");//new CommentKOModel()
        }        
    }
}

namespace CPM.DAL
{
    public class CommentKOModel
    {
        public Comment EmptyComment { get; set; }
        public Comment CommentToAdd { get; set; }
        public List<Comment> AllComments { get; set; }
        public IEnumerable Users { get; set; }
        public int AssignedTo { get; set; }
    }
}