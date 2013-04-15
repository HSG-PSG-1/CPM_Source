// Common global variables
var newTRId = "#trAddNew", newTRIdOnly = "trAddNew";
var lastTR = "#sortable tbody>tr:last";
var delTRClass = "deletedTR", visTRClass = "deletedTR_NO";

function addNewTR() { //Make AJAX GET call to fetch a new TR
    
    //clone and insert the tr after the table
    $(newTRId).clone(true).insertAfter(lastTR);
    $(lastTR).effect('highlight', {}, 500); //highlight
    
    var indx = $('#sortable tbody>tr').length - 1; //var tx1 = $("#tx1"), tx2 = $("#tx2"); tx2.val('');

    $(lastTR + ' td label[class="error"]').remove(); //remove errors (SO: 3028294)
    $(lastTR + ' td div[class="validation-summary-errors"]').hide();

    //replace the [2].id with the incremented [3].id        //Ref: SO - 6468306
    $(lastTR).each(function(index, value) {//Ref: http://api.jquery.com/prop/
        $(this).find('input, select').prop('name',
            function(i, e) { /* DEBUG:tx2.val(tx2.val() + "\n" + e + ":" + indx); */  
            return e.replace(/(\d+)/g, indx); });
    });
    $(lastTR + ' td input[type="text"]').removeClass("error"); //remove class if it was added
    //$(lastTR + ' td input[type="text"]:first').val('');// Reset value of Title - cretes confusion while deleting empty
    //DEBUG:    tx1.val($(lastTR).html());      //Ref: SO - 2145012    
    setAddDel($(lastTR), true);
}

function showHideNew(show, obj) {
    //img   span        td      tr
    var tr = (obj == null) ? $(newTRId)[0] : $(obj).parent().parent()[0];
    //getNthCHK(getFirstTD(tr), 0).checked = show;
    setAddDel(tr, show);
    tr.style.display = show ? "" : "none";

    //CAN'T remove tr if Newly inserted record is deleted (all but the first insert) - because it'll break the sequence and not be parsed in the list in action
    //if (!show && $('tr[id=' + newTRIdOnly + ']').length > 1) { $(tr).remove(); alert('removed.'); }
    if(!show)// reset Title
        $(tr).find('td:nth-child(2) input[type=text]:first').val('[Title]');
}

function setAddDel(tr, show) { 
    $(tr).find('td:first input:checkbox[name*=IsAdded]')[0].checked = show;
    $(tr).find('td:first input:checkbox[name*=IsDeleted]')[0].checked = !show;
}

//chk id postfix: IsAdded   IsDeleted   IsUpdated
function deleteTR(elem) {
    var tr, td, span, chkDel;
    //parent of the link clicked
    span = $(elem).parent()[0];    td = $(span).parent()[0];    tr = $(td).parent()[0]; // parent of TD
    //http://weblogs.asp.net/psperanza/archive/2009/05/07/jquery-selectors-selecting-elements-by-a-partial-id.aspx
    chkDel = $(td).find('input:checkbox[name*=IsDeleted]')[0]; //  getNthCHK(td, chkPos);
    chkDel.checked = chkDel.checked ? false : true; //toggle IsDeleted
    tr.className = (tr.className == delTRClass) ? visTRClass : delTRClass; // try: http://api.jquery.com/toggleClass/

    toggleImg(span); //used from common.js
}

// Return a helper with preserved width of cells 
//Ref: http: //www.foliotek.com/devblog/make-table-rows-sortable-using-jquery-ui-sortable/
var fixHelper = function(e, ui) {// or try this: SO: 1307705/jquery-ui-sortable-with-table-and-tr-width
    ui.children().each(function() {$(this).width($(this).width());});
    return ui;
};

// OR for future: http://stackoverflow.com/questions/6938301/better-javascript-library
// IE6 issue : http://stackoverflow.com/questions/859007/jquery-ui-sortable-problem-on-ie6
$("#sortable tbody").sortable({
    helper: fixHelper,
    cursor: 'move',

    update: function(e, ui) {
        $el = $(ui.item);
        $el.effect('highlight', {}, 2000);

        //Use for debug: alert($('#sortable tbody tr').length);

        $('#sortable tbody tr').each(
                function(currentIndex) {
                    $(this).find('td:nth-child(3)').find('input:first').val(this.rowIndex); //td:first
                });
    }
});