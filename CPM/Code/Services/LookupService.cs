using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic;
using System.Data.Linq.SqlClient;
using CPM.DAL;
using CPM.Helper;
using CPM.Models;
using Webdiyer.WebControls.Mvc;

namespace CPM.Services
{
    public class LookupService : _ServiceBase
    {
        #region Variables
        
        public enum Source
        {
            Customer = 1,
            Internal,
            Vendor,//Make sure Org sequence and value is same as OrgService.OrgType
            Brand,
            BrandItems,
            BrandVendor,
            BrandVendorItems,
            Item,
            Item1,
            Status,
            ShipLoc,
            User,
            OrgSalesperson,
            Salesperson,
            FileHeader,
            FileDetail,
            Defect,
            CustLocations,
            Org,
            OrgType,
            Error_Detail_Level
            //,DefectGrouped
        }
        
        #endregion

        #region Overloads ( Kept for ref)
        /*
        public System.Collections.IEnumerable GetLookup(Source src)
        { return  GetLookup(src, null, null,true);}
        public System.Collections.IEnumerable GetLookup(Source src, bool addEmpty)
        { return GetLookup(src, null, null, addEmpty); }
        public System.Collections.IEnumerable GetLookup(Source src, string term)
        { return GetLookup(src, term, null, true); }
        public System.Collections.IEnumerable GetLookup(Source src, string term, string extras)
        { return GetLookup(src, term, extras, true); }
        */
        #endregion

        /// <summary>
        /// Get lookup based on source passed and search for term
        /// </summary>
        /// <param name="src">LookupService.Source enum value</param>
        /// <param name="term">Term to be searched as contains search</param>
        /// /// <param name="extras">Any extra data passed (required for special cases like Item1)</param>
        /// <returns>IEnumerable LINQ query</returns>       
        public IQueryable GetLookup(Source src, bool addEmpty = true, string term = null, string extras=null)
        {
            IQueryable results;
            bool noTerm = string.IsNullOrEmpty(term), noExtra = (string.IsNullOrEmpty(extras) || int.Parse(extras) <= Defaults.Integer);

            term = (term ?? "").ToLower();

            switch (src)
            {
                #region Orgs,OrgType, Item & Brand

                case Source.Org:
                    //src = noTerm ? Source.Customer : _Enums.ParseEnum<LookupService.Source>(extras);
                    results = new OrgService().GetOrgsByRoleId(int.Parse(extras),term);//GetOrgs(src, term);
                    break;
                case Source.OrgType:
                    results = from o in dbc.MasterOrgTypes
                              orderby o.Title
                              select new { id = o.ID, value = o.Title };
                    break;
                case Source.Customer:
                case Source.Internal:
                case Source.Vendor:
                    results = new OrgService().GetOrgs(src, term);    
                break;
                case Source.Item://HT: Almost obsolete for now
                    results = from i in dbc.MasterInventories
                              where (noTerm || i.ItemNo.ToLower().Contains(term))
                              orderby i.ItemNo
                              select new { id = i.ID, value = i.ItemNo };
                    break;
                case Source.Item1:
                    results = from i in dbc.MasterInventories
                              where (noTerm || i.ItemNo.ToLower().Contains(term) || i.Description.ToLower().Contains(term))
                               && (noExtra || i.BrandId == int.Parse(extras))
                              orderby i.ItemNo
                              select new { id = i.ID, value = i.ItemNo, descr = i.Description, tdo = i.TDOriginal, cc = i.CurrentCost, cp = i.CurrentPrice };
                    break;
                case Source.Brand:
                    results = from b in dbc.MasterBrands
                              where (noTerm || b.Title.ToLower().Contains(term))
                              orderby b.Title//not SortOrder because its not set during import
                              select new { id = b.ID, value = b.Title };

                    break;
                case Source.BrandItems:
                    results = from i in dbc.vw_Brand_Items
                              where (noTerm || i.Title.ToLower().Contains(term))
                              orderby i.Title//not SortOrder because its not set during import
                              select new { id = i.ID, value = i.Title + " (" + i.Items.Value.ToString() + ")" };

                    break;
                case Source.BrandVendor:
                    results = from b in dbc.MasterBrands
                              where (noTerm || b.Title.ToLower().Contains(term)) &&
                              (noExtra || b.VendorID == int.Parse(extras))
                              orderby b.Title//not SortOrder because its not set during import
                              select new { id = b.ID, value = b.Title };

                    break;
                case Source.BrandVendorItems:
                    results = from b in dbc.vw_Brand_Items
                              where (noTerm || b.Title.ToLower().Contains(term)) &&
                              (noExtra || b.VendorID == int.Parse(extras))
                              orderby b.Title//not SortOrder because its not set during import
                              select new { id = b.ID, value = b.Title + " (" + b.Items.Value.ToString() + ")" };

                    break;
                case Source.CustLocations:
                    results = from i in dbc.vw_CustOrg_Locs
                              where (noTerm || i.Name.ToLower().Contains(term))
                              orderby i.Name//not SortOrder because its not set during import
                              select new { id = i.ID, value = i.Name };// + " (" + i.Locs.Value.ToString() + ")"
                    break;

                #endregion

                #region Master
                
                case Source.ShipLoc:
                    // Impose Cust Org specific location constraint
                    if(!_Session.SkipCustLocCheck)
                    results = from i in dbc.vw_CustLoc_User_UserLocs
                              where (noTerm || i.Name.ToLower().Contains(term)) && i.OrgID == int.Parse(extras)
                              && i.UserID == _SessionUsr.ID && (_Session.SkipCustLocCheck || i.UsrLocID != null)
                              orderby i.Name select new { id = i.ID, value = Common.getLocationAndCode(i.Name, i.Code) };
                                  //i.Name + " (" + i.Code.Substring(Config.CustCodeLenInLocCode) + ")" };
                    else
                    results = from i in dbc.CustomerLocations 
                              where (noTerm || i.Name.ToLower().Contains(term)) &&  i.CustomerId == int.Parse(extras)
                              orderby i.Name
                              select new { id = i.ID, value = Common.getLocationAndCode(i.Name, i.Code) };
                                  //i.Name + " (" + i.Code.Substring(Config.CustCodeLenInLocCode) + ")" };
                    break;
                case Source.FileHeader:
                    // http://social.msdn.microsoft.com/forums/en-US/linqprojectgeneral/thread/64fc5db3-38d7-41d3-8510-2df9eae2081a/
                    results = (from i in new MasterService(MasterService.Table.File_Type_Header).FetchAllCached() //dbc.MasterFileTypeHeaders
                               where i.ID > Defaults.Integer && // Special case added while fetching from Master service which adds a default new [Title] entry
                               (noTerm || i.Title.ToLower().Contains(term))
                              orderby i.SortOrder
                              select new { id = i.ID, value = i.Title }).AsQueryable();
                    break;
                case Source.FileDetail:
                    results = (from i in new MasterService(MasterService.Table.File_Type_Detail).FetchAllCached() //in dbc.MasterFileTypeDetails
                               where i.ID > Defaults.Integer && (noTerm || i.Title.ToLower().Contains(term))
                              orderby i.SortOrder
                              select new { id = i.ID, value = i.Title }).AsQueryable();
                    break;
                case Source.Defect:
                    results = (from i in new MasterService(MasterService.Table.Defect).FetchAllCached() //dbc.MasterDefects
                               where i.ID > Defaults.Integer && (noTerm || i.Title.ToLower().Contains(term))
                              orderby i.SortOrder
                              select new { id = i.ID, value = i.Title }).AsQueryable();
                    break;
                #region HT: Kept for future ref
                /*
                case Source.DefectGrouped:
                    results = from i in dbc.MasterDefects
                              where (noTerm || i.Title.ToLower().Contains(term))
                              orderby i.Category, i.SortOrder // DON'T forget to SORT by Category !!!
                              select i;
                              //select new { id = i.ID, value = i.Title };
                    break; */
                #endregion
                case Source.Status:
                    results = (from i in new MasterService(MasterService.Table.Claim_Status).FetchAllCached() //dbc.MasterClaimStatus
                               where i.ID > Defaults.Integer && (noTerm || i.Title.ToLower().Contains(term))
                              orderby i.SortOrder
                              select new { id = i.ID, value = i.Title }).AsQueryable();

                    break;                

                #endregion

                #region Users & Salesperson
                case Source.User:
                    results = from i in dbc.Users
                              where (noTerm || i.Name.ToLower().Contains(term)) // i.Email.ToLower().Contains(term) ||
                              orderby i.Name
                              select new { id = i.ID, value = (i.Name??"") };
                    break;
                case Source.Salesperson:
                    results = from i in dbc.Users
                              where i.RoleID == (int)SecurityService.Roles.Sales && 
                              (noTerm || i.Name.ToLower().Contains(term) )
                              orderby i.Name
                              select new{id = i.ID,value = (i.Name ?? "")};
                    break;
                case Source.OrgSalesperson:
                    results = from i in dbc.vw_CustOrg_SalesUsers
                              where  (noTerm || i.Name.ToLower().Contains(term))
                              orderby i.Name
                              select new{id = i.ID,value = i.Name, spName = i.UserName, spid = i.SalespersonId };
                    break;
                #endregion

                #region Others & default
                case Source.Error_Detail_Level:
                    return new List<Lookup>(){new Lookup(){ id = "0", value = "Summary" },
                    new Lookup(){ id = "1", value = "Detailed" }}.AsQueryable();
                    break;

                default: results = null; break;
                #endregion
            }

            #region Handle special case for no-records found
            if (results.Count() < 1 && addEmpty)
            {
                //CAUTION: make sure the select: & focus: functions are correctly configured!
                // Like : http://stackoverflow.com/questions/8663189/jquery-autocomplete-no-result-message
                List<Lookup> empty = new List<CPM.Models.Lookup>(1);
                empty.Add(new CPM.Models.Lookup() { value = "No results" });
                results = (from e in empty select e).AsQueryable();
            }
            #endregion

            return results;

        }

        // HT: Kept for future ref
        // public static T ConvertObj<T>(object obj) { return (T)obj; }
    }
}
