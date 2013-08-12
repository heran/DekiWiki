<?php
class KalturaHelpers
{
	static $platfromConfig = null;

	function register($dekiName, $dekiEmail, &$secret, &$adminSecret, &$partner, $phone="", 
			 $description="", $describeYourself="", $webSiteUrl="", $contentCategory="",$adultContent=false)
	{
		$kConfig = new KalturaConfiguration(0);
		$kConfig->serviceUrl = KalturaSettings_SERVER_URL;
		$kClient = new KalturaClient($kConfig);
		$kPartner = new KalturaPartner();
		$kPartner -> name = $dekiName;
		$kPartner -> adminName = $dekiName;
		$kPartner -> adminEmail =  $dekiEmail;
		$kPartner -> phone = $phone;
		$kPartner -> describeYourself = $describeYourself;
		$kPartner -> website = $webSiteUrl;
		$kPartner -> contentCategories = $contentCategory;
		$kPartner -> adultContent = $adultContent;
		$kPartner -> description = $description . "\n|" . "MindTouch|" . (isset($wgProductVersion) ? $wgProductVersion : "");
		$kPartner -> commercialUse = "non-commercial_use";
		$kPartner -> type = 103;
		$kPartner = $kClient -> partner -> register ($kPartner);

		$partner  = $kPartner -> id;
		$secret = $kPartner -> secret;
    $adminSecret = $kPartner -> adminSecret;
	}

	function getContributionWizardFlashVars($ks, $kshowId=-2, $partner_data="", $type="", $comment=false)
	{
		$sessionUser = KalturaHelpers::getSessionUser();
		$config = KalturaHelpers::getServiceConfiguration();
		
		$flashVars = array();

		$flashVars["userId"] = $sessionUser->userId;
		$flashVars["sessionId"] = $ks;

		if ($sessionUserId == KalturaSettings_ANONYMOUS_USER_ID) {
			 $flashVars["isAnonymous"] = true;
		}
			
//		$flashVars["partnerId"] 	= 1;
//		$flashVars["subPartnerId"] 	= 100;
		$flashVars["partnerId"] 	= $config->partnerId;
//		$flashVars["subPartnerId"] 	= $config->subPartnerId;
		if ($kshowId)
			// TODO: change the following line for roughcut
			$flashVars["kshow_id"] 	= ($type == 'entry')? $type.'-'.$kshowId: $kshowId;
		else
			$flashVars["kshow_id"] 	= -2;
		
		$flashVars["afterAddentry"] 	= "onContributionWizardAfterAddEntry";
		$flashVars["close"] 		= "onContributionWizardClose";
		$flashVars["partnerData"] 	= $partner_data;
		
		if (!$comment)
			$flashVars["uiConfId"] 		= KalturaHelpers::getPlatformKey("uiconf/uploader",null);
		else
			$flashVars["uiConfId"] 		= KalturaSettings_CW_COMMENTS_UICONF_ID;
			
		$flashVars["terms_of_use"] 	= "http://corp.kaltura.com/tandc" ;
		
		return $flashVars;
	}
	
	function getSimpleEditorFlashVars($ks, $kshowId, $type, $partner_data)
	{
		$sessionUser = KalturaHelpers::getSessionUser();
		$config = KalturaHelpers::getServiceConfiguration();
		
		$flashVars = array();
		
		if($type == 'entry')
		{
			$flashVars["entry_id"] 		= $kshowId;
			$flashVars["kshow_id"] 		= 'entry-'.$kshowId;
		} else {
			$flashVars["entry_id"] 		= -1;
			$flashVars["kshow_id"] 		= $kshowId;
		}

		$flashVars["partner_id"] 	= $config->partnerId;;
		$flashVars["partnerData"] 	= $partner_data;
		$flashVars["subp_id"] 		= $config->subPartnerId;
		$flashVars["uid"] 			= $sessionUser->userId;
		$flashVars["ks"] 			= $ks;
		$flashVars["backF"] 		= "onSimpleEditorBackClick";
		$flashVars["saveF"] 		= "onSimpleEditorSaveClick";
		$flashVars["uiConfId"] 		= KalturaHelpers::getPlatformKey("uiconf/editor",null);
		
		return $flashVars;
	}
	
	function getKalturaPlayerFlashVars($ks, $kshowId = -1, $entryId = -1)
	{
		$sessionUser = KalturaHelpers::getSessionUser();
		$config = KalturaHelpers::getServiceConfiguration();
		
		$flashVars = array();
		
		$flashVars["kshowId"] 		= $kshowId;
		$flashVars["entryId"] 		= $entryId;
		$flashVars["partner_id"] 	= $config->partnerId;
//		$flashVars["subp_id"] 		= $config->subPartnerId;
		$flashVars["uid"] 			= $sessionUser->userId;
		$flashVars["ks"] 			= $ks;
		
		return $flashVars;
	}
	
	function flashVarsToString($flashVars)
	{
		$flashVarsStr = "";
		foreach($flashVars as $key => $value)
		{
			$flashVarsStr .= ($key . "=" . urlencode($value) . "&"); 
		}
		return substr($flashVarsStr, 0, strlen($flashVarsStr) - 1);
	}
	
	function getSwfUrlForBaseWidget() 
	{
		return KalturaHelpers::getSwfUrlForWidget(KalturaSettings_BASE_WIDGET_ID);
	}
	
	function getSwfUrlForWidget($widgetId)
	{
		return KalturaHelpers::getKalturaServerUrl() . "/kwidget/wid/" . $widgetId;
	}
	
	function getContributionWizardUrl($uiConfId = null)
	{
		if ($uiConfId)
			return KalturaHelpers::getKalturaServerUrl() . "/kcw/ui_conf_id/" . $uiConfId;
		else
			return KalturaHelpers::getKalturaServerUrl() . "/kcw/ui_conf_id/" . KalturaSettings_CW_UICONF_ID;
	}
	
	function getSimpleEditorUrl($uiConfId = null)
	{
		if ($uiConfId)
			return KalturaHelpers::getKalturaServerUrl() . "/kse/ui_conf_id/" . $uiConfId;
		else
			return KalturaHelpers::getKalturaServerUrl() . "/kse/ui_conf_id/" . KalturaSettings_SE_UICONF_ID;
	}
	
	function getThumbnailUrl($widgetId = null, $entryId = null, $width = 240, $height= 180)
	{
		$config = KalturaHelpers::getServiceConfiguration();
		$url = KalturaHelpers::getKalturaServerUrl();
		$url .= "/p/" . $config->partnerId;
		$url .= "/sp/" . $config->subPartnerId;
		$url .= "/thumbnail";
		if ($widgetId)
			$url .= "/widget_id/" . $widgetId;
		else if ($entryId)
			$url .= "/entry_id/" . $entryId;
		$url .= "/width/" . $width;
		$url .= "/height/" . $height;
		$url .= "/type/2";
		$url .= "/bgcolor/000000"; 
		return $url;
	}
	
	function getPlatformConfig() {
		if (self::$platfromConfig != null)
		{
			return self::$platfromConfig;
		}

		$activeServices = DekiService::getSiteList(DekiService::TYPE_EXTENSION, true);

		foreach ($activeServices as $aService)
		{	
			if ($aService->getName() == "Kaltura")
			{
				self::$platfromConfig = $aService;
				return $aService;
			}
		}
		return null;

	}

	function getPlatformKey($key = "", $default = "")
	{
/*		$config = KalturaHelpers::getPlatformConfig();
		if ($config != null)
		{ 
			return $config->getConfig($key,$default);
		}
		else
		{
			return $default;
		}*/
		$val = wfGetConfig("kaltura/" . $key);
		if ($val == null ||  strlen($val) == 0)
		{
			return $default;
		}
		return $val;
	}

	function getServiceConfiguration() {

		$partnerId = KalturaHelpers::getPlatformKey("partner-id","0");

		$config = new KalturaConfiguration($partnerId);
		$config->serviceUrl = KalturaHelpers::getKalturaServerUrl();
		$config->setLogger(new KalturaLogger());
		return $config;
	}
	
	function getKalturaServerUrl() {
		$url = KalturaHelpers::getPlatformKey("server-uri",KalturaSettings_SERVER_URL);
		if($url == '') $url = KalturaSettings_SERVER_URL;
		
		// remove the last slash from the url
		if (substr($url, strlen($url) - 1, 1) == '/')
			$url = substr($url, 0, strlen($url) - 1);
		return $url;
	}
	
	function getSessionUser() {
		$user = DekiUser::getCurrent();

		$kalturaUser = new KalturaUser();

		if ($user->getId()) {
			$kalturaUser->userId= $user->getId();
			$kalturaUser->screenName = $user->getFullname();			
		}
		else
		{
			$kalturaUser->userId = KalturaSettings_ANONYMOUS_USER_ID; 
		}

		return $kalturaUser;
	}
	
	function getKalturaClient($isAdmin = false, $privileges = null)
	{
		// get the configuration to use the kaltura client
		$kalturaConfig = KalturaHelpers::getServiceConfiguration();
		
		if(!$privileges) $privileges = 'edit:*';
		// inititialize the kaltura client using the above configurations
		$kalturaClient = new KalturaClient($kalturaConfig);
	
		// get the current logged in user
		$sessionUser = KalturaHelpers::getSessionUser();
		
		if ($isAdmin)
		{
			$adminSecret = variable_get("kaltura_admin_secret", "");
			$result = $kalturaClient->startSession($sessionUser, $adminSecret, true, $privileges);
		}
		else
		{
			$secret = variable_get("kaltura_secret", "");
			$result = $kalturaClient->startSession($sessionUser, $secret, false, $privileges);
		}
			
		if (count(@$result["error"]))
		{
			watchdog("kaltura", $result["error"][0]["code"] . " - " . $result["error"][0]["desc"]);
			return null;
		}
		else
		{
			// now lets get the session key
			$session = $result["result"]["ks"];
			
			// set the session so we can use other service methods
			$kalturaClient->setKs($session);
		}
		
		return $kalturaClient;
	}
}
?>
