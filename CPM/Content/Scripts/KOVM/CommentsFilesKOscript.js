var IsCCEditMode = false;
var NextNewCommentID = -1;
var commentsViewModel = function ()
{
    var self = this;
    self.emptyComment = "";

    self.commentToAdd = ko.observable();
    self.allComments = ko.observableArray();
    self.AssignTo = ko.observable();
    self.AssignToVal = ko.observable();

    self.setEdited = function (comment) {
        comment._Edited(!comment._Added());
        comment.PostedOn(Date111);
    };
    self.setEditedFlag = function (comment) {
        comment._Edited(!comment._Added());
        comment.LastModifiedDate(Date111);
        comment.PostedOn(Date111);

        /*var index = self.allComments.indexOf(comment);
        self.allComments.remove(comment);
        self.allComments.splice(index, 0, comment);*/
    };
    self.addComment = function (comment) {
        if (comment.Comment1() == null || comment.Comment1 == "") {/*http://knockoutjs.com/documentation/event-binding.html */
            showNOTY("Comment is a required field", false);
            return false;
        }
        else {
            if (!IsCCEditMode) {
                //comment.ID(NextNewCommentID);
                self.allComments.push(cloneObservable(comment));
                //self.allComments.push(ko.mapping.fromJS(cloneObservable(comment)));

                self.sendEmailPost(comment);

                NextNewCommentID = NextNewCommentID - 1;
                self.emptyComment.ID = NextNewCommentID; // NOT WORKING as expected

                self.cancelComment(comment);//commentToAdd(cloneObservable(self.emptyCommment));
            }
            else {
                //HT: if observable is set correctly nothing needs to be done
                /*var index = self.allComments.indexOf(comment);
                self.allComments.remove(comment);
                self.allComments.splice(index, 0, comment);*/
                ;
            }
            $('#tblComments').tableNav(); // for newly created TR
            setFocusEditableGrid("tblComments", false);
        }
        return true; /* because we need to ajax submit the form */
    };

    self.removeSelected = function (comment) {
        if (comment != null) {
            comment._Deleted(true);
            if (comment._Added()) {
                comment._Added(false);
                self.allComments.remove(comment);
            }
        }
    };

    self.unRemoveSelected = function (comment) {
        if (comment != null) // Prevent blanks and duplicates
        {
            comment._Deleted(false);
        }
    };


    self.cancelComment = function (comment) {
        IsCCEditMode = false;
        self.commentToAdd.ID(self.emptyComment.ID); //NextNewCommentID
        self.commentToAdd.Comment1(self.emptyComment.Comment1);
        // WON'T work !self.commentToAdd = ko.mapping.fromJS(self.emptyComment);        
    };

    self.sendEmailPost = function (comment) {
        var _AssignedTo = $("#AssignedTo").val();
        var _ClaimNo = $("#ClaimNo").val();
        var proceed = false;
        proceed = !(comment == null || _AssignedTo == null || _ClaimNo == null);

        if (proceed) {
            $.post(commentsEmailURL, /* 'Url.Action("CommentsKOEmail", "Claim", new { ClaimGUID = ViewData["ClaimGUID"] })', */
                    {
                        CommentObj: ko.mapping.toJSON(comment),
                        AssignedTo: _AssignedTo,
                        ClaimNo: _ClaimNo
                    },
                    function (result) {
                        //alert(result); HT: we can notify user if a successful email was sent
                        showNOTY(result.msg, result.sendMail);
                        if (result.sendMail) {
                            $("#AssignToOLD").val(_AssignedTo).trigger("change");
                        }
                    }
            );
        }
    };

    self.saveToServer = function () {
        ko.utils.postJson(location.href, { comments: ko.mapping.toJS(self.allComments) }); //ko.toJSON(self.allComments)
        return false;
    };

    /*http://jsfiddle.net/rniemeyer/GgFa9/
    self.jsonText = ko.computed(function() {
    return JSON.stringify($.map(self.allComments(), function (comment) { return comment.val(); }));
    });*/
};
var viewModelComments = new commentsViewModel();

function createCommentsKO(comments, assignTo)//callDocReadyComments()
{
    var ClaimAssignedTo = $("#AssignedTo").val();
    /*$.getJSON('@Html.Raw(Url.Action("CommentsKOVM", "Claim", new { ClaimGUID = ViewData["ClaimGUID"] }))' +  '&AssignedTo=' + ClaimAssignedTo ,
        function (data) {*/
    if (comments.CommentToAdd.ID != -1)
        comments.CommentToAdd.ID = NextNewCommentID;

    viewModelComments.emptyComment = comments.EmptyComment; /* THIS SHUD NOT BE AN OBSERVABLE */
    viewModelComments.AssignTo(comments.AssignedTo); $("#AssignToOLD").val(comments.AssignedTo);
    viewModelComments.AssignToVal("");

    viewModelComments.commentToAdd = ko.mapping.fromJS(comments.CommentToAdd);
    viewModelComments.allComments = ko.mapping.fromJS(comments.AllComments); //, mapping);
    viewModelComments.Users = ko.mapping.fromJS(comments.Users);

    ko.applyBindings(viewModelComments, document.getElementById("divComments"));
    //});    
}

/*function doCmtDelPost(comment) {
    var data = {}; data[txtId] = txtVal;
    var url = commentDeleteURL;
    $.post(url, data);
return false; // prevent any postback
}*/

/* HT:CAUTION - using document.ready causes duplicate call which replicates the binding! */
function setAssignedTo(ddl) {
    
    /*var ddlID = $(ddl).val();
    var ddlTXT = $(ddl).children("option").filter(":selected").text();
    $(ddl).parent().children("input:first").val(ddlTXT).trigger("change");*/

    setDDLtext(ddl, 'AssignToVal');
    $("#AssignedTo").val($(ddl).val()).trigger("change");
    
    //$("#AssignedToVal").val(ddlTXT).trigger("change");
}

/* -------------------------- */
var IsFHEditMode = false;
var NextNewFileID = -1;

var filesHeaderModel = function () {
    var self = this;

    self.emptyFile = "";

    self.fileToAdd = ko.observable();
    self.allFiles = ko.observableArray(); // Initial items

    self.TriggerOpenWin = function (fileFH) {
        openWin(FileGetURL + fileFH.CodeStr(), 1, 1);
    }

    self.setEdited = function (file) {
        file._Edited(!file._Added()); //alert(ko.mapping.toJSON(ko.mapping.fromJS(new Date())()));
        file.UploadedOn(Date111); //Date111 ko.mapping.fromJS(new Date())
    }
    self.setEditedFlag = function (file) {
        file._Edited(!file._Added());
        file.LastModifiedDate(Date111);
    }
    self.addFile = function (file) {
        if (file.FileName == null || file.FileName == "") {
            //http://knockoutjs.com/documentation/event-binding.html
            showNOTY("Please select a file to upload", false); return false;
        }
        else {
            /* SO: 857618/javascript-how-to-extract-filename-from-a-file-input-control */
            file.FileName = file.FileName.split(/(\\|\/)/g).pop();
            /* ABOVE DOESN'T WORK FOR ALL VERSIONS OF IE - so NEED  to handle it on SERVER SIDE */
            if (!IsFHEditMode) {
                file.ID = NextNewFileID;
                self.allFiles.push(ko.mapping.fromJS(cloneObservable(file)));
                NextNewFileID = NextNewFileID - 1;
                self.emptyFile.ID = NextNewFileID; // NOT WORKING as expected
            }
            else { /* Editmode Handled by KO */; }
            $('#tblFilesH').tableNav(); // for newly created TR
            setFocusEditableGrid("tblFilesH", false);
        }
        return true; // for ajax submit
    };

    self.removeSelected = function (file) {
        if (file == null) return false;

        if (!file._Added()) { file._Deleted(true); return false; }

        var data = {}; data["delFH"] = ko.mapping.toJSON(file);
        $.post(FileDeleteURL, data, function (data, textStatus, jqXHR) {
            file._Deleted(true);
            file._Added(false);
            self.allFiles.remove(file);
        }
        );
    };

    self.unRemoveSelected = function (file) {
        if (file != null) {
            file._Deleted(false);
        }
    };

    self.cancelFile = function (file) {
        IsFHEditMode = false;
        self.fileToAdd(cloneObservable(self.emptyFile));
    };

    self.saveToServer = function () {
        ko.utils.postJson(location.href, { files: ko.mapping.toJS(self.allFiles) }); //ko.toJSON(self.allFiles)
        return false;
    }
};

var viewModelFH = new filesHeaderModel();

function createFilesHeaderKO(data) {    
    if (data.FileToAdd.ID != -1) data.FileToAdd.ID = NextNewFileID;

    viewModelFH.emptyFile = data.EmptyFileHeader; // THIS SHUD NOT BE AN OBSERVABLE
    viewModelFH.fileToAdd(data.FileToAdd);

    if (data.AllFiles != null)
        viewModelFH.allFiles = ko.mapping.fromJS(data.AllFiles);
    else
        viewModelFH.allFiles = ko.observableArray();

    viewModelFH.FileTypes = ko.mapping.fromJS(data.FileTypes);
    ko.applyBindings(viewModelFH, document.getElementById("divFiles"));
}