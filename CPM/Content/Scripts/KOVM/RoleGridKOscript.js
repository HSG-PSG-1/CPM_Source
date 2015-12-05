var IsCCEditMode = false;
var NextNewrecordID = 0;
var recordsViewModel = function () {
    var self = this;
    self.emptyrecord = "";

    self.allRecords = ko.observableArray();
    self.newRecord = ko.observable();
    /*//self.jsonText = ko.computed(function() { return ("JSON: " + ko.toJSON(self.recordToAdd())); });*/

    self.setEdited = function (record) {
        record._Updated(!record._Added());
        record.LastModifiedDate(Date111);
    }
    self.setEditedFlag = function (record) {
        record._Updated(!record._Added());
        record.LastModifiedDate(Date111);

        var titl = record.Title();
        if (titl == "") { showNOTY("This field is required", false); }
        else if ($.grep(self.allRecords(), function (el) {
                    return (!el._Deleted() && el.Title().toLowerCase() === titl.toLowerCase());
        }
               ).length > 1)
        { showNOTY("Duplicate entry found", false); }
        else
        { record.TitleOLD(titl); return; }

        record.Title(record.TitleOLD()); // rollback
    }
    self.addNewRecord = function () {
        var ID = self.newRecord.ID();
        self.newRecord.ID(NextNewrecordID);

        self.newRecord.Title(self.newRecord.Title().replace(ID, NextNewrecordID - 1));
        self.newRecord.TitleOLD(self.newRecord.Title());
        self.allRecords.push((cloneObservable(self.newRecord)));

        NextNewrecordID = NextNewrecordID - 1;
        self.newRecord.ID(NextNewrecordID);

        $("#sortable").tableNav(); // for newly created TR
        setFocusEditableGrid("sortable", false);
        return true;
    };

    self.removeSelected = function (record) {
        if (record != null) {
            record._Deleted(true);
            //if(record.Title().length < 1)   record.Title("(Deleted)")
            if (record._Added()) {
                self.allRecords.remove(record);
                //record._Added(false);
            }
        }
    };

    self.unRemoveSelected = function (record) {
        if (record != null) // Prevent blanks and duplicates
        {
            record._Deleted(false);
        }
    };

    self.saveToServer = function () {
        var jsonData = ko.mapping.toJSON({ "changes": ko.mapping.toJS(self.allRecords) });
        $("input[type=button]").attr('disabled', 'disabled');
        showDlg(true);
        $.ajax({ // SO: 12846689
            url: location.href,
            type: "POST",
            dataType: "json",
            //processData: false,
            contentType: "application/json; charset=utf-8",
            data: jsonData,
            success: function (data) {
                if (data != null && data.length > 0) {
                    showDlg(false);
                    showNOTY(data, false);
                    $("input[type=button]").removeAttr('disabled');
                }
                else {
                    //alert("Operation was successful"); 
                    window.location.href = window.location.href; // Refresh page
                }
            }
        });
        // ko.utils.postJson(location.href, jsonData ); //{ records: ko.mapping.toJS(self.allRecords) });
        return false;
    }
};
var vmMaster = new recordsViewModel();
function createRecordsKO() {
    showDlg(true);
    $.getJSON(rolesKOVMURL, function (dataSet) {
        showDlg(false);
        var data = dataSet.records;
        var newRec;
        if (data != null && data.length > 0)
        { newRec = data.pop(); newRec._Added = true; }

        vmMaster.allRecords = ko.mapping.fromJS(data);// Make sure the last record is removed
        vmMaster.newRecord = ko.mapping.fromJS(newRec);

        vmMaster.OrgTypes = ko.mapping.fromJS(dataSet.OrgTypes);
        ko.applyBindings(vmMaster);

        $("#sortable").tableNav();
        setFocusEditableGrid("sortable", true);
        $("#btnAddNew").focus();
    });
}

$(document).ready(function () {    
    createRecordsKO();
});