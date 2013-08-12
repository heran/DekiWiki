<?php
require_once('simpletest_boostrap.php');

// include classes for testing
require_once(TEST_CORE_ROOT . '/http_plug.php');

class HttpPlugTest extends UnitTestCase
{
	public function Test_String_Constructor()
	{
		$Expect = new EqualExpectation('http://www.mindtouch.com/foo');
		
		$Plug = new HttpPlug('http://www.mindtouch.com/foo');
		$this->assertTrue($Expect->test($Plug->GetUri()));
		
		$Plug = new HttpPlug('http://www.mindtouch.com');
		$Plug = $Plug->At('foo');
		$this->assertTrue($Expect->test($Plug->GetUri()));
		
		// MT-7254 PHP Plug accepts trailing slashes
		$Plug = new HttpPlug('http://www.mindtouch.com/');
		$Plug = $Plug->At('foo');
		$this->assertTrue($Expect->test($Plug->GetUri()));
	}
	
	public function Test_Array_Constructor()
	{
		$uri = 'https://www.mindtouch.com/foo/bar/test?q1=1&q2=2';
		$Expect = new EqualExpectation($uri);
		$parts = parse_url($uri);
		
		$Plug = new HttpPlug($parts);
		$this->assert(
			$Expect,
			$Plug->GetUri(),
			$Plug->GetUri()
		);
		
		unset($parts['scheme']);
		$Plug = new HttpPlug($parts);
		$this->assertFalse($Expect->test($Plug->GetUri()));
	}
}
