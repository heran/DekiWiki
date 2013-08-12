<?php 
class OpenIdProperties extends DekiSiteProperties
{
    const NS_OPENID = 'urn:openid.draft#';

    public function __construct()
    {
        parent::__construct();
    }

    /**
     * @return string - false if not found
     */
    public function getUsername($openIdUsername)
    {
        $name = $this->munge($openIdUsername);
        return $this->get($name, false, self::NS_OPENID);
    }

    public function setUsername($openIdUsername, $mindtouchUsername)
    {
        $name = $this->munge($openIdUsername);
        $this->set($name, $mindtouchUsername, self::NS_OPENID);
        return parent::update();
    }

    protected function munge($u)
    {
        return $u; 
        // At this point I don't think any munging is necessary
        // str_replace('http://', '', $u);
    }
} 
