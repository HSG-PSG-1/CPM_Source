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
    try{$(editDiv).closest('tr').find("td input[class='noBorder']").focus().trigger("click");}catch(e){alert(e);}
    //editDiv.parentElement.parentElement.children[4].click();
}

function doEditableTA(td) {
    try { $(td).closest('tr').find("td textarea[class='noBorder']").focus().trigger("click"); } catch (e) { alert(e); }
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
        alert("This field is required")
        $(ctrl).focus();
        return false;
    }
    return true;
}

/*<span data-bind="text:new Date(parseInt(PostedOn.toString().substr(6))).toLocaleFormat('%d/%m/%Y')"></span>*/