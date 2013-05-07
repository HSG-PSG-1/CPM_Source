using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using CPM.DAL;
using CPM.Models;
using CPM.Services;
using System.Web.Security;

namespace CPM.Helper
{
    public static class _SessionLookup
    {
        public static IEnumerable UserRoles
        {
            get
            {
                object data = HttpContext.Current.Session["UserRoles1"];
                try
                {
                    if (string.IsNullOrEmpty(data.ToString()))
                        return null;
                    return (IEnumerable)(data);
                }
                catch { return null; }
            }
            set { HttpContext.Current.Session["UserRoles1"] = value; }
        }

        public static MasterData MasterData { get { return new MasterData(); } }

        //public static LookupData<T> LookupData<T>() { return new LookupData<T>(); }

        public static LookupData LookupData() { return new LookupData(); }
    }

   public class MasterData
    {
        public List<Master> this[MasterService.Table tbl]
        {
            get
            {
                object data = HttpContext.Current.Session["MasterData_" + (int)tbl];
                try
                {
                    if (string.IsNullOrEmpty(data.ToString()))
                        return null;
                    return (List<Master>)(data);
                }
                catch { return null; }
            }
            set
            {
                if (value != null && value.Count > 0)
                    HttpContext.Current.Session["MasterData_" + (int)tbl] = value;
            }
        }

        public void Remove(MasterService.Table tbl)
        {
            HttpContext.Current.Session.Remove("MasterData_" + (int)tbl);
        }
    }
    /*
   public class LookupData<T>
   {
       public IQueryable<T> this[LookupService.Source src]
       {
           get
           {
               object data = HttpContext.Current.Session["LookupData_" + (int)src];
               try
               {
                   if (string.IsNullOrEmpty(data.ToString()))
                       return null;
                   return (IQueryable<T>)(data);
               }
               catch { return null; }
           }
           set
           {
               if (value != null && value.ToList().Count > 0)
                   HttpContext.Current.Session["LookupData_" + (int)src] = value;
           }
       }
   }
    */
   public class LookupData
   {
       public IEnumerable this[LookupService.Source src]
       {
           get
           {
               object data = HttpContext.Current.Session["LookupData_" + (int)src];
               try
               {
                   if (string.IsNullOrEmpty(data.ToString()))
                       return null;
                   return (IEnumerable)(data);
               }
               catch { return null; }
           }
           set
           {
               if (value != null)// && value.ToList().Count > 0)
                   HttpContext.Current.Session["LookupData_" + (int)src] = value;
           }
       }

       public void Remove(LookupService.Source src)
       {
           HttpContext.Current.Session.Remove("LookupData_" + (int)src);
       }
   }
}