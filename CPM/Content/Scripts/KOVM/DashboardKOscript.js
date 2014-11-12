$().ready(function ()
{
    doCollapse(); //If url has collapse        

    callAutoComplete(brandLookup, "#BrandName", "#BrandID");
    //callAutoComplete("ShipLoc", "#ShipToLoc", "#ShipToLocationID");

    callAutoComplete("Customer", "#CustOrg", "#CustID");
    callAutoComplete("User", "#AssignToName", "#AssignedTo");
    callAutoComplete("Salesperson", "#Salesperson", "#SalespersonID");
    //Ref: $("#SalesPerson").autocomplete({ source: ['this', 'is', 'easy', 'to', 'have', 'an', 'auto', 'complete'] });

    createToFromjQDTP("#ClaimDateFrom", "#ClaimDateTo");
    // Configure Date picker plugin
    /*var dates = $("#ClaimDateFrom1, #ClaimDateTo1").datepicker({
        defaultDate: "+1w",
        minDate:minSQLDate,
        maxDate:maxSQLDate,            
        changeMonth: true,
        numberOfMonths: 3,
        onSelect: function(selectedDate) {
            var option = this.id == "ClaimDateFrom1" ? "minDate" : "maxDate",
                instance = $(this).data("datepicker"),
                date = $.datepicker.parseDate(
                    instance.settings.dateFormat ||
                    $.datepicker._defaults.dateFormat,
                    selectedDate, instance.settings);
            dates.not(this).datepicker("option", option, date);

            $(this).trigger("change"); // Specially for KO
        }            
    });
        // Set format to be used by alt date field
        $("#ClaimDateFrom1").datepicker("option", "altField", '#ClaimDateFrom');
        $("#ClaimDateFrom1").datepicker("option", "altFormat", 'dd-M-yy');

        $("#ClaimDateTo1").datepicker("option", "altField", '#ClaimDateTo');
        $("#ClaimDateTo1").datepicker("option", "altFormat", 'dd-M-yy');
    */
    
    //setFocus("ClaimNos");
});

function setDTPdateForKO() {
    //Special case for dates handling and prevent JSON data conversion issues
    $("#ClaimDateFrom").val($("#ClaimDateFrom1").val()).trigger("change");
    $("#ClaimDateTo").val($("#ClaimDateTo1").val()).trigger("change");
}

function showDialog(action, claimID, archived) {
    var containerID = "#dialog" + claimID;
    $(containerID).dialog({
        modal: false,
        open: function () {
            $(this).html(loading);
            $(this).load(dialogPartialURL + action + '?ClaimID=' + claimID + '&Archived=' + archived);
            setTimeout(function () { $('.ui-dialog-titlebar-close').blur(); }, 1);
        },
        height: 360,
        width: 650,
        title: action,
        close: function (event, ui) { //NOTE: Required so that multiple dialog controls can be references with the same id
            $(this).dialog("destroy").empty();
        }//Can't use .remove() as in SO: 6515052 so we empty the html.
    });
}

var lastTR = "#tblStatusHistory tbody>tr:last";
var oldStat = "#OldStatus", oldStatID = "#OldStatusID", newStat = "#NewStatus", newStatID = "#NewStatusID";

function changeClaimStatusPost(ClaimId, OldStatusID, NewStatusID) {
    var data = {}; //Set data to be posted back
    data["ClaimId"] = ClaimId; data["OldStatusID"] = OldStatusID; data["NewStatusID"] = NewStatusID;

    $.post(ChangeClaimStatusURL.replace('-99', ClaimId), data);
    return false; // prevent any postback
}
function openPrintDialog(ClaimId) {
    if (ClaimId > 0) return openWinScrollable(printURL.replace('-99', ClaimId), 648, 838);
}

/* KO based pagination */
function updatePagedRows(vm) {// Starts with : index=0
    ListURL = ListURL.replace("index=" + (vm.pageIndex() - 1), "index=" + vm.pageIndex());
    showDlg(true);
    $.getJSON(ListURL, function (data) {
        showDlg(false);
        //vmDashboard.Claims = ko.observableArray(data); // Initial items
        //ko.applyBindings(vmDashboard);
        if (data != null)
            ko.utils.arrayForEach(data, function (item) {
                vm_D.fields.push(item);
            });
        else
            vm_D.pageIndex(0);//reset            
    });
}


function resetForm(btn) {
    clearForm(document.getElementById('frm'));
    document.getElementById('doReset').checked = true;
    resetDatepicker('#ClaimDateFrom1, #ClaimDateTo1');

    //trigger changeClaimStatusPost for KO binding notification
    vm_D.search.ClaimNos(null);
    vm_D.search.StatusID(0);
    vm_D.search.AssignToName(null);
    vm_D.search.CustRefNo(null);
    vm_D.search.BrandName(null);
    vm_D.search.CustOrg(null);
    vm_D.search.Salesperson(null);

    vm_D.search.ClaimDateFrom(null);
    vm_D.search.ClaimDateTo(null);

    var hadArchived = vm_D.search.Archived();
    vm_D.search.Archived(false);

    if (hadArchived == true)
        getArchivedData($("#Archived"));

    /*vm_D.quickSearch(null);*/
}

function getArchivedData(chk) {
    var archived = $(chk).is(':checked');

    $.getJSON(ArchivedURL.replace('-99', archived), function (data) {
        vm_D.fields(data.records);
        vm_D.invokeSearch(3);
    });
}

function excelPostback(e) {
    skipCommitChk = true;
    $.ajax({
        type: "POST",
        //contentType: "application/json; charset=utf-8", dataType: "json",
        data: $("#frm").serialize(),
        url: SetSearchOptsURL,
        success: function (data) {
            $("#frmExcel").submit();
        }
    });
    return false; // to cause form postback
}


var claimObj = "";//will be set whn the cell is clicked
function updateStatusHistory() {
    //clone and insert the tr after the table
    $(lastTR).clone(true).insertAfter(lastTR);
    //Update data
    $(lastTR).find('td:first').html(usrName);

    $(lastTR).find('td:first').next().html(todayDT);
    $(lastTR).find('td:last').prev().html($(oldStat).val());
    $(lastTR).find('td:last').html($(newStatID).children("option:selected").text());

    $(lastTR).effect('highlight', {}, 1000); //highlight

    //persist updated status in textbox
    $(oldStatID).val($(newStatID).val());
    $(oldStat).val($(newStatID).children("option:selected").text());
    //update in Grid (NOTE: make sure 'claimObj' is set when td is clicked
    $("#statusDIV" + claimObj.ID).text($(oldStat).val()).trigger("change").effect('highlight', {}, 2000);
    claimObj.Status = $(oldStat).val(); claimObj.StatusID = $(newStatID).val();
}

/* ==================== Dashboard ViewModel ==================== */

var vmDashboard = function () {
    var self = this;
    self.fields = ko.observableArray();//jsondata
    self.pageSize = ko.observable(gridPageSize);
    self.pageIndex = ko.observable(0);
    self.cachedPagesTill = ko.observable(0);
    self.sortField = ko.observable("CNo");
    self.sortOrderNxtAsc = ko.observable(true);
    self.search = ko.observable();
    self.invokeSearch = ko.observable(2);
    /*self.quickSearch =  ko.observable("");*/

    self.previousPage = function () {
        self.pageIndex(self.pageIndex() - 1);
        if (self.cachedPagesTill() < 1)
            self.cachedPagesTill(0);
        self.cachedPagesTill(self.cachedPagesTill() + 1);
    };
    self.nextPage = function () {
        self.pageIndex(self.pageIndex() + 1);
        //if(self.cachedPagesTill() < 1)
        //    updatePagedRows(self);
        self.cachedPagesTill(self.cachedPagesTill() - 1);
    };

    self.filteredRecords = ko.computed(function () {
        var s = self.invokeSearch(); self.invokeSearch(0);

        var s = self.search;

        var ClaimNos = (s.ClaimNos != null && s.ClaimNos() != null && s.ClaimNos() != "") ? s.ClaimNos() : null;
        var StatusID = (s.StatusID != null && s.StatusID() != null && s.StatusID() != "") ? s.StatusID() : 0;
        var AssignToName = (s.AssignToName != null && s.AssignToName() != null && s.AssignToName() != "") ? s.AssignToName().toLowerCase() : null;
        var CustRefNo = (s.CustRefNo != null && s.CustRefNo() != null && s.CustRefNo() != "") ? s.CustRefNo().toLowerCase() : null;
        var BrandName = (s.BrandName != null && s.BrandName() != null && s.BrandName() != "") ? s.BrandName().toLowerCase() : null;
        var CustOrg = (s.CustOrg != null && s.CustOrg() != null && s.CustOrg() != "") ? s.CustOrg().toLowerCase() : null;
        var Salesperson = (s.Salesperson != null && s.Salesperson() != null && s.Salesperson() != "") ? s.Salesperson().toLowerCase() : null;
        var Archived = (s.Archived != null) ? s.Archived() : false;

        var ClaimDateFrom = (s.ClaimDateFrom != null && s.ClaimDateFrom() != "") ? s.ClaimDateFrom() : null;
        var ClaimDateTo = (s.ClaimDateTo != null && s.ClaimDateTo() != "") ? s.ClaimDateTo() : null;

        /*var quickSearch = (self.quickSearch != null && self.quickSearch() != null && self.quickSearch() != "")?self.quickSearch().toLowerCase():null;*/

        self.pageIndex(0);

        return ko.utils.arrayFilter(self.fields(), function (rec) {
            return (
                (ClaimNos == null || rec.CNo.toString().indexOf(ClaimNos) > -1) &&
                (StatusID < 1 || rec.StatusID == StatusID) &&
                (AssignToName == null || rec.AsgnTo.toLowerCase().indexOf(AssignToName) > -1) &&

                (CustRefNo == null || (rec.CustRef != null && rec.CustRef.toLowerCase().indexOf(CustRefNo) > -1)) &&

                (BrandName == null || rec.Brand.toLowerCase().indexOf(BrandName) > -1) &&
                (CustOrg == null || rec.CustOrg.toLowerCase().indexOf(CustOrg) > -1) &&
                (Salesperson == null || rec.SP.toLowerCase().indexOf(Salesperson) > -1) &&
                (ClaimDateFrom == null || new Date(rec.CDate) >= new Date(ClaimDateFrom)) &&
                (ClaimDateTo == null || new Date(rec.CDate) <= new Date(ClaimDateTo)) &&
                (rec.Archvd == Archived)
                /* START : Special case quick search filter 
                && (quickSearch == null ||
                    (
                        rec.Status.toLowerCase().indexOf(quickSearch) > -1 ||
                        rec.AsgnTo.toLowerCase().indexOf(quickSearch) > -1 ||
                        (rec.CustRef != null && rec.CustRef.toLowerCase().indexOf(quickSearch) > -1) ||
                        rec.Brand.toLowerCase().indexOf(quickSearch) > -1 ||
                        rec.CustOrg.toLowerCase().indexOf(quickSearch) > -1 ||
                        rec.SP.toLowerCase().indexOf(quickSearch) > -1
                    )
                )
                 END : Special case quick search filter */
            );
        });
        /*
        SO: 13229970/knockout-filtering
        return ko.utils.arrayFilter([item.FirstName().toLowerCase(), item.lastName().toLowerCase(), 
        item.email().toLowerCase(), item.company().toLowerCase()], function (str) { return str.indexOf(filter) != -1  }).length > 0;*/
        //else            return self.fields();            
    });

    self.maxPageIndex = ko.computed(function () {//dependentObservable
        var s = self.invokeSearch(); self.invokeSearch(0);
        return Math.ceil(self.filteredRecords().length / self.pageSize()) - 1;
    });

    self.pagedRows = ko.computed(function () {//dependentObservable
        var size = self.pageSize();
        var start = self.pageIndex() * size;
        return self.filteredRecords().slice(start, start + size);
    });

    self.sortData = function (data, event, sort) {
        if ((self.sortField() == sort))
            self.sortOrderNxtAsc(!self.sortOrderNxtAsc());
        else
        { self.sortField(sort); self.sortOrderNxtAsc(false); }

        var sortOrder = self.sortOrderNxtAsc() ? -1 : 1; // Asc : Desc

        /*"click: function(data,event){fields.sort(function (l, r) { return l.Status > r.Status ? 1 : -1 })}"*/
        switch (sort) {
            case "CNo":
                self.fields.sort(function (l, r) { return l.CNo > r.CNo ? 1 * sortOrder : -1 * sortOrder });
                break;
            case "CDate":
                self.fields.sort(function (l, r) { return new Date(l.CDate) > new Date(r.CDate) ? 1 * sortOrder : -1 * sortOrder }); // ClaimDate
                break;
            case "CustRef": // Need to convert into string while comparison
                self.fields.sort(function (l, r) { return l.CustRef + "" > r.CustRef + "" ? 1 * sortOrder : -1 * sortOrder });
                break;
            case "CustOrg":
                self.fields.sort(function (l, r) { return l.CustOrg > r.CustOrg ? 1 * sortOrder : -1 * sortOrder });
                break;
            case "ShpLocCode"://ShipToLoc
                self.fields.sort(function (l, r) { return l.ShpLocCode > r.ShpLocCode ? 1 * sortOrder : -1 * sortOrder });
                break;
            case "Brand":
                self.fields.sort(function (l, r) { return l.Brand > r.Brand ? 1 * sortOrder : -1 * sortOrder });
                break;
            case "SP":
                self.fields.sort(function (l, r) { return l.SP > r.SP ? 1 * sortOrder : -1 * sortOrder });
                break;
            case "Status":
                self.fields.sort(function (l, r) { return l.Status > r.Status ? 1 * sortOrder : -1 * sortOrder });
                break;
            case "Cmts":
                self.fields.sort(function (l, r) { return l.Cmts > r.Cmts ? 1 * sortOrder : -1 * sortOrder });
                break;
            case "Files":
                self.fields.sort(function (l, r) { return l.Files > r.Files ? 1 * sortOrder : -1 * sortOrder });
                break;
        }

        $(".header tr th").each(function (i) {
            $(this).html($(this).html().replace("▲", ""));
            $(this).html($(this).html().replace("▼", ""));
            /*$(this).html($(this).html().replace("&#9650;","&#9660;"));*/

            if ($(this).html().indexOf(vm_D.sortField()) > -1)
                $(this).html($(this).html() + " " + (vm_D.sortOrderNxtAsc() ? "▼" : "▲"));
        });

        self.pageIndex(0); self.cachedPagesTill(0);
    }
};