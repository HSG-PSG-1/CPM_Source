using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace CPM.DAL
{
    public partial class ClaimInternalPrint
    {
        public vw_Claim_Master_User_Loc view;
        public List<Comment> comments;
        public List<FileHeader> filesH;
        public List<ClaimDetail> items;
    }
}

