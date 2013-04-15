using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Security.Principal;
using CPM.Services;

namespace CPM.Helper
{
    /* IMP NOTE:
    For now we're using Forms Authentication and for Authorization we use Session + MVC attributes(like IsAuthorize)
    This has been an imp R&D so it is kept for further ref
    */
    
    public class User : IPrincipal
    {
        protected User() { }
        public User(int userId, string userName, string fullName, string password)
        {
            UserId = userId;
            UserName = userName;
            FullName = fullName;
            Password = password;
        }
        public virtual int UserId { get; set; }
        public virtual string UserName { get; set; }
        public virtual string FullName { get; set; }
        public virtual string Password { get; set; }
        public virtual IIdentity Identity
        {
            get;
            set;
        }
        public virtual bool IsInRole(string role)
        {
            /*
            if (Role.Description.ToLower() == role.ToLower())
                return true;
            foreach (Right right in Role.Rights)
            {
                if (right.Description.ToLower() == role.ToLower())
                    return true;
            }
            */
            return false;
        }
    }

    public class Role
    {
        protected Role() { }
        public Role(int roleid, string roleDescription)
        {
            RoleId = roleid;
            Description = roleDescription;
        }
        public virtual int RoleId { get; set; }
        public virtual string Description { get; set; }
        public virtual IList<Right> Rights { get; set; }
    }

    public class Right
    {
        protected Right() { }
        public Right(int rightId, string description)
        {
            RightId = rightId;
            Description = description;
        }
        public virtual int RightId { get; set; }
        public virtual string Description { get; set; }
    }

    public class UserMemberProvider : MembershipProvider
    {
        #region Unimplemented MembershipProvider Methods
        public override string ApplicationName
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }
        public override bool ChangePassword(string username, string oldPassword, string newPassword)
        {
            throw new NotImplementedException();
        }
        public override bool ChangePasswordQuestionAndAnswer(string username, string password, string newPasswordQuestion, string newPasswordAnswer)
        {
            throw new NotImplementedException();
        }
        public override MembershipUser CreateUser(string username, string password, string email, string passwordQuestion, string passwordAnswer, bool isApproved, object providerUserKey, out MembershipCreateStatus status)
        {
            throw new NotImplementedException();
        }

        public override bool DeleteUser(string username, bool deleteAllRelatedData)
        {
            throw new NotImplementedException();
        }
        public override bool EnablePasswordReset
        {
            get { throw new NotImplementedException(); }
        }
        public override bool EnablePasswordRetrieval
        {
            get { throw new NotImplementedException(); }
        }
        public override MembershipUserCollection FindUsersByEmail(string emailToMatch, int pageIndex, int pageSize, out int totalRecords)
        {
            throw new NotImplementedException();
        }
        public override MembershipUserCollection FindUsersByName(string usernameToMatch, int pageIndex, int pageSize, out int totalRecords)
        {
            throw new NotImplementedException();
        }
        public override MembershipUserCollection GetAllUsers(int pageIndex, int pageSize, out int totalRecords)
        {
            throw new NotImplementedException();
        }
        public override int GetNumberOfUsersOnline()
        {
            throw new NotImplementedException();
        }
        public override string GetPassword(string username, string answer)
        {
            throw new NotImplementedException();
        }
        public override MembershipUser GetUser(string username, bool userIsOnline)
        {
            throw new NotImplementedException();
        }
        public override MembershipUser GetUser(object providerUserKey, bool userIsOnline)
        {
            throw new NotImplementedException();
        }
        public override string GetUserNameByEmail(string email)
        {
            throw new NotImplementedException();
        }
        public override int MaxInvalidPasswordAttempts
        {
            get { throw new NotImplementedException(); }
        }
        public override int MinRequiredNonAlphanumericCharacters
        {
            get { throw new NotImplementedException(); }
        }
        public override int MinRequiredPasswordLength
        {
            get { throw new NotImplementedException(); }
        }
        public override int PasswordAttemptWindow
        {
            get { throw new NotImplementedException(); }
        }
        public override MembershipPasswordFormat PasswordFormat
        {
            get { throw new NotImplementedException(); }
        }
        public override string PasswordStrengthRegularExpression
        {
            get { throw new NotImplementedException(); }
        }
        public override bool RequiresQuestionAndAnswer
        {
            get { throw new NotImplementedException(); }
        }
        public override bool RequiresUniqueEmail
        {
            get { throw new NotImplementedException(); }
        }
        public override string ResetPassword(string username, string answer)
        {
            throw new NotImplementedException();
        }
        public override bool UnlockUser(string userName)
        {
            throw new NotImplementedException();
        }
        public override void UpdateUser(MembershipUser user)
        {
            throw new NotImplementedException();
        }

        #endregion
        UserService usrServiceObj;
        public UserMemberProvider() : this(null) {;}
        public UserMemberProvider(UserService usrService): base()
        {
            usrServiceObj = usrService ?? new UserService();
        }
        public User User
        {
            get;
            private set;
        }
        public CPM.DAL.Users CreateUser(string fullName, string passWord, string email)
        {
            return (null); // HT: Implemented as - new UserService().AddEdit(new CPM.DAL.Users());
        }
        public override bool ValidateUser(string username, string password)
        {
            if (string.IsNullOrEmpty(password.Trim())) return false;
            //string hash = EncryptPassword(password);

            CPM.DAL.vw_Users_Role_Org usr = usrServiceObj.Login(username, password);

            if (usr != null && usr != new UserService().emptyView)
            {
                _SessionUsr.setUserSession(usr);//Set session
                return true;
            }
            else return false;
        }
        /* HT: Kept for future usage
        
        /// <summary>
        /// Procuses an MD5 hash string of the password
        /// </summary>
        /// <param name="password">password to hash</param>
        /// <returns>MD5 Hash string</returns>
        protected string EncryptPassword(string password)
        {
            //we use codepage 1252 because that is what sql server uses
            byte[] pwdBytes = Encoding.GetEncoding(1252).GetBytes(password);
            byte[] hashBytes = System.Security.Cryptography.MD5.Create().ComputeHash(pwdBytes);
            return Encoding.GetEncoding(1252).GetString(hashBytes);
        }
        */        
    }

    public class UserRoleProvider : RoleProvider
    {
        UserService usrServiceObj;
        public UserRoleProvider(): this(null){;}
        public UserRoleProvider(UserService usrService): base()
        {
            usrServiceObj = usrService ?? new UserService();
        }
        public override bool IsUserInRole(string username, string roleName)
        {
            
            try
            {
                switch (_Enums.ParseEnum<SecurityService.Roles>(roleName))
                {
                    case SecurityService.Roles.Internal: return _Session.IsInternal;
                    case SecurityService.Roles.Sales: return _Session.IsSales;
                    case SecurityService.Roles.Admin: return _Session.IsAdmin;
                    default: return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        #region Unimplemented
        public override string ApplicationName
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }
        public override void AddUsersToRoles(string[] usernames, string[] roleNames)
        {
            throw new NotImplementedException();
        }
        public override void RemoveUsersFromRoles(string[] usernames, string[] roleNames)
        {
            throw new NotImplementedException();
        }
        public override void CreateRole(string roleName)
        {
            throw new NotImplementedException();
        }
        public override bool DeleteRole(string roleName, bool throwOnPopulatedRole)
        {
            throw new NotImplementedException();
        }
        public override bool RoleExists(string roleName)
        {
            throw new NotImplementedException();
        }
        public override string[] GetUsersInRole(string roleName)
        {
            throw new NotImplementedException();
        }
        public override string[] FindUsersInRole(string roleName, string usernameToMatch)
        {
            throw new NotImplementedException();
        }
        public override string[] GetAllRoles()
        {
            throw new NotImplementedException();
        }
        #endregion

        public override string[] GetRolesForUser(string username)
        {
            /*
            User user = _repository.GetByUserName(username);
            string[] roles = new string[user.Role.Rights.Count + 1];
            roles[0] = user.Role.Description;
            int idx = 0;
            foreach (Right right in user.Role.Rights)
                roles[++idx] = right.Description;
            return roles;
            */
            return Enum.GetValues(typeof(SecurityService.Roles)).Cast<SecurityService.Roles>().Select(enu => enu.ToString()).ToArray<string>();
        }        
    }
}
