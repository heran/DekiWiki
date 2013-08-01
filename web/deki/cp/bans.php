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

class Bans extends DekiControlPanel
{
	// needed to determine the template folder
	protected $name = 'bans';
	
	public function index() 
	{
		$this->executeAction('listing');
	}
	
	// this should be a constant, but their usage of the view makes that hard
	public function getExpiration() 
	{
		return array(
			'infinity' => $this->View->msg('Bans.expiry.forever'), 
			'86400' => $this->View->msg('Bans.expiry.1day'), 
			'432000' => $this->View->msg('Bans.expiry.5days'), 
			'1209600' => $this->View->msg('Bans.expiry.2weeks'), 
			'2629743' => $this->View->msg('Bans.expiry.1month'), 
			'7889229' => $this->View->msg('Bans.expiry.3months'), 
			'custom' => $this->View->msg('Bans.expiry.other')
		);
	}
	
	// this should be a constant, but their usage of the view makes that hard
	public function getExpiryTimestamps() 
	{
		return array('year' => '', 'month' => '', 'day' => '');
	}
	
	// this should be a constant, but their usage of the view makes that hard
	public function getTypes() 
	{
		return array(
			'username' => $this->View->msg('Bans.type.username'), 
			'ipaddress' => $this->View->msg('Bans.type.ipaddress')
		);
	}
	
	private function bulkDelete($items, $suppressmessage = false) 
	{
		if (is_string($items)) 
		{
			$items = array($items);	
		}
		if (empty($items)) 
		{
			return;
		}
		foreach ($items as $banId) 
		{
			$Result = $this->Plug->At('site', 'bans', $banId)->Delete();
			if (!$suppressmessage) 
			{
				$Result->handleResponse();
			}
		}
		if (!$suppressmessage) 
		{
			DekiMessage::success($this->View->msg('Bans.success.deleted'));
		}
	}
	
	public function add() 
	{
		if ($this->Request->isPost())
		{
			if ($this->process(
				$this->Request->getVal('type'), 
				$this->Request->getVal('user'), 
				$this->Request->getVal('expiry'), 
				array(
					$this->Request->getVal('banYear', 0), 
					$this->Request->getVal('banMonth', 0), 
					$this->Request->getVal('banDay', 0)
				),
				$this->Request->getVal('reason'), 
				0 //new ban
			)) 
			{
				if ($this->Request->getVal('returnto')) 
				{
					DekiMessage::ui($this->View->msg('Bans.success.created'), 'success');
					$this->Request->redirect($this->Request->getVal('returnto'));
				}
				else
				{
					DekiMessage::success($this->View->msg('Bans.success.created'));
					$this->Request->redirect($this->getUrl('listing'));
				}
				return;	
			}
		}
		
		$Ban = new DekiBan;
		$this->form($Ban);
		$this->View->output();
	}
	
	public function edit() 
	{
		if ($this->Request->isPost())
		{
			if ($this->process(
				$this->Request->getVal('type'), 
				$this->Request->getVal('user'), 
				$this->Request->getVal('expiry'), 
				array(
					$this->Request->getVal('banYear', 0), 
					$this->Request->getVal('banMonth', 0), 
					$this->Request->getVal('banDay', 0)
				),
				$this->Request->getVal('reason'), 
				$this->Request->getVal('banid')
			))
			{
				DekiMessage::success($this->View->msg('Bans.success.updated'));
				$this->Request->redirect($this->getUrl('listing'));
				return;	
			}
		}
		
		$Ban = new DekiBan;
		$banId = $this->Request->getVal('id');
		if ($banId > 0) 
		{
			$Response = $this->Plug->At('site', 'bans', $banId)->Get();
			if ($Response->handleResponse()) 
			{
				$ban = $Response->getVal('body/ban');
				$Ban = DekiBan::newFromArray($ban);
				$submittext = $this->View->msg('Bans.edit.button');
			}
		}
		$this->form($Ban);
		$this->View->set('form.submit', DekiForm::singleInput('button', 'submit', 'submit', array(), $this->View->msg('Bans.edit.button')));
		$this->View->output();
	}
	
	public function form($Ban) 
	{
		
		$expiryType = $Ban->getId() > 0 && !is_null($Ban->getExpiry(null)) ? 'custom': null;
		$expiryts = $expiryType == 'custom' ? $Ban->getExpiry(null, true): $this->getExpiryTimestamps();
		
		$this->View->set('form.action', $this->getUrl('add'));
		$this->View->set('form.back', $this->getUrl('listing'));
		$this->View->set('form.submit', DekiForm::singleInput('button', 'submit', 'submit', array(), $this->View->msg('Bans.add.button')));
		$this->View->set('form.id', DekiForm::singleInput('hidden', 'banid', $Ban->getid()));
		
		$this->View->set('form.expires', DekiForm::multipleInput('select', 'expiry', $this->getExpiration(), $expiryType, array('id' => 'expiryType'))
				.'<span id="custom-date" style="'.($expiryType == 'custom' || true ? 'display: inline;': 'display:none;').'">'
				.$this->View->msg('Bans.form.customdate').' '
				.DekiForm::singleInput('text', 'banYear', $this->Request->getVal('banYear', $expiryts['year']), array('maxlength' => 4, 'size' => 4)).'/'
				.DekiForm::singleInput('text', 'banMonth', $this->Request->getVal('banMonth', $expiryts['month']), array('maxlength' => 2, 'size' => 2)).'/'
				.DekiForm::singleInput('text', 'banDay', $this->Request->getVal('banDay', $expiryts['day']), array('maxlength' => 2, 'size' => 2)).'</span>'
		);
		$this->View->set('form.user', DekiForm::singleInput('text', 'user', $Ban->getBannedName()));
		$this->View->set('form.type', DekiForm::multipleInput('radio', 'type', array('username' => $this->View->msg('Bans.type.username'), 'ip' => $this->View->msg('Bans.type.ipaddress')), $Ban->getBanType('username')));
		$this->View->set('form.reason', DekiForm::singleInput('text', 'reason', $Ban->getReason()));
	}
	
	private function process($type, $user, $expiry, $expiryts = array(), $reason = '', $banid = 0) 
	{
		$User = DekiUser::getCurrent();
		
		//needs data to ban
		if (empty($type)) 
		{
			DekiMessage::error($this->View->msg('Bans.error.type'));
			return false;	
		}
		if (empty($user)) 
		{
			DekiMessage::error($this->View->msg('Bans.error.empty'));
			return false;
		}
		if ($expiry == 'custom' && (empty($expiryts[1]) || empty($expiryts[2]) || empty($expiryts[0]))) 
		{
			DekiMessage::error($this->View->msg('Bans.error.date'));
			return false;
		}
		
		$absts = $expiry == 'custom' 
			? wfTimestamp(TS_DREAM, mktime(0, 0, 0, (int)$expiryts[1], (int)$expiryts[2], (int)$expiryts[0]))
			: null;
			
		$Ban = new DekiBan;
		$Ban->setExpiry($expiry, $absts);
		$Ban->setReason($reason);
		$Ban->setBannedUser($type, $user);
		
		// some simple ip validation; ipv4 and ipv6 covered
		if ($Ban->getBanType() == 'ip') 
		{
			if (!preg_match('(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}|\w{1,4}:\w{1,4}:\w{1,4}:\w{1,4}:\w{1,4}:\w{1,4}:\w{1,4}:\w{1,4})', $user)) 
			{
				DekiMessage::error($this->View->msg('Bans.error.badip'));
				return false;
			}	
			$Ban->Bannee = null;
		}
		
		// invalid user
		if ($Ban->getBanType() == 'username' && !$Ban->isValidUser()) 
		{
			DekiMessage::error($this->View->msg('Bans.error.baduser'));	
			return false;
		}
				
		if (!is_null($Ban->Bannee) && $Ban->Bannee->getId() == $User->getId()) 
		{
			DekiMessage::error($this->View->msg('Bans.error.self'));
			return false;	
		}
		
		// Bug MT-9895: cannot ban owner or admin account
		if (!is_null($Ban->Bannee) && ($Ban->Bannee->isAdmin() || $Ban->Bannee->isSiteOwner()))
		{
			DekiMessage::error($this->View->msg('Bans.error.baduser.restricted'));
			return false;
		}
		
		// edit is del + add
		if ($banid > 0) 
		{
			$this->bulkDelete($banid, true);
		}
		$Response = $this->Plug->At('site', 'bans')->Post($Ban->toArray());
		if ($Response->handleResponse())
		{
			return true;
		}
		return false;
	}
		
	// main listing view
	public function listing()
	{
		global $wgLang;
		
		if ($this->Request->isPost()) 
		{
			switch ($this->Request->getVal('action')) 
			{
				case 'delete': 
					$this->executeAction('delete');
					return;
			}
		}
		
		// get bans
		$Result = $this->Plug->At('site', 'bans')->Get();
		$Result->handleResponse();
		$bans = $Result->getAll('body/bans/ban', array());
		$Table = new DomTable();
		$Table->setColWidths('110', '100', '100', '100', '210', '80');

		$Table->addRow();
		$Table->addHeading(DekiForm::singleInput('checkbox', '', '', array(), $this->View->msg('Bans.data.user')));
		$Table->addHeading($this->View->msg('Bans.data.expiry'));
		$Table->addHeading($this->View->msg('Bans.data.timestamp'));
		$Table->addHeading($this->View->msg('Bans.data.by'));
		$Th = $Table->addHeading($this->View->msg('Bans.data.reason'));
		$Th = $Table->addHeading('&nbsp;');
		$Th->addClass('edit last');
		
		if (empty($bans)) 
		{
			$Table->setAttribute('class', 'table none');
			$Table->addRow();
			$Td = $Table->addCol('<div class="none">'.$this->View->msg('Bans.data.empty').'</div>');
			$Td->setAttribute('colspan', 6);
			$Td->setAttribute('class', 'last');
		}
		else
		{
			foreach ($bans as $ban) 
			{
				$Ban = DekiBan::newFromArray($ban);
				$Table->addRow();
				$Table->addCol(DekiForm::singleInput('checkbox', 'ban_id['.$Ban->getId().']', $Ban->getId(), array(), htmlspecialchars($Ban->getBannedName())));
				$Table->addCol(!is_null($Ban->getExpiry()) ? $wgLang->date($Ban->getExpiry()): $this->View->msg('Bans.expiry.never'));
				$Table->addCol(htmlspecialchars($Ban->getModified()));
				$Table->addCol(htmlspecialchars($Ban->getBannorName()));
				$Td = $Table->addCol(htmlspecialchars($Ban->getReason()));
				$Td = $Table->addCol(sprintf('<a href="%s" class="edit">%s</a>', $this->getUrl('/edit', array('id' => wfArrayVal($ban, '@id'))), $this->View->msg('Bans.data.edit')));
				$Td->addClass('last edit');
			}
		}
		
		$this->View->set('listing', $Table->saveHtml());
		$this->View->set('form.action', $this->getUrl('/'));
		
		$this->View->output();
	}

	public function delete()
	{
		$banIds = $this->Request->getArray('ban_id');
		$operate = $this->Request->getBool('operate');

		if (!$this->Request->isPost())
		{
			$this->Request->redirect($this->getUrl('listing'));
			return;
		}
		else if (empty($banIds))
		{
			DekiMessage::error($this->View->msg('Bans.data.no-selection'));
			$this->Request->redirect($this->getUrl('listing'));
			return;
		}

		if ($operate)
		{
			$this->bulkDelete($banIds);

			$this->Request->redirect($this->getUrl('listing'));
			return;
		}

		$html = '';
		foreach ($banIds as $banId)
		{
			$Ban = DekiBan::newFromId($banId);
			if (!is_null($Ban))
			{
				$html .= '<li>'. $Ban->toHtml() .'<input type="hidden" name="ban_id[]" value="'. $Ban->getId() .'" />'.'</li>';
			}
		}

		$this->View->set('form.action', $this->getUrl('delete'));
		$this->View->set('form.back', $this->getUrl('listing'));
		
		$this->View->set('bans-list', $html);

		$this->View->output();
	}
}

new Bans();

