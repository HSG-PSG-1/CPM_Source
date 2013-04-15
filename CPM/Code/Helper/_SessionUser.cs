using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using CPM.DAL;
using CPM.Models;
using CPM.Services;
using System.Web.Security;

namespace CPM.Helper
{
    public static class _SessionUsr
    {
        public static void setUserSession(vw_Users_Role_Org data)
        {
            ID = data.ID;
            RoleID = data.RoleID;
            OrgID = data.OrgID;
            SalespersonCode = data.SalespersonCode;
            UserName = data.UserName;
            Email = data.Email;
            RoleName = data.RoleName;
            OrgTypeId = data.OrgTypeId??Defaults.Integer;
            OrgType = data.OrgType;
            OrgName = data.OrgName;
            Country = data.Country;
        }

        public static int ID
        { get { try{ return int.Parse(HttpContext.Current.Session["UsrID"].ToString());}
                catch(Exception ex){ return Defaults.Integer; } }
            set{HttpContext.Current.Session["UsrID"] = value;}
        }

        public static int RoleID
        { get { try{ return int.Parse(HttpContext.Current.Session["UsrRoleID"].ToString());}
                catch(Exception ex){ return Defaults.Integer; } }
            set{HttpContext.Current.Session["UsrRoleID"] = value;}
        }

		public static int OrgID
        { get { try{ return int.Parse(HttpContext.Current.Session["UsrOrgID"].ToString());}
                catch(Exception ex){ return Defaults.Integer; } }
            set{HttpContext.Current.Session["UsrOrgID"] = value;}
        }		
		
        public static string SalespersonCode
        { get {  return (HttpContext.Current.Session["UsrSalespersonCode"]??"").ToString(); }
          set {  HttpContext.Current.Session["UsrSalespersonCode"] = value;}
        }

		public static string UserName
        { get {  return (HttpContext.Current.Session["UsrUserName"]??"").ToString(); }
          set {  HttpContext.Current.Session["UsrUserName"] = value;}
        }
		
        public static string Email
        { get {  return (HttpContext.Current.Session["UsrEmail"]??"").ToString(); }
          set {  HttpContext.Current.Session["UsrEmail"] = value;}
        }

		public static string RoleName
        { get {  return (HttpContext.Current.Session["UsrRoleName"]??"").ToString(); }
          set {  HttpContext.Current.Session["UsrRoleName"] = value;}
        }

        public static int OrgTypeId
        { get { try{ return int.Parse(HttpContext.Current.Session["UsrOrgTypeId"].ToString());}
                catch(Exception ex){ return Defaults.Integer; } }
            set{HttpContext.Current.Session["UsrOrgTypeId"] = value;}
        }
		
		public static string OrgType
        { get {  return (HttpContext.Current.Session["UsrOrgType"]??"").ToString(); }
          set {  HttpContext.Current.Session["UsrOrgType"] = value;}
        }
		
		public static string OrgName
        { get {  return (HttpContext.Current.Session["UsrOrgName"]??"").ToString(); }
          set {  HttpContext.Current.Session["UsrOrgName"] = value;}
        }
		
		public static string Country
        { get {  return (HttpContext.Current.Session["UsrCountry"]??"").ToString(); }
          set {  HttpContext.Current.Session["UsrCountry"] = value;}
        }
		
        // Unused properties
        //private string Password;
		//private int LastModifiedBy;		
		//private System.DateTime LastModifiedDate;		
		//private string LastModifiedByName;
    }
}