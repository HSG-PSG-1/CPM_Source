using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Linq;
using System.Linq.Dynamic;
using System.Data.Linq.SqlClient;
using CPM.DAL;
using CPM.Helper;
using Webdiyer.WebControls.Mvc;

namespace CPM.Services
{
    public class UserService : _ServiceBase
    {
        #region Variables
        
        public readonly vw_Users_Role_Org emptyView = 
            new vw_Users_Role_Org() { ID = Defaults.Integer };
        public readonly Users emptyUsr = new Users() { ID = Defaults.Integer, 
            UserLocations = new EntitySet<UserLocation>() };//Make sure UserLocations is reset
        public const string sortOn = "UserName ASC", sortOn1 = "UserName ASC"; // Default for secondary sort

        public const string orgTypeJS = "if(user.OrgTypeId() == 1)user.OrgType(\"Internal\"); else if(user.OrgTypeId() == 2)user.OrgType(\"Vendor\");";

        #endregion

        #region Login related

        public vw_Users_Role_Org Login(string email, string password)
        {
            vw_Users_Role_Org usr = emptyView;
            
            //using (dbc) HT: Coz we're using the data
            {
                usr = dbc.vw_Users_Role_Orgs.FirstOrDefault(u => u.Email.ToUpper() == email && u.Password == password);
                // CAUTION: SingleOrDefault is causing error !
                    /*(from u in dbc.vw_Users_Role_Orgs
                       where u.Email.ToUpper() == email && u.Password == password
                       select u).SingleOrDefault();*/
            }
            return usr ?? null; // emptyView will not compare correctly
        }

        public MasterRole GetRoleRights(int RoleID)
        {
            MasterRole roleRights = new MasterRole();
            using (dbc) 
            {
                roleRights = dbc.MasterRoles.SingleOrDefault(r => r.ID == RoleID);
                //(from r in dbc.MasterRoles where r.ID == RoleID select r).SingleOrDefault();
                roleRights.Users = null;//to avoid issues with XMLSerializer
            }
            return roleRights;
        }

        #endregion

        #region Search / Fetch

        public PagedList<vw_Users_Role_Org> Search(string orderBy, int? pgIndex, int pageSize, vw_Users_Role_Org usr)
        {
            orderBy = string.IsNullOrEmpty(orderBy) ? sortOn : orderBy;

            using (dbc)
            {
                IQueryable<vw_Users_Role_Org> userQuery = (from vw_u in dbc.vw_Users_Role_Orgs select vw_u);
                //Get filters - if any
                userQuery = PrepareQuery(userQuery, usr);
                // Apply Sorting, Pagination and return PagedList
                return userQuery.OrderBy(orderBy).ToPagedList(pgIndex ?? 1, pageSize);

                /* Apply pagination and return
                return userQuery.Skip(startRow).Take(pageSize).ToList<vw_User_Org_UserRole>();
                */
            }
        }

        public List<vw_Users_Role_Org> SearchKO(vw_Users_Role_Org usr)//string orderBy, int? pgIndex, int pageSize, vw_Users_Role_Org usr, bool fetchAll)
        {
            //orderBy = string.IsNullOrEmpty(orderBy) ? sortOn : orderBy;

            using (dbc)
            {
                IQueryable<vw_Users_Role_Org> userQuery = (from vw_u in dbc.vw_Users_Role_Orgs select vw_u);
                //Get filters - if any
                userQuery = PrepareQuery(userQuery, usr);
                // Apply Sorting
                userQuery = userQuery.OrderBy(sortOn);
                // Apply pagination and return
                //if (fetchAll)
                    return userQuery.ToList<vw_Users_Role_Org>();
                //else
                //    return userQuery.Skip(pgIndex.Value).Take(pageSize).ToList<vw_Users_Role_Org>();
            }
        }

        public static IQueryable<vw_Users_Role_Org> PrepareQuery(IQueryable<vw_Users_Role_Org> userQuery, vw_Users_Role_Org usr)
        {
            #region Append WHERE clause if applicable

            if (!string.IsNullOrEmpty(usr.UserName)) userQuery = userQuery.Where(o => SqlMethods.Like(o.UserName.ToUpper(), usr.UserName.ToUpper()));
            //userQuery.Where(o => o.Name.ToUpper().Contains(userName));
            if (usr.RoleID > 0) userQuery = userQuery.Where(o => o.RoleID == usr.RoleID);
            else if (!string.IsNullOrEmpty(usr.RoleName))
                userQuery = userQuery.Where(o => SqlMethods.Like(o.RoleName.ToLower(), usr.RoleName.ToLower()));

            //if (usr.OrgID > 0) userQuery = userQuery.Where(o => o.OrgID == usr.OrgID);
            if (!string.IsNullOrEmpty(usr.Email))
                userQuery = userQuery.Where(o => SqlMethods.Like(o.Email.ToUpper(), usr.Email.ToUpper()));
            if (!string.IsNullOrEmpty(usr.OrgName))
                userQuery = userQuery.Where(o => SqlMethods.Like(o.OrgName.ToUpper(), usr.OrgName.ToUpper()));
            if (!string.IsNullOrEmpty(usr.Country))
                userQuery = userQuery.Where(o => SqlMethods.Like(o.Country.ToUpper(), usr.Country.ToUpper()));
            if (!string.IsNullOrEmpty(usr.SalespersonCode))
                userQuery = userQuery.Where(o => SqlMethods.Like(o.SalespersonCode.ToUpper(), usr.SalespersonCode.ToUpper()));

            #endregion

            return userQuery;
        }

        public Users GetUserById(int id, int OrgID)
        {
            #region Kept for Ref (Tried to load with Users.UserLocations)
            /*http://stackoverflow.com/questions/32433/select-from-multiple-table-using-linq
            DataLoadOptions dlo = new DataLoadOptions();
            //dlo.LoadWith<UserLocation>(v => v.CustomerLocation);
            //HT: SPECIAL CASE: Have added an association between vw_Users_Role_Org & UserLocation (also set PK in vw)
            //Ref: http://forums.asp.net/t/1514585.aspx
            dlo.LoadWith<vw_Users_Role_Org>(v => v.UserLocations);

            dbc.DeferredLoadingEnabled = false;
            dbc.LoadOptions = dlo; */
            #endregion

            using (dbc)
            {
                vw_Users_Role_Org vw_u = (from vw in dbc.vw_Users_Role_Orgs
                                          where vw.ID == id
                                          select vw).SingleOrDefault<vw_Users_Role_Org>();

                emptyUsr.UserLocations = new EntitySet<UserLocation>();//To make sure it DOESN'T come from cache
                Users usr = emptyUsr;
                if (vw_u != null)
                    usr = new Users
                     {
                         ID = vw_u.ID,
                         Name = vw_u.UserName,                         
                         RoleID = vw_u.RoleID,
                         OrgID = vw_u.OrgID,
                         OrgType = vw_u.OrgTypeId.Value,
                         Email = vw_u.Email,
                         LastModifiedBy = vw_u.LastModifiedBy,
                         Password = vw_u.Password,
                         LastModifiedDate = vw_u.LastModifiedDate,
                         /* Set other special properties */
                         EmailOLD = vw_u.Email,
                         LastModifiedByVal = vw_u.LastModifiedByName,
                         OrgName = vw_u.OrgName,
                         OrgTypeName = vw_u.OrgType,
                         SalespersonCode = vw_u.SalespersonCode
                     };
                
                usr.OriOrgId = usr.OrgID;//Required ahead
                return usr;
            }
        }

        public string GetUserPWDByEmail(string email)
        {
            if (string.IsNullOrEmpty(email)) return string.Empty;
            return (from u in dbc.Users
                    where u.Email.ToLower() == email.ToLower()
                    select u.Password).SingleOrDefault<string>() ?? "";
        }

        public string GetUserEmailByID(int ID)
        {
            if (ID <= Defaults.Integer) return string.Empty;
            return (from u in dbc.Users
                    where u.ID == ID
                    select u.Email).SingleOrDefault<string>() ?? "";
        }
        
        #endregion

        #region Add / Edit / Delete

        public int Add(Users userObj, string LinkedLoc, string UnlinkedLoc)
        {
            //Set lastmodified fields
            userObj.LastModifiedBy = _SessionUsr.ID;
            userObj.LastModifiedDate = DateTime.Now;

            dbc.Users.InsertOnSubmit(userObj);
            dbc.SubmitChanges();

            //Process Locations
            BulkAddDelLocations(userObj.ID, LinkedLoc, UnlinkedLoc, true, userObj.OrgIdChanged);

            return userObj.ID; // Return the 'newly inserted id'
        }

        public static Users GetObjFromVW(vw_Users_Role_Org usr)
        {
            return new Users()
            {
                ID = usr.ID,
                RoleID = usr.RoleID,
                OrgID = usr.OrgID,
                Name = usr.UserName,
                Email = usr.Email,
                Password = usr.Password,
                LastModifiedBy = usr.LastModifiedBy,
                LastModifiedDate = DateTime.Now
            };
        }

        public int AddEdit(Users userObj, string LinkedLoc, string UnlinkedLoc)
        {
            int userID = userObj.ID;

            if (userID <= Defaults.Integer) // Insert
                userID = Add(userObj, LinkedLoc, UnlinkedLoc);

            else
            {
                #region Update
                
                //Set lastmodified fields
                userObj.LastModifiedBy = _SessionUsr.ID;
                userObj.LastModifiedDate = DateTime.Now;

                dbc.Users.Attach(userObj);//attach the object as modified
                dbc.Refresh(System.Data.Linq.RefreshMode.KeepCurrentValues, userObj);

                //Process Locations
                BulkAddDelLocations(userID, LinkedLoc, UnlinkedLoc, false, userObj.OrgIdChanged);
                dbc.SubmitChanges();

                #endregion
            }

            #region If the user is current User's user then update the session for userAttributes
            
            if (_SessionUsr.ID == userObj.ID)
            {
                _SessionUsr.setUserSession(Login(userObj.Email, userObj.Password)); // HT: Probably no other way
                _Session.RoleRights = GetRoleRights(userObj.RoleID);
            }

            #endregion

            return userObj.ID;
        }

        public bool Delete(Users userObj)
        {
            dbc.Users.DeleteOnSubmit(dbc.Users.Single(c => c.ID == userObj.ID));
            dbc.SubmitChanges();
            // Following code is not working - they say 'optimistic concurrency' is not too gud in L2S
            //dbc.users.Attach(userObj);//attach the object to be deleted
            //dbc.users.DeleteOnSubmit(userObj);//delete the object
            //dbc.SubmitChanges();
            return true;
        }

        #endregion

        #region User Location

        public List<UserLocation> GetUserLocations(int usrID, int OrgID, bool orgChanged)
        {
            List<UserLocation> result = new List<UserLocation>();

            if (usrID > 0 && !orgChanged)//EDIT MODE, DON'T use - _Session.SkipCustLocCheck
            {
                #region Edit User mode

                List<vw_CustLoc_User_UserLoc> source = (from v in dbc.vw_CustLoc_User_UserLocs
                                                        where v.OrgID == OrgID && v.UserID == usrID
                                                        orderby v.Name
                                                        select v).ToList();
                // Put data in destination model
                foreach (vw_CustLoc_User_UserLoc loc in source)
                    result.Add(new UserLocation()
                    {
                        LocID = loc.ID,
                        UserID = usrID,
                        Location = loc.Name,
                        LocCode = loc.Code,
                        WasLinked = (loc.UsrLocID.HasValue),
                        IsLinked = (loc.UsrLocID.HasValue)
                    });

                #endregion
            }
            else
            {
                #region Add User mode
                List<CustomerLocation> source =
                    (from l in dbc.CustomerLocations where l.CustomerId == OrgID orderby l.Name select l).ToList();
                // Put data in destination model
                foreach (CustomerLocation loc in source)
                    result.Add(new UserLocation()
                    {
                        LocID = loc.ID,
                        UserID = usrID,
                        Location = loc.Name,
                        LocCode = loc.Code
                    });
                #endregion
            }
            return result.OrderByDescending(o => o.IsLinked).ToList();
        }

        public List<UserLocation> GetUserLocations1(int usrID, int OrgID)
        {// KEpt for ref and future usage
            #region Initialize & populate existing locs data

            List<UserLocation> result = new List<UserLocation>();
            List<UserLocation> existing = (from ul in dbc.UserLocations where ul.UserID == usrID select ul).ToList<UserLocation>();

            List<CustomerLocation> locs = (from cl in dbc.CustomerLocations
                                           where cl.CustomerId == OrgID
                                           orderby cl.Name
                                           select cl).ToList<CustomerLocation>();//DON'T use - _Session.SkipCustLocCheck

            foreach (UserLocation uloc in existing)
            {// Add additional data
                uloc.WasLinked = uloc.IsLinked = true;
                uloc.Location = (from l in locs where l.ID == uloc.LocID select l.Name).SingleOrDefault<string>();
            }

            #endregion

            //Add existing (order by Location)
            result.AddRange(from ul in existing orderby ul.Location select ul);
            //Skip existing and add the others(order by Location)
            IEnumerable<int> LocIDs = (from ul in existing select ul.LocID).Cast<int>();
            result.AddRange(from cl in locs where !LocIDs.Contains(cl.ID) select Transform(cl, usrID));

            return result;
        }

        UserLocation Transform(CustomerLocation c, int usrID)
        {
            return new UserLocation() { IsLinked = false, WasLinked = false, Location = c.Name, LocID = c.ID, UserID = usrID };
        }

        public void BulkAddDelLocations(int userID, string LinkedLoc, string UnlinkedLoc, bool doSubmit, bool OrgIdChanged)
        {
            //Delete all the previous relationships before having new ones
            if (OrgIdChanged)
                dbc.UserLocations.DeleteAllOnSubmit(dbc.UserLocations.Where(ul => ul.UserID == userID));

            if (!string.IsNullOrEmpty(UnlinkedLoc))// Unlink = delete
                dbc.UserLocations.DeleteAllOnSubmit(dbc.UserLocations.Where(ul => ul.UserID == userID &&
                    Defaults.stringToIntList(UnlinkedLoc).Contains(ul.LocID)));

            if (!string.IsNullOrEmpty(LinkedLoc))// Link = add
                foreach (int LocationID in Defaults.stringToIntList(LinkedLoc))
                    dbc.UserLocations.InsertOnSubmit(new UserLocation()
                    {
                        UserID = userID,
                        LocID = LocationID,
                        LastModifiedBy = _SessionUsr.ID,
                        LastModifiedDate = DateTime.Now
                    });

            #region OLD (kept for ref): Add code to bulk delete existing ones?
            /*if (locations != null){//Process locations
                foreach (CPM.DAL.UserLocation uloc in locations)
                {
                    uloc.UserID = userID;//Don't forget (esp for newly added)
                    //Set lastmodified fields
                    uloc.LastModifiedBy = _SessionUsr.ID;
                    uloc.LastModifiedDate = DateTime.Now;

                    if (uloc.IsDeleted)//Delete
                        dbc.UserLocations.DeleteOnSubmit(dbc.UserLocations.Single(ul => ul.UserID == userID && ul.LocID == uloc.LocID));
                    else if (uloc.IsAdded)//Insert
                        dbc.UserLocations.InsertOnSubmit(uloc);
                }
            }*/
            #endregion

            if (doSubmit)//Submit only if mentioned (required while edit)
                dbc.SubmitChanges();
        }

        #endregion

        #region Extra functions

        public override bool IsReferred(Object oObj)
        {
            Users uObj = (Users)oObj;
            //Check if tis user has any Claims or if he's assigned any claims
            bool referred = (dbc.Claims.Where(u => u.AssignedTo == uObj.ID || u.SalespersonID == uObj.ID
                //|| u.CustID == uObj.OrgID - NOT needed because we just need the Customer Org (not necessarily user related to it)
                ).Count() > 0);

            referred = referred || (dbc.MasterOrgs.Where(o => o.SalespersonId == uObj.ID).Count() > 0);
            referred = referred || (dbc.Comments.Where(f => f.UserID == uObj.ID).Count() > 0);
            referred = referred || (dbc.FileHeaders.Where(f => f.UserID == uObj.ID).Count() > 0);
            referred = referred || (dbc.FileDetails.Where(f => f.UserID == uObj.ID).Count() > 0);
            
            // For future usage - not really because tis just archvied history!
            //if (!referred) referred = (dbc.ActivityHistories.Where(u => u.UserID == uObj.ID).Count() > 0);
            
            return referred;
        }

        public bool IsUserEmailDuplicate(string userEmail)
        {
            return (UserEmailCount(userEmail) > 0);
        }

        public int UserEmailCount(string userEmail)
        {
            return dbc.Users.Where(u => u.Email.ToUpper() == userEmail.ToUpper()).Count();
        }

        #endregion        
    }
}
