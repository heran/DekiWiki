<?php
/*
 * MindTouch Core - open source enterprise collaborative networking
 * Copyright (c) 2006-2010 MindTouch Inc.
 * www.mindtouch.com oss@mindtouch.com
 *
 * For community documentation and downloads visit www.opengarden.org;
 * please review the licensing section.
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License along
 * with this program; if not, write to the Free Software Foundation, Inc.,
 * 59 Temple Place - Suite 330, Boston, MA 02111-1307, USA.
 * http://www.gnu.org/copyleft/gpl.html
 */

define('DEKI_ADMIN', true);
require_once('index.php');


class ProductActivation extends DekiControlPanel
{
	// needed to determine the template folder
	protected $name = 'product_activation';
	
	public function index()
	{
		$this->executeAction('license');
	}
	
	// main listing view
	public function license()
	{
		global $wgDekiApiKey, $wgLang;
		
		if ($this->Request->isPost()) 
		{
			$Result = $this->Plug->At('license')->With('apikey', $wgDekiApiKey)->PutFile($_FILES['license']['tmp_name'], 'application/xml');
			if ($Result->handleResponse()) 
			{
				wfSetConfig('site/activation', null);
				wfSaveConfig();
				// todo: specific messages per-license
				DekiMessage::success($this->View->msg('Activation.success'));
				
				$this->Request->redirect($this->getUrl('/'));
				return;
			}
			else
			{
				DekiMessage::error($this->View->msg('Activation.error', ProductURL::SUPPORT));
			}
		}
		
		$License = DekiLicense::getCurrent();
		// we can get into a funky state where the config keys don't match up to the license
		if ($License->getLicenseType() == DekiSite::PRODUCT_COMMERCIAL && DekiSite::isCore()) 
		{
			DekiMessage::error($this->View->msg('Activation.error-unsigned', ProductURL::SUPPORT));	
		}
		
		// get the total number of non-deactivated users; limit 0 hopefully speeds this up
		$Result = $this->Plug->At('users')->With('activatedfilter', 'true')->With('limit', '0')->Get();
		$usercount = $Result->getVal('body/users/@querycount'); 
		$expired = $License->getExpirationDate();
		
		// supporting information
		$this->View->set('supporturl', ProductURL::SUPPORT);
		$highlight = $this->Request->getVal('highlight');
		if (!is_null($highlight))
		{
			$this->View->set('highlight.sales', $highlight == 'sales');
			$this->View->set('highlight.support', $highlight == 'support');
		}
		
		// license information
		$this->View->set('usercount', $usercount); 
		$this->View->set('site.expired', DekiSite::isExpired());
		$this->View->set('site.inactive', DekiSite::isInactive());
		$this->View->set('license.primary', $License->getPrimaryContact());
		$this->View->set('license.secondary', $License->getSecondaryContact());
		$this->View->set('license.expires', !empty($expired) ? $wgLang->date($expired): null); // community doesn't expire
		$this->View->set('license.date', $wgLang->date(mktime())); 
		$this->View->set('license.usercount', $License->getUserCount()); 
		$this->View->set('license.sitecount', $License->getSiteCount()); 
		$this->View->set('license.hosts', $License->getHosts());
		$sids = $License->getSids();
		foreach ($sids as $k => $v)
		{
			$sids[$k] = is_null($v) ? null: $wgLang->date($v, true);
		}
		$this->View->set('license.has.sids', !empty($sids));
		$this->View->set('license.sids', $sids);
		$this->View->set('license.licensee', $License->getLicensee());
		if (DekiSite::isInactive())
		{
			$this->View->set('license.type', 'Inactive');
		}
		else
		{
			$this->View->set('license.type', $License->getLicensedProduct());
		}
		$this->View->set('license.type.expired', $this->View->msg('Product.type.expired'));
		$this->View->set('license.terms', DekiLicense::getEmbeddedMSA());
		$this->View->set('license.productkey', DekiSite::getProductKey());
		$this->View->set('license.has.rating', $License->hasCapabilityRating());
		$this->View->set('license.has.anon', $License->hasCapabilityAnon());
		$this->View->set('license.has.search', $License->hasCapabilitySearch());
		$this->View->set('license.has.memcache', $License->hasCapabilityMemCache());
		$this->View->set('license.has.caching', $License->hasCapabilityCaching());
		$this->View->set('license.is.core', DekiSite::isCore());
		
		if ($this->canManageSeats)
		{
			$count = $License->getSeatCount();
			if ($count == -1)
			{
				// TODO: localize
				$count = 'Unlimited';
			}
			$this->View->set('license.seat.count', $count);
		}
		
		$this->View->set('form.action', $this->getUrl('/'));
		$this->View->output();
	}
}

new ProductActivation();
