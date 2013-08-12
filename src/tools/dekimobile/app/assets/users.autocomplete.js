$(function() {
    $("#text-username").autocomplete("login.php", { extraParams : { "params" : "ajax/find" } });
});
