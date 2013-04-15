/* http://ericmbarnard.github.com/KoGrid/#/examples
function pagingVm(){
    var self = this; 
    this.myData = ko.observableArray([]);
  
    this.filterOptions = {
        filterText: ko.observable(""),
        useExternalFilter: true
    };
  
    this.pagingOptions = {
        pageSizes: ko.observableArray([250, 500, 1000]),
        pageSize: ko.observable(250),
        totalServerItems: ko.observable(0),
        currentPage: ko.observable(1)     
    };
  
    this.setPagingData = function(data, page, pageSize){	
        var pagedData = data.slice((page - 1) * pageSize, page * pageSize);
        self.myData(pagedData);
        self.pagingOptions.totalServerItems(data.length);
    };
  
    this.getPagedDataAsync = function (pageSize, page, searchText) {
        setTimeout(function () {
            var data;
            if (searchText) {
                var ft = searchText.toLowerCase();
                $.getJSON('largeLoad.json', function (largeLoad) {		
                    data = largeLoad.filter(function(item) {
                        return JSON.stringify(item).toLowerCase().indexOf(ft) != -1;
                    });
                    self.setPagingData(data,page,pageSize);
                });            
            } else {
                $.getJSON('largeLoad.json', function (largeLoad) {
	                self.setPagingData(largeLoad,page,pageSize);
                });
            }
        }, 100);
    };
  
    self.filterOptions.filterText.subscribe(function (data) {
        self.getPagedDataAsync(self.pagingOptions.pageSize(), self.pagingOptions.currentPage(), self.filterOptions.filterText());
    });   

    self.pagingOptions.pageSizes.subscribe(function (data) {
        self.getPagedDataAsync(self.pagingOptions.pageSize(), self.pagingOptions.currentPage(), self.filterOptions.filterText());
    });
    self.pagingOptions.pageSize.subscribe(function (data) {
        self.getPagedDataAsync(self.pagingOptions.pageSize(), self.pagingOptions.currentPage(), self.filterOptions.filterText());
    });
    self.pagingOptions.totalServerItems.subscribe(function (data) {
        self.getPagedDataAsync(self.pagingOptions.pageSize(), self.pagingOptions.currentPage(), self.filterOptions.filterText());
    });
    self.pagingOptions.currentPage.subscribe(function (data) {
        self.getPagedDataAsync(self.pagingOptions.pageSize(), self.pagingOptions.currentPage(), self.filterOptions.filterText());
    });
  
    self.getPagedDataAsync(self.pagingOptions.pageSize(), self.pagingOptions.currentPage());

    this.gridOptions = {
        data: self.myData,
        enablePaging: true,
        pagingOptions: self.pagingOptions,
        filterOptions: self.filterOptions
    };	
};
ko.applyBindings(new pagingVm());
*/

//=========== HT: Extra functions and handling reqwuired by our custom KO implementation
var Date111 = "/Date(-62135596800000)/";
//http://stackoverflow.com/questions/8735617/handling-dates-with-asp-net-mvc-and-knockoutjs

// http://www.tutorialspoint.com/javascript/date_tolocaleformat.htm
// Or new Date(parseInt(jsonDate.substr(6))).toLocaleFormat('%d/%m/%Y')

ko.bindingHandlers.date = {
    init: function (element, valueAccessor, allBindingsAccessor, viewModel) {
        var jsonDate = valueAccessor();
        /*
        //It can be an observable or a mapped ko
        if (jsonDate != null && jsonDate.length < 1) try { jsonDate = jsonDate(); } catch (e) { jsonDate = null; }

        var value = new Date(); // today by default         
        //alert(value.toString());        
        if (jsonDate != null && jsonDate != Date111) {
        try { value = new Date(parseInt(jsonDate.substr(6))); } catch (e) { alert(e); } //value = new Date();
        }
        */
        var ret = ParseJSONdate(jsonDate); //value.getMonth() + 1 + "/" + value.getDate() + "/" + value.getFullYear();

        if (element.value == null) element.innerHTML = ret;
        else $(element).val(ret); //input element
    },
    update: function (element, valueAccessor, allBindingsAccessor, viewModel) {
        //alert(element + ":" + valueAccessor());
        var jsonDate = valueAccessor();
        /*
        //It can be an observable or a mapped ko
        if (jsonDate != null && jsonDate.length < 1) try { jsonDate = jsonDate(); } catch (e) { jsonDate = null; }

        var value = new Date(); // today by default         
        //alert(value.toString());        
        if (jsonDate != null && jsonDate != Date111) {
            try { value = new Date(parseInt(jsonDate.substr(6))); } catch (e) { alert(e); } //value = new Date();
        }
        */
        var ret = ParseJSONdate(jsonDate); //value.getMonth() + 1 + "/" + value.getDate() + "/" + value.getFullYear();

        if (element.value == null) element.innerHTML = ret;
        else $(element).val(ret); //input element
    }
};

function ParseJSONdate(jsonDate) {
    //It can be an observable or a mapped ko
    if (jsonDate != null && jsonDate.length < 1)
        try { jsonDate = jsonDate(); } catch (e) { jsonDate = null; }

    var value = new Date(); // today by default         
    //alert(value.toString());        
    if (jsonDate != null && jsonDate != Date111) {
        try { value = new Date(parseInt(jsonDate.substr(6))); } catch (e) { alert(e); }
    }
    return value.getMonth() + 1 + "/" + value.getDate() + "/" + value.getFullYear();
}