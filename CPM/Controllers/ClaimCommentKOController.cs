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

        public CommentKOModel GetCommentKOModel(int ClaimID, string ClaimGUID, int AssignedTo)
        {        
            //Set Comment object
            Comment newObj = new Comment() { ID = -1, _Added = true, ClaimID = ClaimID, ClaimGUID = ClaimGUID, CommentBy = _SessionUsr.Email, LastModifiedBy = _SessionUsr.ID, LastModifiedDate = DateTime.Now, PostedOn = DateTime.Now, UserID = _SessionUsr.ID, Archived = false };

            DAL.CommentKOModel vm = new CommentKOModel()
            {
                CommentToAdd = newObj, EmptyComment = newObj, 
                AllComments = new CommentService().Search(ClaimID, null),
                AssignedTo = AssignedTo
            };

            vm.Users = new LookupService().GetLookup(LookupService.Source.User);

            return vm;
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
        {            
            bool sendMail = (ClaimID > Defaults.Integer && AssignedTo != _SessionUsr.ID);// No need to send mail if its current user
            string msg = sendMail ? "Email queued for new comment" : "Self notification : No email queued";
            try
            {
                #region Check and send email
                if (sendMail)
                {// No need to send mail if its current user
                    string UserEmail = new UserService().GetUserEmailByID(AssignedTo);
                    sendMail = MailManager.AssignToMail(ClaimNo.ToString(), CommentObj.Comment1, ClaimID, UserEmail, (_SessionUsr.UserName), true);
                }
                #endregion
            }
            catch (Exception ex) { sendMail = false; msg = ex.Message; }
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