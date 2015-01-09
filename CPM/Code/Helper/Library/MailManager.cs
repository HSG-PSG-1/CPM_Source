#region using directives

using Microsoft.VisualBasic;
using System.Net.Mail;
using System.Xml;
using System.Data;
using System.Data.SqlClient;      
using System.Web;
using System;

#endregion //using directives

namespace CPM.Helper
{
    public class MailManager
    {
        #region Configure & send email
        
        public const char emailSep = ',';
        
        /// <summary>
        /// send email
        /// </summary>
        /// <param name="message">MailMessage to be sent</param>
        /// <returns>True if email is sent successfully else false</returns>
        private static bool Send(MailMessage message)
        {
            try
            {                
                #region Send Email
                SmtpClient mail = new SmtpClient();
                if (Config.DebugMail)
                    HttpContext.Current.Response.Write(
                            string.Format(MailTemplate.debugEmailText, message.From, message.To, message.Subject, message.Body, message.Attachments));
                else
                    mail.Send(message);
                #endregion

                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        //Function to configure email
        /// <summary>
        /// configure email
        /// </summary>
        /// <param name="From">From address</param>
        /// <param name="To">To Address (to send the same message to multiple receipient separate individual email address with semicolon ";")</param>
        /// <param name="Body">Email body</param>
        /// <param name="Subject">Subject</param>
        /// <param name="Attachments">Attachment</param>
        /// <returns>configured MailMessage</returns>
        public static MailMessage ConfigureMailMessage(string From, string ToAddr, string Body, string Subject, string Attachments)
        {
            MailMessage message = new MailMessage();

            #region Set Mail parameters
            // SET To
            if (!string.IsNullOrEmpty(From.Trim()))//if (!(From == null) && !(From.Trim().Length == 0))
            {
                MailAddress fromAddress = new MailAddress(From);
                message.From = fromAddress;
            }
            // SET From
            string[] toAddress = ToAddr.Split(new char[] { emailSep });
            foreach (string address in toAddress)
                message.To.Add(address);

            #endregion

            message.IsBodyHtml = true;
            message.Subject = Subject;
            message.Body = Body;

            if (!string.IsNullOrEmpty(Attachments.Trim()))//if (!((Attachments == null)) && !((Attachments.Trim().Length == 0)))
            {
                System.Net.Mail.Attachment attachment = new System.Net.Mail.Attachment(Attachments);
                message.Attachments.Add(attachment);
            }

            return message;
        }

        #endregion
        //Function to send claim comment mail
        /// <summary>
        /// Function to send Assign To change claim email
        /// </summary>
        /// <param name="Comment">Claim comment</param>
        /// <param name="ClaimId">Claim Id</param>
        /// <param name="AssignToEmail">Email of the user to whom the Claim has been Assigned</param>
        /// <returns>True if sent</returns>
        public static bool AssignToMail(string ClaimNo, string Comment, int ClaimId, string AssignToEmail, string Assigner, bool FromComment)
        {
            MailTemplate template = new MailTemplate(MailTemplate.Templates.ClaimAssignTo);
            //fetch subject and contents
            string Subject = template.Subject.Replace("[CLAIM#]", ClaimNo);
            System.Text.StringBuilder Body = new System.Text.StringBuilder(template.Body);
            //set contents
            string claimLink = //FromComment ? Defaults.trimLastURLSegment(HttpContext.Current.Request.Url.ToString()) : HttpContext.Current.Request.Url.ToString();
            HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Authority) +
            new System.Web.Mvc.UrlHelper(HttpContext.Current.Request.RequestContext).Action("Manage?", "Claim", new { ClaimID = ClaimId });
            
            claimLink = System.Web.HttpUtility.UrlDecode(claimLink);//.TrimEnd(new char[] { '?' });//Need to add Manage? so that mvc doesn't remove default action, now trim it.

            Body = Body.Replace("[CLAIM#]", ClaimNo).Replace("[ASSIGNER]", Assigner).Replace("[COMMENT]", Comment).Replace("[LINK]", claimLink);
            //send email            
            return Send(ConfigureMailMessage(Config.ContactEmail, AssignToEmail, Body.ToString(), Subject, ""));
        }
        //Function to send forget password mail
        /// <summary>
        /// Function for forget password mail
        /// </summary>
        /// <param name="userEmail">User Email</param>
        /// <param name="password">User Password</param>
        /// <returns>true if the email is sent successfully</returns>
        public static bool ForgotPwdMail(string UserEmail, string Password, string From)
        {
            MailTemplate template = new MailTemplate(MailTemplate.Templates.ForgotPwd);
            //fetch subject and contents
            string Subject = template.Subject;
            System.Text.StringBuilder Body = new System.Text.StringBuilder(template.Body);
            string LoginURL = HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Authority) +
            new System.Web.Mvc.UrlHelper(HttpContext.Current.Request.RequestContext).Action("Login", "Common", 
            new { email = UserEmail, pwd = Crypto.EncodeStr(Password, true), remember = true });
            //set contents
            Body = Body.Replace("[EMAIL]", UserEmail).Replace("[PASSWORD]", Password).Replace("[LOGINURL]", LoginURL);
            //send email
            return Send(ConfigureMailMessage(From, UserEmail, Body.ToString(), Subject, ""));
        }        
        //Function for ApplicationErrorMail
        /// <summary>
        /// Function for ApplicationErrorMail
        /// </summary>
        public static bool ApplicationErrorMail(string ErrorDetails)
        {
            MailTemplate template = new MailTemplate(MailTemplate.Templates.ApplicationError);
            //fetch subject and contents
            string Subject = template.Subject;
            System.Text.StringBuilder Body = new System.Text.StringBuilder(template.Body);
            //set contents
            Body = Body.Replace("[ERRORDETAILS]", ErrorDetails);
            //send email
            return Send(ConfigureApplicationErrorMail(ErrorDetails, new MailMessage()));
        }
        // Special case function for ELMAH (only configure email)
        /// <summary>
        /// Configure email
        /// </summary>
        /// <param name="ErrorDetails">Error details</param>
        /// <param name="mailMsg">MailMessage to configure</param>
        /// <returns>MailMessage</returns>
        public static MailMessage ConfigureApplicationErrorMail(string ErrorDetails, MailMessage mailMsg, string physicalApplicationPath = "")
        {
            MailTemplate template = new MailTemplate(MailTemplate.Templates.ApplicationError, physicalApplicationPath);
            //fetch subject and contents
            string Subject = template.Subject;
            System.Text.StringBuilder Body = new System.Text.StringBuilder(template.Body);
            //set contents
            Body = Body.Replace("[ERRORDETAILS]", ErrorDetails);
            //configure email
            mailMsg.From = new MailAddress(Config.ContactEmail);
            mailMsg.To.Clear(); // Provide comma sep as per - http://msdn.microsoft.com/en-us/library/ms144695.aspx
            mailMsg.To.Add(Config.ApplicationErrorEmail);
            mailMsg.Subject = Subject;
            mailMsg.Body = Body.ToString() + mailMsg.Body; // Prepand our dody info
            return mailMsg;
        } 
    }

    class MailTemplate
    {
        #region Varialbles
        static char directorySeparator = System.IO.Path.DirectorySeparatorChar;
        public const string debugEmailText = "<b>FROM</b>:{0}<BR/><b>TO</b>:{1}<BR/><b>SUBJECT</b>:{2}<BR/><b>BODY</b>:{3}<BR/><b>ATTACHMENTS</b>:{4}<HR/>";
        public enum Templates : int
        {
            ForgotPwd = 0,
            ApplicationError,
            ClaimAssignTo
        }
        string fileNam = string.Empty;
        XmlDocument XD = new XmlDocument();
        #endregion //Varialbles

        public string Subject { get {return XD.DocumentElement.SelectSingleNode("subject").InnerText; } }
        public string Body { get { return XD.DocumentElement.SelectSingleNode("body").InnerText; } }
        public MailTemplate(Templates file, string physicalApplicationPath = "") // physicalApplicationPath  - Pass it for locations in which HttpContext is NULL 
        {
            switch (file)
            {
                case Templates.ApplicationError: fileNam = Templates.ApplicationError.ToString(); break;
                case Templates.ForgotPwd: fileNam = Templates.ForgotPwd.ToString(); break;
                case Templates.ClaimAssignTo: fileNam = Templates.ClaimAssignTo.ToString(); break;
            }

            XD.Load(GetEmailTemplatePath(physicalApplicationPath));
        }

        /// <summary>
        /// Get the physical path of the email template directory
        /// </summary>
        public string GetEmailTemplatePath(string physicalApplicationPath = "")// physicalApplicationPath  - Pass it for locations in which HttpContext is NULL 
        {
            string EmailTemplatePath = ((HttpContext.Current == null) ? physicalApplicationPath : HttpContext.Current.Request.PhysicalApplicationPath);
            EmailTemplatePath += directorySeparator + Config.EmailTemplatePathByRefToRoot + directorySeparator;

            return EmailTemplatePath + fileNam + ".xml";
        }
    }
}
