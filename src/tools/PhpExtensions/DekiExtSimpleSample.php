<?php
include('DekiExt.php');

DekiExt(
    
    //extension title
    "Mindtouch Deki Extension Php Service",                           

    null,

    //returns "Hello World" as a string
    array(
     "sayHello():str" => "HelloWorld"
    )
);

function HelloWorld()
{
    return "Hello World";
}
?>
