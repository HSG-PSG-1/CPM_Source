using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic;
using System.Data.Linq.SqlClient;
using CPM.DAL;
using CPM.Helper;
using Webdiyer.WebControls.Mvc;

namespace CPM.Services
{
    public class CommentService : _ServiceBase
    {
        #region Variables & Constructor
        
        public readonly Comment newObj = new Comment() { ID = Defaults.Integer };
        
        public CommentService() : base() {;}
        public CommentService(CPMmodel dbcExisting) : base(dbcExisting) { ;}

        #endregion

        #region Search / Fetch

        public List<Comment> Search(int claimID, int? userID)
        {
            //using (dbc)//HT: DON'T coz we're sending IQueryable
            IQueryable<Comment> cQuery = from c in dbc.Comments
                                         join u in dbc.Users on new { UserID = c.UserID } equals new { UserID = u.ID } into u_join
                                         from u in u_join.DefaultIfEmpty()
                                         where c.ClaimID == claimID
                                         orderby c.PostedOn descending
                                         select Transform(c,u.Name, c.ClaimID);

            //Append WHERE clause if applicable
            //if ((userID ?? 0) > 0) cQuery = cQuery.Where(o => o.UserID == userID.Value);

            return cQuery.ToList<Comment>();
        }

        Comment Transform(Comment c, string commentBy, int claimID)
        {
            return c.Set(c1 => { c1.CommentBy = commentBy;                 
                /* IMP: NOTE - The following is NOT needed because we've set the Association Access to Internal in the dbml
                 c1.Claim = null; c1.ClaimID1 = claimID; */
            });
        }
                
        public Comment GetCommentById(int id)
        {
            using (dbc)
            {
                Comment cmt = (from c in dbc.Comments where c.ID == id select c).SingleOrDefault<Comment>();

                if (cmt == null) return newObj;
                cmt.Claim = new Claim();//HT: So that it doesn't complain NULL later
                return cmt;
            }
        }

        #endregion

        #region Add / Edit / Delete & Bulk

        public int Add(Comment commentObj, bool doSubmit)
        {
            //triple ensure that the latest comment.PostedOn date is NOT null
            //commentObj.PostedOn = DateTime.Now;
            //Set lastmodified fields
            commentObj.LastModifiedBy = _SessionUsr.ID;
            commentObj.LastModifiedDate = DateTime.Now;

            dbc.Comments.InsertOnSubmit(commentObj);
            if (doSubmit) dbc.SubmitChanges();

            return commentObj.ID; // Return the 'newly inserted id'
        }
                
        public int AddEdit(Comment commentObj, bool doSubmit)
        {
            if (commentObj.ID <= Defaults.Integer) // Insert
                return Add(commentObj, doSubmit);

            else // Update
            {
                //Set lastmodified fields
                commentObj.LastModifiedBy = _SessionUsr.ID;
                commentObj.LastModifiedDate = DateTime.Now;

                dbc.Comments.Attach(commentObj);//attach the object as modified
                dbc.Refresh(System.Data.Linq.RefreshMode.KeepCurrentValues, commentObj);//Optimistic-concurrency (simplest solution)
                if (doSubmit) dbc.SubmitChanges();
            }

            return commentObj.ID;
        }
                
        public void Delete(Comment commentObj, bool doSubmit)
        {
            dbc.Comments.DeleteOnSubmit(dbc.Comments.Single(c => c.ID == commentObj.ID && c.ClaimID== commentObj.ClaimID));
            if (doSubmit) dbc.SubmitChanges();
        }
        
        public void BulkAddEditDel(List<Comment> records, int CliamID, bool doSubmit)
        {
            #region NOTE
            /* Perform Bulk Add, Edit & Del based on Object properties set in VIEW
             MEANT ONLY FOR ASYNC BULK OPERATIONS
             Handle transaction, error and final commit in Callee 
                        
            //using{dbc}, try-catch and transaction must be handled in callee function
            //Also handle the final commit as follows:
            //dbc.SubmitChanges();//Make a FINAL submit instead of periodic updates
            //txn.Commit();//Commit
            */
            #endregion

            foreach (Comment item in records)
            {
                #region Perform Db operations
                item.ClaimID = CliamID; //Required when adding new Claim
                item.LastModifiedBy = _SessionUsr.ID;
                item.LastModifiedDate = DateTime.Now;
                item.PostedOn = DateTime.Now;// double ensure dates are not null !

                if (item._Deleted)
                    Delete(item, false);
                else if (item._Edited)//Make sure Delete is FIRST
                    AddEdit(item, false);
                else if (item._Added)
                    Add(item, false);
                #endregion
            }
            if (doSubmit) dbc.SubmitChanges();//Make a FINAL submit instead of periodic updates
            //txn.Commit();//Commit - Handled in parent caller routine
        }

        #endregion
    }

    public class CAWcomment : CAWBase
    {
        #region Variables & Constructor

        public CAWcomment(bool Async) : base(Async) {;}

        #endregion

        #region Search / Fetch

        public List<Comment> Search(int claimID, int? userID, string claimGUID)
        {
            List<Comment> result = new List<Comment>();
            Claim clmObj = _Session.Claims[claimGUID];
            int sessionCommentCount = clmObj.aComments.Count;

            if (!IsAsync || sessionCommentCount < 1)
            {// Sync or first time Async
                result = new CommentService().Search(claimID, userID);
                clmObj.aComments.AddRange(result);
                _Session.Claims[claimGUID] = clmObj;
            }

            return IsAsync ? clmObj.aComments : result; // _Session.Claims[claimGUID]
        }

        public Comment GetCommentById(int? id, Claim claimobj)
        {
            // IMP: It cn have -ve values for newly inserted records which are in session
            Comment newObj = new CommentService().newObj;

            if (id.HasValue)
            {
                if (IsAsync) newObj = (claimobj.aComments.SingleOrDefault(c => c.ID == id.Value)) ?? newObj;
                // .SingleOrDefault();//_Session.Claims[claimobj.ClaimGUID]
                else newObj = new CommentService().GetCommentById(id.Value);
            }
            
            //HT: Make sure ClaimId is assigned after the above Claim obj is created
            newObj.ClaimID = claimobj.ID;
            newObj.ClaimGUID = claimobj.ClaimGUID;
            newObj.Archived = claimobj.Archived;

            return newObj;
        }

        #endregion

        #region Add / Edit / Delete

        public int AddEdit(Comment commentObj, bool changeAssignTo, int AssignTo, string AssignToVal)
        {
            Claim claimObj = _Session.Claims[commentObj.ClaimGUID];
            bool sendMail = (claimObj.ID > Defaults.Integer && AssignTo != _SessionUsr.ID);// No need to send mail if its current user
            try
            {
                if (IsAsync)
                {
                    #region Do Async add/edit
                    claimObj.aComments = commentObj.setProp().doOpr(claimObj.aComments);
                    claimObj.AssignedTo = AssignTo;
                    claimObj.AssignToChanged = changeAssignTo;
                    claimObj.AssignToComment = commentObj.Comment1;
                    claimObj.AssignToVal = AssignToVal;
                    #endregion

                    _Session.Claims[commentObj.ClaimGUID] = claimObj;
                    return commentObj.ID;
                }
                else
                {
                    //HT: Handle the following two things from Claim Add/Edit not Comment
                    //new ClaimService().AssignClaim(commentObj.ClaimID, AssignTo);
                    //MailManager.AssignToMail(commentObj.Comment1, commentObj.ClaimID, AssignToEmail);
                    return new CommentService().AddEdit(commentObj.setProp(), true);
                }
            }
            #region Catch exception or if success finally send Email
            catch (Exception ex)
            { sendMail = false; throw ex; }
            finally
            {
                #region Check and send email
                if (sendMail)//Make sure "_Session.Claim" is available
                {
                    string UserEmail = new UserService().GetUserEmailByID(claimObj.AssignedTo);
                    MailManager.AssignToMail(claimObj.ClaimNo.ToString(), claimObj.AssignToComment, claimObj.ID, UserEmail, (_SessionUsr.UserName), true);
                }
                #endregion
            }
            #endregion
        }

        public void Delete(Comment commentObj)
        {
            commentObj._Deleted = true; commentObj._Added = commentObj._Edited = false;

            if (IsAsync)// Do Async delete
            {
                Claim data = _Session.Claims[commentObj.ClaimGUID];
                data.aComments = commentObj.doOpr(data.aComments);
                _Session.Claims[commentObj.ClaimGUID] = data;                
            }
            else
                new CommentService().Delete(commentObj, true);
        }

        #endregion
    }
}