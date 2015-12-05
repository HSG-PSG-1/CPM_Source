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
        try { value = new Date(parseInt(jsonDate.substr(6))); } catch (e) { alert(e.message); } //value = new Date();
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
            try { value = new Date(parseInt(jsonDate.substr(6))); } catch (e) { alert(e.message); } //value = new Date();
        }
        */
        var ret = ParseJSONdate(jsonDate); //value.getMonth() + 1 + "/" + value.getDate() + "/" + value.getFullYear();

        if (element.value == null) element.innerHTML = ret;
        else { $(element).val(ret);/*.trigger("change");*/ } //input element
    }
};

function ParseJSONdate(jsonDate) {
    //It can be an observable or a mapped ko
    if (jsonDate != null && jsonDate.length < 1)
        try { jsonDate = jsonDate(); } catch (e) { jsonDate = null; }

    var value = new Date(); // today by default         
    //alert(value.toString());        
    if (jsonDate != null && jsonDate != Date111) {
        try { value = new Date(parseInt(jsonDate.substr(6))); } catch (e) { alert(e.message + ":" + jsonDate + "."); }
    }
    value = makeDateTimezoneNeutral(value); // HT: DON'T forget
    return value.getMonth() + 1 + "/" + value.getDate() + "/" + value.getFullYear();
}

function formatDecimal(val) { 
if(val==null) return 0.00;
try{return val.toFixed(2);}catch(e){return 0.00;}
}

ko.unWrappedToJSON = function (obj) {
    return JSON.stringify(ko.toJS(obj), function (key, val) {
        return key === '__ko_mapping__' ? null : val; //undefined : val;
    });
}

function copyObservable(observableObject) 
{ return ko.mapping.fromJS(ko.toJS(observableObject)); }

function cloneObservable(obj) {
    if (ko.isWriteableObservable(obj))
        return ko.observable(obj()); //this is the trick

    if (obj === null || typeof obj !== 'object') return obj;

    var temp = obj.constructor(); // give temp the original obj's constructor
    for (var key in obj) {

        if (key === '__ko_mapping__') continue; //HT: Special case to exclude the ko mapping

        temp[key] = cloneObservable(obj[key]);
    }

    return temp;
    //return ko.mapping.fromJS(ko.toJS(observableObject)); 
}

var doTDHover = true;

function editable(ctrl, show) 
{
    if (show) $(ctrl).removeClass('noBorder').addClass('note'); //.attr('readOnly', '')
    else $(ctrl).removeClass('note').addClass('noBorder');//.attr('readOnly', true)
}

function doEditable(editDiv)
{
    var selector = "td input[class='editableTX'], textarea[class='editableTX']";
    try { $(editDiv).closest('tr').find(selector).eq(0).focus().trigger("click"); } catch (e) { alert(e.message); }
    //editDiv.parentElement.parentElement.children[4].click();
}

function setSubmitBtn(ctrl, btnID) {
    if ($(ctrl).val() != "") { $("#" + btnID).removeAttr("disabled"); }
    else { $("#" + btnID).attr("disabled", true); }
}

function doRequiredChk(ctrl)
{
    var val = $(ctrl).val();
    if (val == null || val.length < 1) {
        showNOTY("This field is required", false);
        $(ctrl).focus();
        return false;
    }
    return true;
}

/*<span data-bind="text:new Date(parseInt(PostedOn.toString().substr(6))).toLocaleFormat('%d/%m/%Y')"></span>*/

ko.bindingHandlers.datepicker = {
    init: function (element, valueAccessor, allBindingsAccessor) {
        try {
            //initialize datepicker with some optional options
            var options = allBindingsAccessor().datepickerOptions || {};

            var funcOnSelectdate = function () {
                var observable = valueAccessor();
                observable($(element).datepicker("getDate"));

                // explicitly trigger change for alt field which stored the text date
                try { $($(element).datepicker("option", 'altField')).change(); }
                catch (e) { ; }
            }

            options.onSelect = funcOnSelectdate;

            // NEW : special case SO : http://bugs.jqueryui.com/ticket/5734
            options.onClose = function (selectedDate, inst) {
                if (selectedDate == '') {
                    $(inst.settings["altField"]).val(selectedDate);
                }
            };

            $(element).datepicker(options);

            //handle the field changing
            ko.utils.registerEventHandler(element, "change", funcOnSelectdate);

            //handle disposal (if KO removes by the template binding)
            ko.utils.domNodeDisposal.addDisposeCallback(element, function () {
                $(element).datepicker("destroy");
            });

        } catch (ex) { alert(ex.message); }
    },
    update: function (element, valueAccessor) {
        try {
            var value = ko.utils.unwrapObservable(valueAccessor());

            //handle date data coming via json from Microsoft
            if (String(value).indexOf('/Date(') == 0) {
                value = new Date(parseInt(value.replace(/\/Date\((.*?)\)\//gi, "$1")));
            }

            current = $(element).datepicker("getDate");

            if (value - current !== 0) {
                value = makeDateTimezoneNeutral(value); // HT: DON'T forget
                $(element).datepicker("setDate", value);
            }

            /* For string data passed as yy-mm-dd
            if (typeof (value) === "string") { // JSON string from server
            value = value.split("T")[0]; // Removes time
            }

            var current = $(element).datepicker("getDate");

            if (value - current !== 0) {
            var parsedDate = $.datepicker.parseDate('yy-mm-dd', value);
            $(element).datepicker("setDate", parsedDate);
            } */

        } catch (ex) { alert(ex.message); }
    }
};
/* <input data-bind="datepicker: myDate, datepickerOptions: { minDate: new Date() }" /> */
function makeDateTimezoneNeutral(dt) {
    console.log(dt);
    // HT: SPECIAL CASE - some time zone client browsers will show upto 1 day offset based on the UTC time diff
    //DOESN'T work - dt = new Date(dt.getTime() - dt.getTimezoneOffset() * 60000);
    dt.setHours(dt.getHours() + dt.getTimezoneOffset() / 60);
    // ^^^ this shud nullify that difference as per SO : 1486476 (works),  26028466 (nope)
    console.log(dt);
    return dt;
}