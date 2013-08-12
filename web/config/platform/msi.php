<?php
global $wgPathConvert, $wgPathIdentify, $wgPathPrince;
global $wgIsMSI;
global $wgPathConf, $wgAttachPath, $wgLucenePath;

$wgPathConvert = realpath('../../api/bin/convert.exe');
$wgPathIdentify = realpath('../../api/bin/identify.exe');
$wgPathPrince = realpath('../../redist/Prince/engine/bin/prince.exe');

$wgIsMSI = true;

$wgPathConf = realpath('../../api/bin');
$wgAttachPath = realpath('../../data/files');
$wgLucenePath = realpath('../../data/index').'\\$1';
