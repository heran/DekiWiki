<?php
// BitRock installer configuration
global $wgPathConvert, $wgPathIdentify, $wgPathPrince;
global $wgIsWAMP;
global $wgDBadminuser, $wgDBadminpassword;

$pathRoot = 'C:\\Program Files\\MindTouch\\';

$wgPathConvert  = $pathRoot . 'imagemagick\\convert.exe';
$wgPathIdentify = $pathRoot . 'imagemagick\\identify.exe';
$wgPathPrince   = $pathRoot . 'Prince\\bin\\Prince.exe';

$wgIsWAMP = true;

$wgDBadminuser = "root";
$wgDBadminpassword = "password";

