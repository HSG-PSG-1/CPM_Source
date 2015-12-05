using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using System.Text.RegularExpressions;
using CPM.Services;
using CPM.Helper;

namespace CPM.DAL
{
    public partial class UserLocation
    {
        public bool WasLinked { get; set; }// True if already linked
        public bool IsLinked { get; set; }// True if linked by user
        public string Location { get; set; }
        public string LocCode { get; set; }
        public string LocAndCode { get { return Common.getLocationAndCode(this.Location, this.LocCode); } }
        
        public bool IsAdded { get { return !WasLinked && IsLinked; } }
        public bool IsDeleted { get { return WasLinked && !IsLinked && (this.ID != null); } }
    }
    
    [DuplicateMatchAttribute(DuplicateMatchAttribute.Target.User, DuplicateMatchAttribute.Field.Email,
        "Email", "EmailOLD", ErrorMessage = "Duplicate '{0}' found.")]
    /* url: Custom-class-level-attributes - 
     * http://weblogs.asp.net/peterblum/archive/2009/12/07/the-customvalidationattribute.aspx 
     * SO: 2280539/custom-model-validation-of-dependent-properties-using-data-annotations*/
    [MetadataType(typeof(UserMetadata))]
    public partial class Users 
    {//Extra properties
        public string EmailOLD { get; set; } /* Extra property used to check whether value is changed or not */
        public int OriOrgId { get; set; }
        public bool OrgIdChanged { get { return (OriOrgId > 0 && OrgID != OriOrgId); } }
        public string LastModifiedByVal { get; set; }
        public string OrgName { get; set; }
        public string OrgTypeName { get; set; }
        public int OrgType { get; set; }//HT:Careful!
        public bool showLocations { get { return (OrgType == (int)OrgService.OrgType.Customer && OrgID > 0); } }
        public bool isSalesperson { get { return (OrgType == (int)SecurityService.Roles.Sales); } }

        public const string chkDelRefMsg = "User cannot be deleted because he's linked with atleast one of the following:"+
            "<ul><li>A Claim is assigned to this user.</li></ul>";
    }

    public class UserMetadata
    {
        [StringLength(50, ErrorMessage = Defaults.MaxLengthMsg)]
        [DisplayName("Name")]
        [Required]
        public string Name { get; set; }
        
        [DisplayName("Org Type")]
        [Required(ErrorMessage = "Organization type"+Defaults.RequiredMsgAppend)]
        public int OrgType { get; set; }

        [DisplayName("Organization")]
        [Required(ErrorMessage = "Organization" + Defaults.RequiredMsgAppend )]
        public int OrgID { get; set; }

        [DisplayName("Role")]
        [Required(ErrorMessage = "Role" + Defaults.RequiredMsgAppend)]
        public int RoleID { get; set; }
        
        [StringLength(80, ErrorMessage = Defaults.MaxLengthMsg)]
        [DisplayName("Email")]
        [Required(ErrorMessage = Defaults.RequiredMsg)]
        [RegularExpression(Defaults.EmailRegEx, ErrorMessage = Defaults.InvalidEmailMsg)]
        /* OLD Kept for ref
         * [UniqueAttribute(UniqueAttribute.Target.User, UniqueAttribute.Field.UserEmail, ErrorMessage = 
         * "Duplicate Email address found.")]
         * [EmailAddress(ErrorMessage = "Valid Email Address is required.")]*/
        /* Use RegEx because it allows client-side validation */
        public string Email { get; set; }

        [StringLength(20, ErrorMessage=Defaults.MaxLengthMsg)]
        [DisplayName("Password")]
        [Required(ErrorMessage = Defaults.RequiredMsg )]
        public string Password { get; set; }
                
        [DisplayName("Salesperson Code")]
        [StringLength(6, ErrorMessage = Defaults.MaxLengthMsg)]
        public int SalespersonCode { get; set; }

        [DisplayName("Last Modified By")]
        public string LastModifiedBy { get; set; }

        [DisplayName("Last Modified Date")]
        public DateTime LastModifiedDate { get; set; }
    }

    [Serializable]
    public partial class vw_Users_Role_Org
    {
        public bool Editing { get; set; }
        public bool Edited { get; set; }

        public string LastModifiedDateTxt { get { return LastModifiedDate.ToString(Defaults.dtTFormat, Defaults.ci); } }
    }
}
