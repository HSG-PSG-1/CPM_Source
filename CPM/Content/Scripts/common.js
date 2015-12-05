// // //  jQuery is required for certain functions so be careful  // // // 

////////////////////START : auto complete/////////////////////////

var autoCompMinLen = 2; //MUST for Auto-Complete
var autoCompleteALL = "  "; //Must be equal to autoCompMinLen
var delTR = ""; // Required tohold te deleted TR fopas using taconite plugin

// If the required field is empty then empty the impact control
function checkReq(ctrl, impactCtrl) { if (!($(ctrl).val().toString().length > 0)) $(impactCtrl).val('').trigger("change"); }
// Log the selected item.id or empty into id textbox
function log(item, idBox, txtBox) {
    $(idBox).val(item ? item.id : '').trigger("change"); /*"#ItemID" */
    //$(txtBox).trigger("change"); /* IE 10+ doesn't trigger the change value */
    //try { $(txtBox).validate().valid(); } catch (e) { }
}
//Toggle the display of the two images in parent (make sure you follow the sequence)
function toggleDDimg(parent, drop) {
    parent.children('img:first').css("display", drop ? "none" : "");
    parent.children('img:last').css("display", drop ? "" : "none");
}
//Stuff the autocomplete control with blank space and trigger search
function getStuffedAutoCompVal(ctrl) {
    var valu = $(ctrl).val();//Empty the value
    while (valu == null || valu.length < autoCompMinLen) { valu = valu + " "; }
    $(ctrl).focus();
    return valu;
}
//Common global function to configure & render Autocomplete plugin
function renderAutoComplete(url, idBox, txtBox) {
    var ddSpan = $(idBox).parent().children('span:first');

    $(txtBox).autocomplete({//"#txtItemID"
    source: url
    , autoFocus: true
    , minLength: autoCompMinLen
    , select: function (event, ui) { if (ui.item.id == null) { event.preventDefault(); } else log(ui.item, idBox, txtBox); }
    , focus: function(event, ui) {  if (ui.item.id == null) event.preventDefault(); }
    , change: function (event, ui) { $(txtBox).trigger("change"); } // for chrome
    //, search: function(event, ui) {$("#msg").show();$("#msg").html($(txtBox + ', li').length); $("#msg").dialog();}
    //Tie up events to toggle dropdown images
    , open: function(event, ui) { toggleDDimg(ddSpan, true); $("#msg").hide(); } //$(txtBox).find('li').length
    , close: function(event, ui) { toggleDDimg(ddSpan, false); }
    })
    
    //KEPT for future ref
    /*.dblclick(function(e) { HT: Sometimes too many double clicks cause open/close toggle confusion!
    //http://stackoverflow.com/questions/4439736/jquery-ui-autocomplete-triggering-a-search-from-outside-of-autocomplete
    $(this).autocomplete('search', getStuffedAutoCompVal(this));//$(this).val()));
    }) */
    ;
    
    //Attach onblur event to empty ID field
    $(txtBox).change(function() { checkReq(txtBox, idBox); }); //blur
    $(txtBox).focus(function() { $(this).select(); });//select text
    //Set initial value (null if it was defaulted to 0) and set tooltip
    var val = $(idBox).val(); $(idBox).val((val == "0") ? "" : val).trigger("change"); //Because sometimes Int is by default initialized by 0
    $(txtBox).attr('title', 'Start typing to search or enter blank space twice to view all');
    $(idBox).css("display", "none");
}
////////////////////END : auto complete/////////////////////////

// Set dropdown selected item text into the supplied textbox
function setDDLtext(ddl, txtID) {
    var ddl = $(ddl);//  document.getElementById(ddlID);
    //var selIndex = (ddl.selectedIndex != null) ? ddl.selectedIndex : 0;
    var selText = "";
    if (ddl.prop('selectedIndex') > -1)
        selText = ddl.children("option").filter(":selected").text();
    $("#" + txtID).val(selText).trigger("change"); //$("#" + txtID).val(ddl.options[selIndex].text).trigger("change");
}

function roundNumber(rnum, rlength) { // Arguments: number to round, number of decimal places
    var newnumber = Math.round(rnum * Math.pow(10, rlength)) / Math.pow(10, rlength);
    return parseFloat(newnumber); // Output the result to the form field (change for your purposes)
}

function confirmDeleteM(evt, msg) {
    var GoAhead = window.confirm(msg);
    return stopEvent(evt, GoAhead);
}

function confirmDelete(evt) {
    return confirmDeleteM(evt, "Are you sure you want to delete this record?");
    //var GoAhead = window.confirm("Are you sure you want to delete this record?");    return stopEvent(evt, GoAhead);
}

function stopEvent(e, GoAhead) {
    if (GoAhead) return true; //Continue

    if (!e) e = window.event;
    //e.cancelBubble is supported by IE - this will kill the bubbling process.
    if (e.cancelBubble != null) e.cancelBubble = true;
    if (e.stopPropagation) e.stopPropagation(); //e.stopPropagation works in Firefox.
    if (e.preventDefault) e.preventDefault();
    if (e.returnValue != null) e.returnValue = false; // http://blog.patricktresp.de/2012/02/
    return false;
}

////////////////////START : ajax postback/////////////////////////

//HT:CAUTION: Make sure the below two variables are initiated!
//defaultURL = ''; //Example: $('#frmComments').attr('action');
// containerId = ''; //Example: "#divComments";
//var loginURL = "/Common/Login"; //Make sure Login page has table with id=tblLogin001

function reload(url, defaultURL, containerId) { //SO: 1304299/jquery-load-content-from-link-with-ajax
    if (url == null || url.length < 1) url = defaultURL;
    $(containerId).html(loading); //Make sure 'loading' is declared!
    $(containerId).load(url, function() { checksession(); });   // load the html response into a DOM element
    return false; // prevent further execution
}

function reloadWithCallback(url, defaultURL, containerId, callback) {
    if (url == null || url.length < 1) url = defaultURL;
    $(containerId).html(loading); //Make sure 'loading' is declared!
    $(containerId).load(url, callback);
    return false;
}

// Common global function to configure & render ajaxForm plugin
//HT: Make sure 'jquery.form.js' is included!. All ids must have '#' prefixed (jQuery ready)
function doAjaxForm(frmId, targetId, resetId) {
    var isFormOutside = ($(resetId)[0] != null);
    //AJAXify form
    $(frmId).ajaxForm({
        target: targetId
            , success: function (data, textStatus) {
                /* For testing : alert(textStatus);*/
                checksession();
                /* HT: http://stackoverflow.com/questions/199099/how-to-manage-a-redirect-request-after-a-jquery-ajax-call */
                if (isFormOutside) $(resetId).attr("checked", false); //Its a Grid (Form is OUTSIDE)
                else $(frmId).show(); //Its a Form (Form is INSIDE)
            }
            , error: function (xhr, textStatus, errorThrown) {
                //alert(textStatus + ":" + errorThrown);
                // Boil the ASP.NET AJAX error down to JSON.
                //var err = eval("(" + request.responseText + ")");
                // Display the specific error raised by the server
                $(targetId).html("<div class='error'>Error has occurred. Possible causes:<br/><ul>" +
                "<li>Session is corrupted or expired. Please logout & login again.</li>" +
                "<li>Request size including file upload (if any) exceeds the maximum allowable size.</li>" +
                "<li>Internal processing error.</li>" +
                "</ul><i class='small'>It might be a glitch. But if you're getting this error consistently. Kindly report us with specific details.</i></div>"); //xhr.responseText
                //console.log(request.responseText);
            }
            , beforeSubmit: function (arr, form, options) {
                //jQuery.validate + ajaxForm : http://www.chrillo.info/2008/10/14/jquery-ajaxform-and-validate-with-ajaxsubmit/
                var isValid = $(form).valid(); // MAGIC! will be true if validation is NOT used
                if (!isValid) return isValid; //return false and STOP if form is NOT valid

                if (isFormOutside) $(targetId).html(loading);
                else { $(frmId).hide(); $(targetId).prepend(loading); }
            }
    });
}

//If session was invalid and the Login view has been rendered then redirect
function checksession() {
    if ($('#tblLogin001')[0] != null) {
        alert('Session Expired');
        window.location.href = (loginURL == null) ? "/Common/Login" : loginURL;
    }
    /*loginURL is defined in Masterpage*/
}

////////////////////END : ajax postback/////////////////////////

function clearForm(oForm) {
    var elements = oForm.elements;
    oForm.reset();
    try{
    for (i = 0; i < elements.length; i++) {

		if (!elements[i].type) continue;//HT: To avoid unwanted elements

		field_type = elements[i].type.toLowerCase();
        switch (field_type) {

            case "text":
            case "password":
            case "textarea":
            case "hidden":
                elements[i].value = "";
                break;

            case "radio":
            case "checkbox":
                if (elements[i].checked) {
                    elements[i].checked = false;
                }
                break;

            case "select-one":
            case "select-multi":
                elements[i].selectedIndex = -1;
                break;

            default: break;
        }
    }
    }catch(e){}
}

//minSQLDate & maxSQLDate are declared in Master page
function resetDatepicker(commaSepJQdatepickerIDs) {
    $(commaSepJQdatepickerIDs).val('').trigger("change").datepicker('option', { minDate: minSQLDate, maxDate: maxSQLDate });
}

//////////////////// START: Scripts to keep the search panel hidden while paging/sorting  ////////////////////
var tblSearch = 'tblSearch';
function doCollapse() { if (window.location.href.search('sp=none') > -1) showHideDiv(tblSearch); }
function chkCollapse(a) {//NOTE: pager absords the whole url unlike sorting
    try {
        var divElem = document.getElementById(tblSearch);
        var hidden = (divElem.style.display == "none");
        var isHrefWithHiddenSet = (a.href.search('sp=none') > -1);

        if (isHrefWithHiddenSet && hidden) return true;
        // for stale pager links
        else if (!hidden) window.location = a.href.replace('&sp=none', ''); //return true;
        // for sort headers
        else window.location = a.href + "&sp=" + divElem.style.display;  //if(a.href.search('sp=none') <= -1)            
        return false;
    } catch (ex) { return true; }
}
//////////////////// END: Scripts to keep the search panel hidden while paging/sorting  ////////////////////

function toggleDDL(txt, ddl) {
    flag = (ddl.style.display == "none"); //(document.getElementById(ddlID)

    ddl.style.display = flag ? "inline" : "none";
    ddl.style.width = flag ? "80px" : "0px";

    var width = txt.getAttribute("width1");
    txt.style.width = flag ? "53px" : width;
}
// Function to pre/post attach "%" to be used for string comparison when applying filter in db
function setFiltered(filter, txt) {
    var optr = "%";
    //txt = document.getElementById(txt);
    var val = txt.value.replace("%", "").replace("%", "").replace("%", "").replace("%", ""); //replace % 4 times

    if (val != null && val.length > 0) {//Proceed only if value is non-empty
        switch (filter) {
            case "3": txt.value = val + optr; break; //opts.starts_with
            case "4": txt.value = optr + val; break; //opts.ends_with
            case "2": txt.value = val; break; //opts.exact

            case "1":
            default: txt.value = optr + val + optr; break; // opts.contains 
        }
    }
    txt.focus(); //set back focus
    //Set value in filter-textbox (if any)
    var paramTxt = txt.getAttribute("paramTxt");
    if (paramTxt != null && paramTxt.length > 0)
        document.getElementById(paramTxt).value = txt.value;
}

function toggleImg(parentSpan) {//use for span with two toggle img
    if (parentSpan == null) return;
    // toggle image display
    $(parentSpan).children('img:first').toggle();
    $(parentSpan).children('img:last').toggle();    
}

//Toggle show/hide for a section
//It assumes that the image will have the id = '<divID>Img'
//INFO: For MVC we've declared it in the Master file
//var showImgPath = "../../Content/Images/aroL.gif";var hideImgPath = "../../Content/Images/aroB.gif";
function showHideDiv(element) {
    showImgPath = (showImgPath == null) ? "../../Content/Images/aroL.gif" : showImgPath;
    hideImgPath = (hideImgPath == null) ? "../../Content/Images/aroB.gif" : hideImgPath;

    var divElem = document.getElementById(element);
    var img = document.getElementById(divElem.id + "Img");

    var showFlag = (divElem.style.display == "" || divElem.style.display == "");
    divElem.style.display = showFlag ? "none" : "";
    img.src = showFlag ? showImgPath : hideImgPath;
}

var targetWin1;
function openWin(url, h, w) {
    //http://msdn.microsoft.com/en-us/library/ms536651(VS.85).aspx
    var left = (screen.width / 2) - (w / 2);    
    var top = (screen.height / 2) - (h / 2);
    //Make sure if the window is opened second time old one is closed
    if (targetWin1 != null) targetWin1.close();
    //Open new window
    targetWin1 = window.open(url, "_blank", "toolbar=no, location=no, directories=no, status=no, menubar=no, scrollbars=no,"+
    " resizable=yes, copyhistory=no, width=" + w + ", height=" + h + ", top=" + top + ", left=" + left);

    return targetWin1;
}
function openWinScrollable(url, h, w) {
    //http://msdn.microsoft.com/en-us/library/ms536651(VS.85).aspx
    var left = (screen.width / 2) - (w / 2);
    var top = (screen.height / 2) - (h / 2);
    //Make sure if the window is opened second time old one is closed
    if (targetWin1 != null) targetWin1.close();
    //Open new window
    targetWin1 = window.open(url, "_blank", "toolbar=no, location=no, directories=no, status=no, menubar=no, scrollbars=yes," + 
    " resizable=yes, copyhistory=no, width=" + w + ", height=" + h + ", top=" + top + ", left=" + left);
    return targetWin1;
}

function setFocus(elemID) {    
    var elem = document.getElementById(elemID);
    if (elem == null) {
        elem = document.getElementsByName(elemID); //special case for MVC who don't render id!
        if (elem.length > 0) elem = elem[0];//If its a checkbox it'll have 2 of same name
    }
    try { elem.focus(); return; } catch (ex) { /*alert(elem + ":" + elemID + ":" + ex.message);*/ } //skip if id is wrong
}

function toggleTbody(tbod) {//http://bytes.com/topic/javascript/answers/147170-how-hide-table-rows-grouping-them-into-div//id
    if (document.getElementById) {
        // alert(tbod);
        // var tbod = document.getElementById(id);
        if (tbod && typeof tbod.className == 'string') {
            if (tbod.className == 'off') tbod.className = 'on';
            else tbod.className = 'off';
        }
    }
    return false;
}

function reviseLastGrpCount(prevID) {
    if ($("div.pager").length > 0) {//revise only if there's any pagination
        
        //jQ selector NOT working in IE!
        if ($('div.pager a[disabled]:last-child').length < 1) {//if its the last page then no need to add "+"
        
            $('#td' + prevID).append("+"); //'Count: ' + $('#' + prevID).siblings().length +
        }        
        //BUG: http://stackoverflow.com/questions/3046557/error-in-jquery-attribute-selector-and-ie6-7
        // IE renders only "disabled" not "disabled = disabled" 
    }
}

//Define a function variable which can be used and also re-initialized in pages
var sorterTextExtraction = function(node) {
var val = $(node).children('input:first-child'); // textbox
val = val.length < 1 ? $(node).children('select:first-child') : val; // dropdown
//return value
if(val.length < 1) return node.innerHTML;// text only
return (val.attr('checked') != null)? val.attr('checked'): val.val(); //checkbox or txt/drop
}

var doFurtherProcessing = function(success) { };
function showOprResult(spanId, success) {
    // Highlight, fadeOut and finally REMOVE!
    $(spanId).effect('highlight', {}, 4000).fadeOut((success == 1) ? 1000 : 8000, function () { $(spanId).html("&nbsp;").remove(); /* show();*/ });

    //Special case for forms which need to do post processing        
    //doFurtherProcessing(); HT: Handled at the end of effect call back    
    try { DisableSubmitButtons(false); /*$.unblockUI();*/ } catch (e) { }
}
function showNOTY(msg, success) {
    // Highlight, fadeOut and finally REMOVE!
    //$(spanId).effect('highlight', {}, 4000).fadeOut((success == 1) ? 1000 : 8000, function () { $(spanId).html("&nbsp;").remove(); /* show();*/ });
    noty({
        text: msg,
        type: success ? "success" : "error",
        dismissQueue: true,
        timeout: success ? 2000 : 7000,
        layout: 'topCenter',
        theme: 'defaultTheme',
        killer: true
    });

    //Special case for forms which need to do post processing        
    //doFurtherProcessing(); HT: Handled at the end of effect call back    
    try { DisableSubmitButtons(false); /*$.unblockUI();*/ } catch (e) { }
}
function setDefaultIfEmpty(txt, defaultStr) {// use with onblur (don't use with jQuery.validate class = "required"
    if ($(txt).val() == '') $(txt).val(defaultStr).trigger("change"); // make sure defaultStr is a string
}

function trimTextAreaMaxLen(txt,maxlen) { // requires jQuery    
    if (maxlen == null) 
        maxlen = $(txt).attr("maxlength");
    try { $(txt).val($(txt).val().slice(0, maxlen)).trigger("change"); } catch (ex) { return false; }
    return true;    
}

/*function trimTextAreaMaxLen(txt) { // requires jQuery
    try{var maxlen = $(txt).attr("maxlength");
    $(txt).val($(txt).val().slice(0, maxlen)).trigger("change");}
    catch(ex){;}
}*/

function chartPieSelectHandler(chartObj,dataTbl,keyPos) {

    var selectedItem = chartObj.getSelection()[0]; // [0] for pie

    if (selectedItem) { return dataTbl.getValue(selectedItem.row, keyPos); }
    else return null;

    /* Extra code kept for future ref
    var message = "";
    for (var i = 0; i < selection.length; i++) {
    var item = selection[i];
    if (item.row != null && item.column != null) {       message += '{row:' + item.row + ',column:' + item.column + '}';
    } else if (item.row != null) {                                      message += '{row:' + item.row + '}';
    } else if (item.column != null) {                               message += '{column:' + item.column + '}';
    }
    }
    if (message == '') {            message = 'nothing';          }
    alert('You selected ' + message);
    */
}
function DisableSubmitButtons(disable) {
    $('input[type = "submit"]').prop('disabled', disable);
}
function showDlg(show) {
    if (show && $("#divdlg")){
        $("#divdlg").dialog({ title: "", modal: true, height: 110, width: 50});
        $("#divdlg").parent().find(".ui-dialog-titlebar").hide();  // Hide title : http://forum.jquery.com/topic/ui-dialog-remove-title-bar
    }
    else if($("#divdlg"))
        $("#divdlg").dialog("destroy");
}

function createToFromjQDTP(FromDtpID, ToDtpID) {
    var ToDtpID1 = ToDtpID + "1";
    var FromDtpID1 = FromDtpID + "1";
    //NEW : http://jqueryui.com/datepicker/#date-range
    $(FromDtpID1).datepicker({
        defaultDate: "+1w",
        minDate: minSQLDate,
        maxDate: maxSQLDate,
        changeMonth: true,
        numberOfMonths: 3,
        altField: FromDtpID, altFormat: 'dd M yy',
        onSelect: function (selectedDate) { $(FromDtpID).trigger("change"); }, // DON'T : .val(selectedDate)
        onClose: function (selectedDate, inst) {
            $(ToDtpID1).datepicker("option", "minDate", selectedDate);
            if (selectedDate == '') { // special case SO : http://bugs.jqueryui.com/ticket/5734
                $(inst.settings["altField"]).val(selectedDate);
            }
            $(FromDtpID).trigger("change"); // Specially for KO (not FromDtpID1)
        }
    });
    $(ToDtpID1).datepicker({
        defaultDate: "+1w",
        changeMonth: true,
        numberOfMonths: 3,
        altField: ToDtpID, altFormat: 'dd M yy',
        onSelect: function (selectedDate) { $(ToDtpID).trigger("change"); }, // DON'T : .val(selectedDate)        
        onClose: function (selectedDate, inst) {
            $(FromDtpID1).datepicker("option", "maxDate", selectedDate);

            if (selectedDate == '') { // special case SO : http://bugs.jqueryui.com/ticket/5734
                $(inst.settings["altField"]).val(selectedDate);
            }
            $(ToDtpID).trigger("change"); // Specially for KO (not ToDtpID1)
        }
    });

    // Set format to be used by alt date field
    //$(FromDtpID1).datepicker("option", "altField", FromDtpID);    $(FromDtpID1).datepicker("option", "altFormat", 'dd-M-yy');
    //$(ToDtpID1).datepicker("option", "altField", ToDtpID);    $(ToDtpID1).datepicker("option", "altFormat", 'dd-M-yy');
}

function createjQDTP(DtpID) {
    var DtpID1 = "#" + DtpID + "Str";
    //NEW : http://jqueryui.com/datepicker/#date-range
    $(DtpID1).datepicker({
        minDate: minSQLDate,
        maxDate: maxSQLDate,
        changeMonth: true
    });
    // Set format to be used by alt date field
    $(DtpID1).datepicker("option", "altField", "#" + DtpID);
    $(DtpID1).datepicker("option", "altFormat", 'dd-M-yy');
}

function setAutofocus() { $('[autofocus]:not(:focus)').eq(0).focus(); }

String.prototype.Ufloat = function () {
    var val = parseFloat(this);
    return parseFloat(((val > 0) ? val : 0.00).toFixed(2));
}

String.prototype.Uint = function () {
    return parseInt((this > 0) ? this : 0); // DON'T as it might affect calculation .toFixed(2);
}

function setFocusEditableGrid(tableID, isFirstTROrLast) {
    var trPosition = isFirstTROrLast ? "tr:first" : "tr:last";    
    tableID = "#" + tableID;
    $(tableID).find(trPosition).find('input[class=editableTX],textarea,select').filter(':visible:first').focus();
}

/* --------------------------- jQuery.browser ----------------------------- */
var matched, browser;

jQuery.uaMatch = function (ua) {
    ua = ua.toLowerCase();

    var match = /(chrome)[ \/]([\w.]+)/.exec(ua) ||
        /(webkit)[ \/]([\w.]+)/.exec(ua) ||
        /(opera)(?:.*version|)[ \/]([\w.]+)/.exec(ua) ||
        /(msie)[\s?]([\w.]+)/.exec(ua) ||
        /(trident)(?:.*? rv:([\w.]+)|)/.exec(ua) ||
        ua.indexOf("compatible") < 0 && /(mozilla)(?:.*? rv:([\w.]+)|)/.exec(ua) ||
        [];

    return {
        browser: match[1] || "",
        version: match[2] || "0"
    };
};

matched = jQuery.uaMatch(navigator.userAgent);
//IE 11+ fix (Trident) 
matched.browser = matched.browser == 'trident' ? 'msie' : matched.browser;
browser = {};

if (matched.browser) {
    browser[matched.browser] = true;
    browser.version = matched.version;
}

// Chrome is Webkit, but Webkit is also Safari.
if (browser.chrome) {
    browser.webkit = true;
} else if (browser.webkit) {
    browser.safari = true;
}

jQuery.browser = browser;
// log removed - adds an extra dependency
//log(jQuery.browser)
/* --------------------------- jQuery.browser ----------------------------- */