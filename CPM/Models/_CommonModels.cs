using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Collections;
using System.Collections.Generic;
using CPM.Helper;
using System.Runtime.Serialization;

namespace CPM.Models
{
    #region Ad-hoc Models
    public class LogInModel
    {// Log in model used to capture user data
        [Required(ErrorMessage = Defaults.RequiredMsg)]
        [DisplayName("Email")]
        [StringLength(80, ErrorMessage = Defaults.MaxLengthMsg)]
        public string Email { get; set; }

        [Required(ErrorMessage = Defaults.RequiredMsg)]
        [DataType(DataType.Password)]
        [DisplayName("Password")]
        [StringLength(20, ErrorMessage = Defaults.MaxLengthMsg)]
        public string Password { get; set; }

        [DisplayName("Remember me")]
        public bool RememberMe { get; set; }
    }
    #endregion

    public class Lookup
    {// Used by dropdown type of lookup fields
        public string id { get; set; }
        public string value { get; set; }
    }

    [Serializable]
    public class PRGModel
    {
        public string viewModelObj { get; set; }
        public IEnumerable modelErrors { get; set; }//System.Collections.IEnumerable

        public PRGModel() {;}
        public PRGModel(object prgData)
        {
            if (prgData != null)
                try
                {
                    this.viewModelObj = (prgData as PRGModel).viewModelObj;
                    this.modelErrors = (prgData as PRGModel).modelErrors;
                }
                catch (Exception ex) { ;}
        }
        public PRGModel SetPRGModel<T>(T modelToPass, System.Web.Mvc.ModelStateDictionary modelState)
        {
            viewModelObj = Serialization.Serialize<T>(modelToPass);
            modelErrors = modelState.Errors();//.Errors(); // Extension method  needed because "ModelStateDictionary" is too deep to serialize!
            return this;
        }
        
        public T GetObject<T>()
        {
            return Serialization.Deserialize<T>(viewModelObj);
        }

        public void ExtractData<T>(ref T modelData, System.Web.Mvc.ModelStateDictionary modelState)
        {
            modelData = Serialization.Deserialize<T>(viewModelObj);
            // Iterate thru each errors and populate
            foreach (KeyValuePair<string, List<string>> err in ((KeyValuePair<string, List<string>>[])modelErrors))
                foreach (string er in err.Value) // There might be multiple errors related to one field!
                    modelState.AddModelError(err.Key, er);
        }
    }
}

namespace CPM.DAL
{
    public class Common
    {
        public static string getLocationAndCode(string Location, string Code)
        {
            try { return Location + " (" + Code.Substring(Config.CustCodeLenInLocCode) + ")"; }
            catch { return Location ?? ""; }
        }
    }

    [Serializable]
    public partial class MasterRole { }
}