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
        public ActionResult Comments(int ClaimID, string ClaimGUID) // PartialViewResultViewResultBase 
        {
            ViewData["ClaimGUID"] = ClaimGUID;
            return View();            
        }

        [CacheControl(HttpCacheability.NoCache), HttpGet]
        public JsonResult CommentsKOVM(int ClaimID, string ClaimGUID, int AssignedTo) // PartialViewResultViewResultBase 
        {
            if (_Session.IsOnlyCustomer) 
                return Json(null);//Customer doesn't have access to Comments

            //Set Comment object
            Comment newObj = new Comment() { ID = -1, _Added = true, ClaimID = ClaimID, ClaimGUID = ClaimGUID, CommentBy = _SessionUsr.Email, LastModifiedBy = _SessionUsr.ID, LastModifiedDate = DateTime.Now, PostedOn = DateTime.Now, UserID = _SessionUsr.ID, Archived = false };

            #region Kept for testing
            /*
            List<Comment> comments = new List<Comment>();
            try { comments = ((List<Comment>)Session["Comments_Demo"]); }
            catch (Exception ex) { comments = null; }
            bool sendResult = (comments != null && comments.Count() > 0);
            if (sendResult) 
                Session.Remove("Comments_Demo");
            */
            #endregion

            DAL.CommentKOModel vm = new CommentKOModel()
            {
                CommentToAdd = newObj, EmptyComment = newObj, 
                //AllComments = (sendResult? comments : new CAWcomment(false).Search(ClaimID, null, ClaimGUID)),
                AllComments = new CommentService().Search(ClaimID, null),//(new CAWcomment(false).Search(ClaimID, null, ClaimGUID)),
                AssignedTo = AssignedTo
            };

            vm.Users = new LookupService().GetLookup(LookupService.Source.User);

            return Json(vm, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        //HT: Kept for testing
        public ActionResult Comments(int? ClaimID, [FromJson] IEnumerable<Comment> comments) // IEnumerable
        {
            #region Process based on ModelState (Old kept for review / ref)
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

            return View();// RedirectToAction("Comments");//new CommentKOModel()
        }

        [HttpPost]
        public JsonResult CommentsKOEmail(int ClaimID, string ClaimGUID, int AssignedTo, int ClaimNo, [FromJson] Comment CommentObj)
        //int AssignedToOLD,
        {   /*         
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
            return Json(sendMail, JsonRequestBehavior.AllowGet); ;// RedirectToAction("Comments");//new CommentKOModel()
            */
            string msg = "Email queued for new comment";
            bool sendMail = CommentService.SendEmail(ClaimID, AssignedTo, ClaimNo.ToString(), CommentObj, ref msg);
            HttpContext.Response.Clear(); // to avoid debug email content from rendering !
            return Json(new { sendMail, msg }, JsonRequestBehavior.AllowGet);
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