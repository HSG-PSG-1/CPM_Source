using System;
using System.Collections.Generic;
using System.Linq;
using CPM.DAL;

namespace CPM.Services
{
    public class NavigatorService
    {
        public enum NavFeature
        { 
            Dashboard = 0,
            ClaimEntry,
            Usr,
            Print,
            ActivityLog
        }

        public string NavigationStr { get; set; }

        public NavigatorService(string NavString)
        {
            NavigationStr = NavString;
        }

        public object Navigate(ref NavFeature NavTo)
        {
            if (string.IsNullOrEmpty(NavigationStr)) return null;

            NavString str = new NavString(NavigationStr);
            Feature NavFeature = new DashboardNav(str.Feature);

            //HT: Needto deploy is Print
            bool matched = false;
            if (str.HasIDs || NavFeature.FeatureMatch())
            {
                NavTo = NavigatorService.NavFeature.Dashboard;
                matched = true;
                
                if (str.HasIDs)
                    NavFeature.IdSearch(str.Filters??str.Feature);
                else if (str.HasFilters)
                    NavFeature.Search(str.Filters);

                return ((DashboardNav)NavFeature).filter;
            }
            
            if (!matched)
            {
                NavFeature = new ClaimNav(str.Feature);
                if (str.HasIDs || NavFeature.FeatureMatch())
                {
                    NavTo = NavigatorService.NavFeature.ClaimEntry;
                    matched = true;

                    if (str.HasIDs)
                        NavFeature.IdSearch(str.Filters ?? str.Feature);
                    else if (str.HasFilters)
                        NavFeature.Search(str.Filters);

                    return ((ClaimNav)NavFeature).ClaimId;
                }
            }

            if (!matched)
            {
                NavFeature = new UserNav(str.Feature);
                if (str.HasIDs || NavFeature.FeatureMatch())
                {
                    NavTo = NavigatorService.NavFeature.Usr;
                    matched = true;

                    if (str.HasIDs)
                        NavFeature.IdSearch(str.Filters ?? str.Feature);
                    else if (str.HasFilters)
                        NavFeature.Search(str.Filters);

                    return ((UserNav)NavFeature).filter;
                }
            }

            return null;
        }

        public class NavString
        {
            string[] TrimPrefix = new string[] { " ", "search", "find", "all", "all the" };

            public string Feature { get; set; }
            public bool HasFilters { get; set; }
            public bool HasIDs { get; set; }
            public string Filters { get; set; }

            public string PolishedStr { get; set; }

            public NavString(string str)
            {
                str = PolishedStr = Polish(str);
                string[] data = str.Split(new char[] { ' ' });

                HasFilters = data.Length > 1;
                HasIDs = !HasFilters || (str.IndexOf('=') < 0);
                
                if (HasFilters || data.Length > 0) Feature = data[0];
                if (HasFilters) Filters = data[1];
            }

            public string Polish(string str)
            {
                //Remove unused prefix
                if(string.IsNullOrEmpty((str??"").Trim())) return string.Empty;

                str = str.ToLower();

                #region Clean Prefix
                
                bool dirty = true;
                while (dirty)
                {
                    dirty = false;
                    foreach (string prefix in TrimPrefix)
                    {
                        dirty = str.StartsWith(prefix);
                        if (dirty)
                            str = str.Substring(str.IndexOf(prefix), prefix.Length);
                    }
                }

                #endregion                

                //arrange
                str = str.Replace(", ", ",").Replace(" ,", ",");

                return str.Trim();
            }
        }

        public abstract class Feature
        {
            public NavigatorService.NavFeature FeatureName { get; set; }
            public string[] Alias { get; set; }
            public string Data { get; set; }

            public abstract  bool FeatureMatch();

            public abstract void IdSearch(object Id);
            //public abstract void IdsSearch(object[] Id);
            public abstract void Search(object filters);
        }

        public class DashboardNav : Feature
        {
            public vw_Claim_Dashboard filter { get; set; }
            public DashboardNav(string NavString)
            {
                Alias = new string[]
                {
                    "dashboard","dashbaord", "dshboard", "dasboard", "dashboad",
                    "search", "sarch", "searh", "seerch", "search",
                    "claims", "clais", "caims",
                    "all", "al"
                };

                filter = new vw_Claim_Dashboard();
                Data = NavString;
            }

            public override bool FeatureMatch()
            {
                return Alias.Contains((Data ?? "").ToLower());
            }

            public override void IdSearch(object Id)
            {
                if(Id == null) return;

                int idVal = -1;

                if (int.TryParse(Id.ToString(),out idVal)) // Search By Claim Id (3)
                    filter.ClaimNos = Id.ToString();
                else if (Id.GetType() == typeof(string) && Id.ToString().Contains(',')) // Search By Claim Ids (1,2,3)
                    filter.ClaimNos = Id.ToString();
                else
                    filter.CustRefNo = Id.ToString();                
            }

            /*public override void IdsSearch(object[] Id)
            {
                IdSearch(string.Join(",",Id));
            }*/

            public override void Search(object filters)
            {
                if(filters == null) return;
                //Try to split by ','  then by ' ' 
                string[] filterArr = filters.ToString().Contains(',') ?
                    filters.ToString().Split(new char[] { ',' }) : filters.ToString().Split(new char[] { ' ' });

                string[] data;
                foreach (string fltr in filterArr)
                {
                    if (!fltr.Contains('=')) continue;
                    try
                    {
                        data = fltr.Split(new char[] { '=' });
                        setProperty(data[0], data[1]);
                    }
                    catch (Exception ex) { continue; }
                }
            }

            void setProperty(string prop, string val)
            {
                prop = TryMapProp(prop);

                System.Reflection.PropertyInfo pi =
                    // http://stackoverflow.com/questions/264745/bindingflags-ignorecase-not-working-for-type-getproperty
                filter.GetType().GetProperty(prop, System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

                if (pi != null)
                {
                    if (pi.PropertyType == typeof(string)) val = "%" + val + "%";
                    pi.SetValue(filter, val.Trim(), null);
                }
            }

            string TryMapProp(string prop)
            { 
                string[] oriProp = new string[]
                {"Archived", "ClaimNos", "CustRefNo", "BrandName","Status","CustOrg","Salesperson","ShipToLoc"};
                string[] AliasProp = new string[]{
                    "archived claims;archived;",
                    "claim;claim#;claim #;#;claimno;claimnos;claimnumber;claim number;",
                    "custrefno;custref#;customerrefno;cust ref no;customer reference number;customer ref #;customerreferencenumber;",
                    "brandname;brand;brand name;",
                    "status;statusname;status name;",
                    "custorg;org;customer;cust org;customer org;",
                    "salesperson;spsn;sales person;sales;",
                    "shiptoloc;location;shiptolocation;loc;ship to loc;shipping;shipping loc;shipping location;"};

                int i = 0;
                foreach(string str in AliasProp)
                    if(str.Contains(prop.Trim().ToLower()+";"))
                        return oriProp[i];
                    else
                        i++;

                return prop;
            }
        }

        public class ClaimNav : Feature
        {
            public int ClaimId { get; set; }
            public ClaimNav(string NavString)
            {
                Alias = new string[]
                {
                    "claim","claimentry", "claim entry"                    
                };

                Data = NavString;
            }

            public override bool FeatureMatch()
            {
                return Alias.Contains((Data ?? "").ToLower());
            }

            public override void IdSearch(object Id)
            {
                if (Id == null) return;

                int idVal = -1;

                if (int.TryParse(Id.ToString(), out idVal)) // Search By Claim Id (3)
                    ClaimId = idVal;                
            }            

            public override void Search(object filters)
            {
                if (filters == null) return;

                string[] data;

                if (!filters.ToString().Contains('=')) 
                try
                {
                    data = filters.ToString().Split(new char[] { '=' });
                    
                    int idVal = -1;
                    if (int.TryParse(data[1], out idVal)) // Search By Claim Id (3)
                        ClaimId = idVal;                                    
                }
                catch (Exception ex) { }
            }
        }

        public class UserNav : Feature
        {
            public vw_Users_Role_Org filter { get; set; }
            public UserNav(string NavString)
            {
                Alias = new string[]
                {
                    "user","users","usersearch","user search","userlist","user list"                    
                };

                filter = new vw_Users_Role_Org();
                Data = NavString;
            }

            public override bool FeatureMatch()
            {
                return Alias.Contains((Data ?? "").ToLower());
            }

            public override void IdSearch(object Id)
            {
                if (Id == null) return;

                int idVal = -1;

                if (int.TryParse(Id.ToString(), out idVal)) // Search By User Id (3)
                    filter.ID = idVal;
                else
                    filter.UserName = Id.ToString();
            }

            public override void Search(object filters)
            {
                if (filters == null) return;
                //Try to split by ','  then by ' ' 
                string[] filterArr = filters.ToString().Contains(',') ?
                    filters.ToString().Split(new char[] { ',' }) : filters.ToString().Split(new char[] { ' ' });

                string[] data;
                foreach (string fltr in filterArr)
                {
                    if (!fltr.Contains('=')) continue;
                    try
                    {
                        data = fltr.Split(new char[] { '=' });
                        setProperty(data[0], data[1]);
                    }
                    catch (Exception ex) { continue; }
                }
            }

            void setProperty(string prop, string val)
            {
                prop = TryMapProp(prop);

                System.Reflection.PropertyInfo pi =
                    // http://stackoverflow.com/questions/264745/bindingflags-ignorecase-not-working-for-type-getproperty
                filter.GetType().GetProperty(prop, System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

                if (pi != null)
                {
                    if (pi.PropertyType == typeof(string)) val = "%" + val + "%";
                    pi.SetValue(filter, val.Trim(), null);
                }
            }

            string TryMapProp(string prop)
            {
                string[] oriProp = new string[] { "UserName", "Email", "RoleID", "RoleName", "OrgName", "Country", "SalespersonCode" };
                string[] AliasProp = new string[]{
                    "username;user name;name;",
                    "email;emailid;useremail;user email;",
                    "roleid;role id;",
                    "rolename;role;role name;",
                    "orgname;org name;org;organization;",
                    "country;contry;cuntry;",
                    "salesperson;spsn;sales person;sales;salespersoncode;sales person code;spsncode;spsn code;"
                };

                int i = 0;
                foreach (string str in AliasProp)
                    if (str.Contains(prop.Trim().ToLower() + ";"))
                        return oriProp[i];
                    else
                        i++;

                return prop;
            }
        }
    }
}
