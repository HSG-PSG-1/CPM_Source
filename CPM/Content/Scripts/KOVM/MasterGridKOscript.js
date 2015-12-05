var IsCCEditMode = false;
var NextNewrecordID = 0;
var recordsViewModel = function () {
    var self = this;
    self.emptyrecord = "";

    self.allRecords = ko.observableArray();
    self.newRecord = ko.observable();
    
    self.setEdited = function (record) {
        record._Updated(!record._Added());
        record.LastModifiedDate(Date111); 
    }
    
    self.setEditedFlag = function (record) {
        record._Updated(!record._Added());
        record.LastModifiedDate(Date111);
        
        var titl = record.Title();
        if (titl == "") { showNOTY("This field is required", false); }
        else if($.grep(self.allRecords(), function(el)
                {
                    return (!el._Deleted() && el.Title().toLowerCase() === titl.toLowerCase());}
               ).length > 1)
        { showNOTY("Duplicate entry found", false); }
        else
           { record.TitleOLD(titl); return;}

        record.Title(record.TitleOLD()); // rollback
    }
    self.addNewRecord = function () {
        var ID = self.newRecord.ID();
        self.newRecord.ID(NextNewrecordID);

        self.newRecord.Title(self.newRecord.Title().replace(ID, NextNewrecordID-1));
        self.newRecord.TitleOLD(self.newRecord.Title());
        self.allRecords.push((cloneObservable(self.newRecord)));

        NextNewrecordID = NextNewrecordID - 1;
        self.newRecord.ID(NextNewrecordID);        
        
        $("#sortable").tableNav(); // for newly created TR
        setFocusEditableGrid("sortable", false);
        return true;
    };
    
    self.removeSelected = function (record) {
        if (record != null)
        {
            record._Deleted(true);
            if (record._Added()) {                
                self.allRecords.remove(record);
                //record._Added(false);
            }
        }
    };

    self.unRemoveSelected = function (record) {
        if (record != null) // Prevent blanks and duplicates
            record._Deleted(false);        
    };

    self.cancelrecord = function (record) {
        IsCCEditMode = false;        
        self.recordToAdd(cloneObservable(self.emptyrecord));
    };

    self.saveToServer = function () {
    var jsonData = ko.mapping.toJSON({ "records" : ko.mapping.toJS(self.allRecords) });
    $("input[type=button]").attr('disabled','disabled');
        $.ajax({ // SO: 12846689
            url: location.href,
            type: "POST",
            dataType: "json",
            //processData: false,
            contentType: "application/json; charset=utf-8",
            data: jsonData,
            success: function(data) {
                if(data != null && data.length > 0)
                {
                    showNOTY(data, false);
                    $("input[type=button]").removeAttr('disabled');
                }
                else
                {   //alert("Operation was successful"); 
                    window.location.href = window.location.href; // Refresh page
                }
                $("input[type=button]").removeAttr('disabled');
            }
        });
        // ko.utils.postJson(location.href, jsonData ); //{ records: ko.mapping.toJS(self.allRecords) });
        return false;
    }
};
var vmMaster = new recordsViewModel();
function createRecordsKO()
{
    showDlg(true);
    $.getJSON(manageKOVMURL + '?masterTbl=' + getSelMaster(),
        function (data) {
            showDlg(false);
             var newRec;
             if(data != null && data.length > 0)
             { newRec = data.pop(); newRec._Added = true; }
            
            vmMaster.allRecords = ko.mapping.fromJS(data);// Make sure the last record is removed
            vmMaster.newRecord = ko.mapping.fromJS(newRec);            
            ko.applyBindings(vmMaster);

            $("#sortable").tableNav();
        });
}

$(document).ready(function () {
    createRecordsKO();    
});

function getSelMaster()
{
    return $("#ddlMaster").children("option").filter(":selected").text();
}