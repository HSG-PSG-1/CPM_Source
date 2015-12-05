(function () {
    (function ($) {
        return $.fn.tableNav = function () {
            var elementSelector = "input:enabled, select:enabled, textarea:enabled, a:visible"; // SO : 267615 - HT: instead of only input:enabled
            var $tbody, max_x, max_y, y;
            $tbody = $(this).find('tbody');
            $tbody.find('input:enabled').off('keyup.tablenav click.tablenav');
            max_x = $tbody.find('tr:first-child').find(elementSelector).not(':hidden').length - 1;
            max_y = $tbody.find('tr').length - 1;
            y = 0;
            $tbody.find('tr').each(function () {
                var x;
                x = 0;
                $(this).find(elementSelector).not(':hidden').each(function () { // find(elementSelector).not(':hidden') - to be able to add nav to other non visible tabs
                    $(this).on('click.tablenav', function () {
                        return $(this).focus(); // SO : 6263424 use focus() instead of select();
                    });
                    $(this).attr('data-x', x).attr('data-y', y);

                    //Special case for select (dropdown)
                    if ($(this).is("select")) {
                        $(this).attr('oldIndex', $(this)[0].selectedIndex);
                        $(this).on('mouseup', function (e) {
                            $(this).attr('oldIndex', $(this)[0].selectedIndex); 
                        });
                    }

                    $(this).on('keyup.tablenav', function (e) { // sequence : keydown, keypress, keyup
                        var new_x, new_y, old_x, old_y;
                        old_x = parseInt($(this).attr('data-x'), 10);
                        old_y = parseInt($(this).attr('data-y'), 10);
                        new_x = old_x;
                        new_y = old_y;
                        switch (e.which) {
                            case 37: // LEFT
                                new_x = old_x - 1;
                                break;
                            case 38: // UP
                                new_y = old_y - 1;
                                break;
                            case 39: // RIGHT
                                new_x = old_x + 1;
                                break;
                            case 40: // DOWN
                                new_y = old_y + 1;
                                break;
                            default: // if any other key was pressed then its ok!
                                if ($(this).is("select")) { $(this).attr('oldIndex', $(this)[0].selectedIndex); }
                                return;
                        }
                        //HT: DOESN'T work ! To stop dropdown from changing items (SO : 13992715)
                        if (e.preventDefault) e.preventDefault();
                        if (e.stopPropagation) e.stopPropagation();

                        new_x = new_x < 0 ? max_x : new_x;
                        new_x = new_x > max_x ? 0 : new_x;
                        new_y = new_y < 0 ? max_y : new_y;
                        new_y = new_y > max_y ? 0 : new_y;

                        if ($(this).is("select")) { // trigger change necessary for KO likedata binding things
                            $(this).prop('selectedIndex', parseInt($(this).attr("oldIndex"))).trigger("change");
                        }
                        if ($(this).hasClass("hasDatepicker")) {
                            $(this).datepicker("hide");
                        }

                        $tbody.find('input[data-x=' + new_x + '][data-y=' + new_y + '], select[data-x=' + new_x + '][data-y=' + new_y + '], textarea[data-x=' + new_x + '][data-y=' + new_y + '], a[data-x=' + new_x + '][data-y=' + new_y + ']').focus(); //click() (also added select & textarea;
                        
                        return false;
                    });
                    return x++;
                });
                return y++;
            });
            return $(this);
        };
    })(jQuery);

}).call(this);