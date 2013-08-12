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

if (defined('MINDTOUCH_DEKI'))
{
	DekiPlugin::registerHook(Hooks::SPECIAL_PAGE_RESTRICT, 'wfSpecialPageRestrictions');
}

function wfSpecialPageRestrictions($pageName, &$pageTitle, &$html, &$subhtml)
{
	$Special = new SpecialPageRestrictions($pageName, basename(__FILE__, '.php'));
	
	// set the page title
	$pageTitle = $Special->getPageTitle();
	$Special->output($html, $subhtml);	
}

class SpecialPageRestrictions extends SpecialPagePlugin
{
	protected $pageName = 'PageRestrictions';

	public function output(&$html, &$subhtml)
	{
		$Request = DekiRequest::getInstance();
		
		$pageId = $Request->getInt('id', 0);
		// attempt to fetch the page information
		$Title = Title::newFromId($pageId);		
		
		if (is_null($Title))
		{
			self::redirectHome();
			return;
		}
		
		// load the requested article
		$Article = new Article($Title);

		// begin hack
		global $wgBlockedNamespaces;
		$lastBlockedNamespaces = $wgBlockedNamespaces;
		// hack part.1, temporarily block the special namespace to avoid showing edit details
		if ($key = array_search(NS_SPECIAL, $wgBlockedNamespaces)) {
			unset($wgBlockedNamespaces[$key]);	
		}
		
		if (!$Article->userCanRestrict())
		{
			DekiMessage::error(wfMsg('Page.PageRestrictions.error.cannotrestrict'));
			self::redirect($Title->getLocalUrl());
			return;	
		}

		// hack part.2, restore the blockednamespaces
		$wgBlockedNamespaces = $lastBlockedNamespaces;

		if ($Request->isPost())
		{
			if ($this->setRestriction($Article)) 
			{
				$SpecialTitle = Title::newFromText($this->pageName, NS_SPECIAL);
				self::redirect($Request->getVal('add') || $Request->getVal('remove') 
					? $SpecialTitle->getLocalUrl('id='.$Request->getVal('id'))
					: $Article->getTitle()->getLocalUrl()
				);
				return;
			}
		}
			
		$html = $this->getRestrictForm($Article);
		$subhtml = wfMsg('Page.PageRestrictions.return-to', $Title->getLocalUrl(), $Title->getPrefixedText()); 		
		return $html;
	}
	
	public function setRestriction($Article) 
	{
		$Request = DekiRequest::getInstance();
		$User = DekiUser::getCurrent();
		
		// determine how to cascade the changes
		$cascade = 'none'; 
		if ($Request->getVal('subpages'))
		{
			$cascade = $Request->getVal('cascade');
		}
		
		// first thing, let's grab the list of users that currently exist
		$Security = new XArray($Article->getSecurity());
		
		// determine the type of update
		$type = $Security->getVal('body/security/permissions.page/restriction/#text');
		if ($Request->getVal('restrictType') != $type) 
		{
			$type = $Request->getVal('restrictType');	
		}
		
		// get all the existing grants
		$grants = $Security->getAll('body/security/grants/grant', array());
		
		// used to generate the XML later for sending to the API
		$users = array(/* $userid => $role */);
		$groups = array(/* $groupid => $role */);
		
		// don't lock this user out
		if (!array_key_exists($User->getId(), $users))
		{
			global $wgDefaultRestrictRole;
			$users[$User->getId()] = $wgDefaultRestrictRole;
		}
		
		// loop through and grab each grant
		if (!empty($grants))
		{
			foreach ($grants as $grant) 
			{
				$Grant = new DekiResult($grant);
				if ($Grant->getVal('user/@id', 0) > 0) 
				{
					$users[$Grant->getVal('user/@id')] = $Grant->getVal('permissions/role/#text');	
				}
				elseif ($Grant->getVal('group/@id', 0) > 0) 
				{
					$groups[$Grant->getVal('group/@id')] = $Grant->getVal('permissions/role/#text');
				}
			}
		}
		
		// determine the post type, normal vs javascript
		if ($Request->has('progressive'))
		{
			// check for removed grants before adding new ones
			$removeGrants = $Request->getArray('remove_grants');
			foreach ($removeGrants as $grantType => &$grants)
			{
				foreach ($grants as $id => $role)
				{
					if ($grantType == 'g')
					{
						// new group grant
						unset($groups[$id]);
					}
					else
					{
						//can't lock yourself out of the page!
						if ($id == $User->getId()) {
							continue;
						}
						// new user grant
						unset($users[$id]);
					}
				}
			}
			unset($grants);
			
			// posted new_grants are generated via javascript
			$newGrants = $Request->getArray('new_grants');
			foreach ($newGrants as $grantType => &$grants)
			{
				foreach ($grants as $id => $role)
				{
					if ($grantType == 'g')
					{
						// new group grant
						$groups[$id] = $role;
					}
					else
					{
						// new user grant
						$users[$id] = $role;
					}
				}
			}
			unset($grants);
		}
		else
		// normal form post	
		{
			// if we're adding a new user to the list
			if ($Request->getVal('add')) 
			{
				
				$nu = User::newFromName($Request->getVal('matchuser'));
				if (is_null($nu) || $nu->getId() == 0) 
				{
					wfMessagePush('general', wfMsg('Page.PageRestrictions.error.user'));				
					return false;
				}
				$users[$nu->getId()] = $Request->getVal('role');
			}
			
			// if we're removing users/groups from this list
			if ($Request->getVal('remove')) 
			{
				$list = $Request->getVal('list');
				if (!empty($list)) 
				{
					if (!empty($list['u'])) 
					{
						foreach ($list['u'] as $userid => $junk) 
						{
							//can't lock yourself out of the page!
							if ($userid == $User->getId()) 
							{
								continue;
							}
							unset($users[$userid]);	
						}
					}	
					if (!empty($list['g'])) 
					{
						foreach ($list['g'] as $groupid => $junk) 
						{
							unset($groups[$groupid]);	
						}
					}
				}
			}
		}
		
		$grants = array();
		$list = array();
		foreach ($users as $userId => $role)
		{
			$grants[] = array('permissions' => array('role' => $role), 'user' => array('@id' => $userId));
			$DekiUser = DekiUser::newfromId($userId);
			$list[] = $DekiUser->getName(). ' ('.$role.')';
		}

		foreach ($groups as $groupId => $role)
		{
			$grants[] = array('permissions' => array('role' => $role), 'group' => array('@id' => $groupId));
			$DekiGroup = DekiGroup::newfromId($groupId);
			$list[] = $DekiGroup->getName(). wfMsg('System.Common.group-suffix').' ('.$role.')';
		}
	
		//generate the XML document to PUT for grant list
		$xml = array(
			'security' => array(
				'permissions.page' => array(
					'restriction' => $type
				),
				'grants' => array(
					'@restriction' => $type,
					 array('grant' => $grants)
				)
			)
		);
		
		// apply the new permissions
		$Plug = DekiPlug::getInstance()->At('pages', $Article->getId(), 'security')->With('cascade', $cascade);
		$Result = $Plug->Put($xml);
		
		if ($Result->handleResponse())
		{
			$message = wfMsg('Page.PageRestrictions.success-updated', wfMsg('Page.PageRestrictions.type-'.strtolower($type)));
			if ($type != 'Public') 
			{
				$message .= ' ' . wfMsg('Page.PageRestrictions.success-updated-list', implode(', ', $list));
			}
			wfMessagePush('general', $message, 'success');
			return true;
		}
		
		return false;
	}
	
	public function getRestrictForm($Article)
	{
		global $wgDefaultRestrictRole, $wgOut, $wgTitle;
		$Request = DekiRequest::getInstance();
		
		$html = '';
		
		$Security = new DekiResult($Article->getSecurity());
		
		$html.= '<form method="post" action="'.$wgTitle->getLocalUrl('id='.$Request->getVal('id')).'" class="page-restrict">';
		$html.= '<div class="current">'.wfMsg('Page.PageRestrictions.restriction.description', $Article->getTitle()->getDisplayText()).'</div>';
		$html.= '<div class="options">'.DekiForm::multipleInput(
			'radio', 
			'restrictType', 
			array(
				'Public' => wfMsg('Page.PageRestrictions.desc-public'), 
				'Semi-Public' => wfMsg('Page.PageRestrictions.desc-semipublic'),
				'Private' => wfMsg('Page.PageRestrictions.desc-private')
			), 
			$Security->getVal('body/security/permissions.page/restriction/#text', 'Public')
		).'</div>';
		$siteRoles = DekiRole::getSiteList();
		$roles = array();
		foreach ($siteRoles as $Role) 
		{
			$roles[$Role->getName()] = $Role->getDisplayName();
		}

		// add the css & javascript
		$this->includeSpecialCss('special_pagerestrictions.css');
		$this->includeSpecialJavascript('special_pagerestrictions.js');
		
		// @TODO: use SpecialForm::getUserAutocomplete()
		$wgOut->addHeadHTML('
			<script type="text/javascript" src="/skins/common/yui/datasource/datasource.js"></script>
			<script type="text/javascript" src="/skins/common/yui/json/json.js"></script>
			<script type="text/javascript" src="/skins/common/yui/autocomplete/autocomplete.js"></script>			
			<link rel="stylesheet" type="text/css" media="screen" href="/skins/common/yui/autocomplete/autocomplete.css"/>
		
			<script type="text/javascript">
			YAHOO.util.Event.onContentReady("autoCompInput",
				function()
				{
					var dataSource	= new YAHOO.util.XHRDataSource("/deki/gui/usergroupsearch.php");
					dataSource.responseType = YAHOO.util.XHRDataSource.TYPE_JSON;
					dataSource.responseSchema = {
						resultsList : "results",
						fields : [
							"item",
							{key: "id"}
						]
					};
	
					var autoComplete 		= new YAHOO.widget.AutoComplete("autoCompInput", "autoCompContainer", dataSource);
					autoComplete.animVert 	= false;
	
					autoComplete.generateRequest = function(sQuery) {
						return "?mode=usersandgroups&query=" + sQuery;
					};
					
					var itemSelectHandler = function(sType, aArgs) {
						var oAcInstance = aArgs[0];
						var oData = aArgs[2];
						
						if ( oData && oData[0] )
						{
							oAcInstance._elTextbox.value = Deki.$.htmlDecode(oData[1] ? oData[1] : oData[0]);
						}
					};
					
					autoComplete.itemSelectEvent.subscribe(itemSelectHandler);					
				}
			);</script>');
		
		$html .= '<div class="grants">';
		$html .= '<h3>'.wfMsg('Page.PageRestrictions.grant.title').'</h3>'
			.'<div class="add">'
			// royk: this sucks, but using floats and overflow to clear the floats hides the autocomplete box
			.'<div class="form">'
			.'<table border="0" cellspacing="0" cellpadding="0">'
			.'<tr>'
			.'<td>'			
			.wfMsg('Page.PageRestrictions.grant.input')
			.'<div id="autoComplete">'
			.DekiForm::singleInput('text', 'matchuser', '', array('id' => 'autoCompInput'))
			.'<span id="deki-validuser" style="display:none;">'.wfMsg('Dialog.Restrict.error-invalid-user').'</span>'
			.'<div id="autoCompContainer"></div></div>'
			.'</td>'
			.'<td>'
			.wfMsg('Page.PageRestrictions.grant.roles').'<br/>'
			.DekiForm::multipleInput('select', 'role', $roles, $wgDefaultRestrictRole)
			.'</td>'
			.'<td>'
			.'<div class="submit">'.DekiForm::singleInput('button', 'add', wfMsg('Page.PageRestrictions.grant.button'), array('id' => 'deki-addgrant'), wfMsg('Page.PageRestrictions.grant.button')).'</div>'
			.'</td>'
			.'</tr>'
			.'</table>'
			.'</div>'
			.DekiForm::singleInput('hidden', 'processAdd', '', array('id' => 'processAdd'))
			.'</div>';
		
		$restrictList = $this->getRestrictionList($Article);
		// guerrics: always show the grantlist for javascript
		$html .= '<div class="grantlist">';
		$html .= '<h4>' . wfMsg('Page.PageRestrictions.grant.subtitle') . '</h4>';
		$html .= '<ul>';
		
		// first list item is for PE
		// changes here should also be reflected below
		$label =
			'<span class="name">'.
				'None' .
			'</span> '.
			'<span class="role">'.
				'None'.
			'</span> '.
			'<a href="#" class="remove-grant"><span>'. wfMsg('Page.PageRestrictions.grant.remove') .'</span></a>'
		;
		// hide the element, it's only used for cloning
		$html .= 
		'<li class="pe-template" style="display: none;">'.
			DekiForm::singleInput(
				'checkbox',
				'pe-template',
				'None',
				array('class' => 'remove-grant'),
				$label
			).
		'</li>';
		// /first item

		if (!empty($restrictList)) 
		{
			foreach ($restrictList as $restriction) 
			{
				$Result = new DekiResult($restriction);

				// get the role name for the checkbox
				$roleName = $Result->getVal('permissions/role/#text');
				
				// user
				if ($Result->getVal('user/@id', 0) > 0) 
				{
					$name = $Result->getVal('user/username');
					$fullname = $Result->getVal('user/fullname');
					if (!empty($fullname))
					{
						$name .= ' ('. $fullname .')';
					}
					$id = $Result->getVal('user/@id', 0);
					$prefix = 'u';
					$class = 'user';
				}
				// group
				elseif ($Result->getVal('group/@id', 0) > 0) 
				{
					$name = $Result->getVal('group/groupname');
					$id = $Result->getVal('group/@id', 0);
					$prefix = 'g';
					$class = 'group';
				}
				else 
				{
					continue;
				}
				
				// changes here should also be reflected in the first element
				$label =
					'<span class="name">'.
						htmlspecialchars($name) .
					'</span> '.
					'<span class="role">'.
						htmlspecialchars($Result->getVal('permissions/role/#text')).
					'</span> '.
					'<a href="#" style="display: none;" class="remove-grant"><span>'.
						wfMsg('Page.PageRestrictions.grant.remove') 
					.'</span></a>'
				;
				
				$html.= 
				'<li class="'.$class.'">'.
					DekiForm::singleInput(
						'checkbox',
						'list['.$prefix.']['.$id.']',
						$roleName,
						array('class' => 'remove-grant'),
						$label
					).
				'</li>';
			}
		}
		$html .= '</ul>';
		$html .= '<div class="remove">'.DekiForm::singleInput('button', 'remove', 'remove', array('id' => 'deki-removegrant', 'class' => 'remove-grant'), wfMsg('Page.PageRestrictions.form.remove')).'</div></div>';
		$html .= '</div>';
		// end grantlist
		
		// cascade options
		$options = array(
			'delta' => wfMsg('Page.PageRestrictions.cascade.delta'),
			'absolute' => wfMsg('Page.PageRestrictions.cascade.absolute'),
		);
			
		$html .= '<div class="cascade"><div>'.DekiForm::singleInput('checkbox', 'subpages', null, array(), wfMsg('Page.PageRestrictions.recursive')).'</div>';
		$html .= '<div id="deki-cascade">'.DekiForm::multipleInput('radio', 'cascade', $options, 'delta').'</div>';
		$html .= '</div>';
		
		$cancelUrl = $Article->getTitle()->getLocalUrl();
		$cancelPageTitle = htmlspecialchars($Article->getTitle()->getDisplayText());
		$html.= '<div class="submit">'.
			DekiForm::singleInput('button', 'save', 'save', array(), wfMsg('Page.PageRestrictions.form.submit'))
			. '<span class="or">'
			.wfMsg('Page.PageRestrictions.form.cancel', $cancelUrl, $cancelPageTitle)
			.'</span>'
		.'</div>';
		
		$html.= '</form>';
		return $html;
	}
	
	// this needs to be a part of article
	public function getRestrictionList($Article) 
	{
		// returns old plug format
		$security = $Article->getSecurity();
		
		// create deki result object from old plug format
		$Result = new DekiResult($security);
		$List = $Result->getAll('body/security/grants/grant', array());
		return $List;
	}
}
