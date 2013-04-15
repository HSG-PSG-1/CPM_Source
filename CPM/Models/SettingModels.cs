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
    public partial class MasterSetting
    {
        public string LastModifiedByVal { get; set; }
        [Required]
        public SettingVal SettingValue { get; set ; }

        [MetadataType(typeof(SettingValMetadata))]
        public class SettingVal {
            public string val { get; set; }
            public string setting { get; set; }            
            public SettingService.settings settingEnum
            {
                get
                {
                    try
                    {
                        return _Enums.ParseEnum<SettingService.settings>(setting.Trim());
                    }catch(Exception ex)
                    {return SettingService.settings.Empty;}
                }
            }
            //public int io { get; set; }
        }

        public class SettingValMetadata
        {
            [Required(ErrorMessage = "Setting value required.")]
            public string val { get; set; }            
        }
    }
}