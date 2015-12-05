using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using CPM.DAL;

namespace CPM.Services
{
    //HT: IMP: http://www.sidarok.com/web/blog/content/2008/05/02/10-tips-to-improve-your-linq-to-sql-application-performance.html
    public abstract class _ServiceBase
    {
        #region Variables & Constructor

        public const int insertNewId = -1;
        public CPMmodel dbc;
        public static string delRefChkMsg = "Entity being deleted is referred by another entity. So cannot delete it.";

        public _ServiceBase()
        {
            if (dbc == null)
            {
                dbc = new CPMmodel();
                /*dbc = new CPMmodel(new StackExchange.Profiling.Data.ProfiledDbConnection
                    (dbc.Connection, StackExchange.Profiling.MiniProfiler.Current)) { DeferredLoadingEnabled = true };*/
            }
            //http://blogs.microsoft.co.il/blogs/bursteg/archive/2007/10/06/linq-to-sql-deferred-loading-lazy-load.aspx
            //http://dotnetslackers.com/articles/csharp/Load-Lazy-in-LINQ-to-SQL.aspx
        }

        public _ServiceBase(CPMmodel dbcExisting)
        {//Special case because even using(dbc) caused renewal of dbc from within
            dbc = dbcExisting;
        }

        #endregion

        #region Extra functions

        /// <summary>
        /// Check if object is being referred by any other entity (NOTE: Meant to be overridden in most of the cases)
        /// </summary>
        /// <param name="oObj">object to be checked</param>
        /// <returns>true if object is being referred by atleast one entity</returns>
        public virtual bool IsReferred(Object oObj) { return true; }

        #endregion
    }
}
