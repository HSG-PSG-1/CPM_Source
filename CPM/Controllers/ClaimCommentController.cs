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
    //[CompressFilter] - don't use it here
    [IsAuthorize(IsAuthorizeAttribute.Rights.NONE)]//Special case for some dirty session-abandoned pages and hacks
    public partial class ClaimController : BaseController
    {
        #region Actions for Comments - Secure
        
        [AccessClaim("ClaimID")]
        public ActionResult CommentsPg(int ClaimID, string ClaimGUID, int? CommentID)
        {//For separate Comments page
            if (_Session.IsOnlyCustomer) return RedirectToAction("Manage", new { ClaimGUID = ClaimGUID });//Customer doesn't have access to Comments
            else
            {
                ViewData["ClaimGUID"] = ClaimGUID; // Need to pass GUID
                return View();
            }
        }
        
        //Comments List (Claim\1\Comments) & Edit (Claim\1\Comments\2)
        [CacheControl(HttpCacheability.NoCache), HttpGet]
        public PartialViewResult Comments(int ClaimID, string ClaimGUID, int? CommentID) // ActionResult ViewResultBase 
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
            
            ViewData["commentObj"] = newObj;
            ViewData["Users"] = new LookupService().GetLookup(LookupService.Source.User);
            ViewData["claimObj"] = claimObj; // For AssignTo & AssignToVal (in future for more properties)

            return PartialView("~/Views/Claim/EditorTemplates/Comments.cshtml", 
                new CAWcomment(IsAsync).Search(ClaimID, null, ClaimGUID));//.Cast<Comment>()); - NOT needed
        }

        [HttpPost]
        public ActionResult CommentDelete(int ClaimID, string ClaimGUID, int CommentID)
        {
            new CAWcomment(IsAsync).Delete(new Comment() { ID = CommentID, ClaimID = ClaimID, ClaimGUID = ClaimGUID });
            //Taconite XML
            return this.Content(Defaults.getTaconite(true, Defaults.getOprResult(true, ""), "cmtOprMsg"), "text/xml");
        }
        
        [HttpPost]
        public ActionResult Comments(int ClaimID, Comment CommentObj)
        {
            ViewData["commentObj"] = CommentObj;

            if (string.IsNullOrEmpty(CommentObj.Comment1))//because Comment1 is added by us to prevent conflict with the comment class
                ModelState.AddModelError("Comment1", "Comment required");

            #region Process based on ModelState
            if (ModelState.IsValid)
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
            }
            #endregion
        }

        #endregion
    }
}
