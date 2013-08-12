<?php

define('TEST_ROOT', dirname(__FILE__));
define('TEST_WEB_ROOT', '../../../web');
define('TEST_CORE_ROOT', TEST_WEB_ROOT. '/deki/core');
define('TEST_SUITE_ROOT', TEST_ROOT . '/redist/simpletest-1.0.1');

// load the test suite dependencies
require_once(TEST_SUITE_ROOT .'/autorun.php');
require_once(TEST_SUITE_ROOT .'/expectation.php');
