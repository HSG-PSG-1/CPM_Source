var IsCDEditMode = false;
var NextNewItemID = -1;
var itemsModel = function () {
    var self = this;
    var _dummyObservable = ko.observable(); // use inside computed property

    self.emptyItem = "";

    self.showGrid = ko.observable(true);
    self.itemToAdd = ko.observable();
    self.IsCDEditMode = ko.observable();
    self.allItems = ko.observableArray([]);
    self.allFilesDetail = new Array();

    self.invalidateToRefreshComputed = function () {
        _dummyObservable.notifySubscribers(); //fake a change notification
    };
    /*self.itemToAdd.Defect = ko.computed(function () {            
    return $("#ddlDefect option[value='" + self.itemToAdd.NatureOfDefect + "']").text();
    });*/

    self.MaxID = ko.computed(function () {
        _dummyObservable(); //retrieve and ignore the value
        var len = self.allItems().length;
        var last = self.allItems()[len-1];
        return (last == null)? -1 : last.ID();
    });

    self.CreditAmtTotal = ko.computed(function () {
        // http://stackoverflow.com/questions/10940302/knockout-compute-sum
        var total = parseFloat(0.00);
        _dummyObservable(); //retrieve and ignore the value
        for (var i = 0; i < self.allItems().length; i++) {
            if (!self.allItems()[i]._Deleted())
                total += parseFloat(self.allItems()[i].CreditAmt1());
        }
        return total.toFixed(2);
    });

    self.InvoiceAmtTotal = ko.computed(function () {
        // SO:9351939
        var total = parseFloat(0.00);
        _dummyObservable(); //retrieve and ignore the value
        ko.utils.arrayForEach(self.allItems(), function (item) {
            if (!item._Deleted())
                total += parseFloat(item.InvoiceAmt1());
        });
        return total.toFixed(2);
    });

    self.formatData = function () {
        ko.utils.arrayForEach(self.allItems(), function (item) {
            item.InvoiceAmt1(item.InvoiceAmt1().toFixed(2));
            item.CreditAmt1(item.CreditAmt1().toFixed(2));
        });
    }

    self.openAttachFilesDetail = function (item) {
        var url = FilesDetailURL.replace("-999", item.ID()); /*"@Url.Action("FilesDetail", "Claim")" + "?ClaimDetailID=" + item.ID() + "&ClaimGUID=@ViewData["ClaimGUID"]"; */        
        var winFD = openWin(url, 450, 650);
        //winFD.onunload = 
    }

    self.openAttachFilesDetailDirect = function (item) {
        var url = FilesDetailURL.replace("-999", item.ID());
        var winFD = openWin(url, 450, 650);
        self.itemToAdd(item);
    }

    self.showAddNew = function () {
        if (!brandHasItems()) { showNOTY("Please select a Brand which has atleast one item", false); return; }

        self.itemToAdd(cloneObservable(self.emptyItem)); //self.itemToAdd(ko.mapping.fromJS(self.emptyItem));

        self.showGrid(false); $("#itmEntryDailog").dialog("open");
        doBrandsFillAndNumericTXT();
        IsCDEditMode = false;
    }

    self.setEdited = function (item) {
        item._Edited(!item._Added());
        item.LastModifiedDate(Date111);
        /*if(item.aDFilesJSON == null || item.aDFilesJSON() == null){item.aDFilesJSON = ko.observable("[]");}*/

        self.itemToAdd(item); //ko.mapping.fromJS(ko.toJS(item), self.itemToAdd);

        self.showGrid(false); $("#itmEntryDailog").dialog("open");
        doBrandsFillAndNumericTXT();
        IsCDEditMode = true;
    };

    self.setEditedFlag = function (item) {
        item._Edited(!item._Added());
        item.LastModifiedDate(Date111);
    };

    self.saveItem = function (item) {
        if (item.ItemCode == null || item.ItemCode == "")
        //if (self.itemToAdd.ItemCode == null || self.itemToAdd.ItemCode == "")
            showNOTY("Item is a required field", false);
        else {
            if (!IsCDEditMode) {
                item.ID = NextNewItemID;
                self.allItems.push(ko.mapping.fromJS(cloneObservable(item))); // self.allItems.push(item);
                NextNewItemID = NextNewItemID - 1;
            }
            else {
                /*var old = ko.utils.arrayFirst(self.allItems(), function (item) {return item.ID() == self.itemToAdd.ID(); });
                self.allItems.replace(old, self.itemToAdd); //self.locations.valueHasMutated();*/
            }

            IsCDEditMode = false;
            //HT: How to reset the add new obj
            self.cancelItem(item); /*//ko.toJS(self.emptyItem) // ko.mapping.fromJS(ko.toJS(self.emptyItem), self.itemToAdd);*/
            self.showGrid(true); $("#itmEntryDailog").dialog("close");
        }
    };

    self.removeSelected = function (item) {
        if (item != null) // Prevent blanks and duplicates
        {
            //self.allItems.valueWillMutate();
            item._Deleted(true);
            if (item._Added()) {
                item._Added(false);
                self.allItems.remove(item);
            }
            /*var old = ko.utils.arrayFirst(self.allItems(), function (itm) { return itm.ID == item.ID; });
            self.allItems.replace(old, item);
            var index = self.allItems.indexOf(item);
            self.allItems.remove(item);
            self.allItems.splice(index, 0, item);*/
        }
    };

    self.unRemoveSelected = function (item) {
        if (item != null) // Prevent blanks and duplicates
        {
            item._Deleted(false);
        }
    };
    
    self.cancelItem = function (item) {
        IsCDEditMode = false;
        self.itemToAdd(cloneObservable(self.emptyItem)); //self.itemToAdd(ko.mapping.fromJS(self.emptyItem));
        self.showGrid(true); $("#itmEntryDailog").dialog("close");
    };

    self.saveToServer = function () {
        ko.utils.postJson(location.href, { items: ko.mapping.toJS(self.allItems) });
        return false;
    };

    /*self.createTableNav = function (data, element) {
        return;
        if (element.ID() == self.MaxID())
        { alert(element.ID() + ":" + $("#tblItems").find('tbody').find('input:enabled').length); $("#tblItems").tableNav(); }
    };*/
};

var viewModelItems = new itemsModel();
function createItemsKO(data) {
    /* Because we need to show/hide file upload if (data.ItemToAdd.ID != -1) data.ItemToAdd.ID = NextNewItemID;*/

    viewModelItems.emptyItem = data.ItemToAdd;
    viewModelItems.itemToAdd(data.ItemToAdd);
    viewModelItems.allFilesDetail = new Array();

    viewModelItems.allItems = ko.mapping.fromJS(data.AllItems);

    try { viewModelItems.allItems.sort(function (l, r) { return l.ID() < r.ID() ? -1 : 1 }); } catch (ex) { ; }

    viewModelItems.Defects = ko.mapping.fromJS(data.Defects);
    viewModelItems.showGrid(data.showGrid); // viewModelItems.showGrid = ko.mapping.fromJS(data.showGrid);

    viewModelItems.invalidateToRefreshComputed();
    viewModelItems.formatData();

    ko.applyBindings(viewModelItems, document.getElementById("divItems"));    

    $("#itmEntryDailog").dialog({ // REQUIRED for old ITem entry
        modal: true,
        autoOpen: false,
        height: 430,
        width: 710,
        close: function (event, ui) { viewModelItems.showGrid(true); },
        open: function (event, ui) { setTimeout(function(){$('.ui-dialog-titlebar-close').blur();},1);}
    });

    doBrandsFillAndNumericTXT();
    //setTimeout(function () { $("#tblItems").tableNav(); },5000); // take time to create ko binding    
}
/*
function addFilesDetail(dFiles){
if(viewModelItems.allFilesDetail != null && dFiles != null && dFiles.length > 0)
viewModelItems.allFilesDetail.push(dFiles);
}*/
