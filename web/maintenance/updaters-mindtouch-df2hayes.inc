<?php
/**
 * @package MediaWiki
 * @subpackage Maintenance
 */

# function called from update-db.php to apply all new schema changes
function do_all_updates() {
	do_combo_permission_update();
	add_pages_contenttype();
	do_misc_updates();
	do_attachments_update();
	do_tags_update();
	do_df2hayes_update();
	do_specialpages_update();
	do_hidden_roles_update();
	do_users_user_builtin_update();
	do_tags_tag_type_update();
	do_users_unique_keys_update();
	do_old_content_type_update();
	do_archive_ar_last_page_id_update();
	do_df2hayes_misc();
	do_special_pages_update();
	do_settings_update();
	do_user_registration_update();
	do_extension_services_update();
	do_archive_id();
	do_service_sid_nullable_update();
	do_admin_pages_update();
	do_comments_update();
	do_users_serviceid_update();
	do_archive_ar_old_id_update();
	do_convert_web_to_feed_service();
	do_add_users_ext_column_update();
	do_attach_nullable_update();
	do_add_sitesettings_update();
	do_anonymous_user_update();
	do_requestlog_update();
	do_drop_unused_tables();
	do_utf8_table_conversion();
	do_fix_pages();
	do_default_user_update();
    do_admin_page_update();
	do_tags_181d_update();
	do_unhide_none_role();
	do_change_private_permission();
	//here ends 1.8.2 database updates
	do_rename_admin_pages();
	do_remove_hidden_roles();
	do_add_attachment_indices();
	do_hosted_version_pages();
	do_hosted_roles_update();
	//here ends 1.8.3 database updates
	
	//here starts 1.9.0 database updates
	do_add_page_parent_index();
	do_page_template_id_update();
	
	//here starts 1.10.0 database updates
	do_config_languages_update();	
}

// add page_template_id column in pages table
function do_page_template_id_update() {
	global $wgDatabase;
	$meta = $wgDatabase->fieldInfo('pages', 'page_template_id');
	if ($meta === false) {
		$wgDatabase->query('ALTER TABLE `pages` ADD page_template_id INT(8) DEFAULT NULL');
		echo("added page_template_id column to pages table\n");
	} 
}

//adds an index to page_parent in pages table
function do_add_page_parent_index() {
	global $wgDatabase;
	$meta = $wgDatabase->fieldInfo('pages', 'page_parent');
	if ($meta->multiple_key == 0) 
	{
		$wgDatabase->query("ALTER table `pages` ADD index `page_parent` (`page_parent`);");
		echo("<li>Added index `page_parent` to `pages`</li>\n");
	}
}

function do_add_attachment_indices() {
	global $wgDatabase;
	$meta = $wgDatabase->fieldInfo('attachments', 'at_from');
	if ($meta === false) 
	{
		return;	
	}
	if ($meta->multiple_key == 0) 
	{
		$wgDatabase->query("ALTER TABLE `attachments` ADD INDEX `name_ext_from` (`at_from`, `at_extension`, `at_name`) ");
		echo("<li>Updated `attachments`: Added indices</li>\n");	
	}
}

function do_remove_hidden_roles() {
	global $wgDatabase;
	$meta = $wgDatabase->fieldInfo('roles', 'role_hidden');
	//field doesn't exist, update already done
	if ($meta === false) {
		return;
	}
	$wgDatabase->query("ALTER TABLE `roles` DROP `role_hidden`");
	echo("<li>Updated `roles`: dropped unused column role_hidden</li>\n");
}

function do_rename_admin_pages() {
	global $AdminRedirectedPages, $wgDatabase;
	foreach ($AdminRedirectedPages as $old => $new) {
		$q = $wgDatabase->select('pages', array('page_id'), array('page_title' => $new, 'page_namespace' => NS_ADMIN));
		if ($wgDatabase->numRows($q) == 0) {
			$wgDatabase->update('pages', array('page_title' => $new), array('page_title' => $old, 'page_namespace' => NS_ADMIN));
		}
		else {
			$wgDatabase->delete('pages', array('page_title' => $old, 'page_namespace' => NS_ADMIN));	
		}
	}	
}
function do_hosted_roles_update() {
	global $wgHostedVersion;
	if ($wgHostedVersion) {
		global $wgDatabase;
		$isManager = false;
		
		$q = $wgDatabase->select('roles', array('role_id', 'role_name', 'role_perm_flags'));
		while ($r = $wgDatabase->fetchRow($q)) {
			//None gets LOGIN role
			if ($r['role_name'] == 'None') {
				if ($r['role_perm_flags'] != 1) {
					$wgDatabase->update('roles', array('role_perm_flags' => 1), array('role_id' => $r['role_id']));	
				}
			}
			//Contributor gets CHANGEPERMISSION removed
			if ($r['role_name'] == 'Contributor') {
				if ($r['role_perm_flags'] != 319) {
					$wgDatabase->update('roles', array('role_perm_flags' => 319), array('role_id' => $r['role_id']));	
				}
			}
			//Have to create a Manager role
			if ($r['role_name'] == 'Manager') {
				$isManager = true;	
			}
			//remove redundant role; "none" ends up with the same bitmask
			if ($r['role_name'] == 'Guest') {
				$wgDatabase->delete('roles', array('role_name' => 'Guest'));
			}
		}
		//Add Manager
		if (!$isManager) {
			$wgDatabase->insert('roles', array('role_name' => 'Manager', 'role_perm_flags' => 1343));
		}
	}
}
function do_hosted_version_pages() {
	global $wgHostedVersion, $wgAdminPageBlackList, $wgDatabase;
	if ($wgHostedVersion) {
		if (count($wgAdminPageBlackList) > 0) {
			foreach ($wgAdminPageBlackList as $page) {
				$wgDatabase->delete('pages', array('page_title' => $page, 'page_namespace' => NS_ADMIN));
			}
			echo("removed admin pages for hosted version\n");
		}
	}
}
function do_fix_pages() {
	global $wgDatabase, $IP;
	echo "<li>updating home page title, parent id's</li>\n";

	$sql = "SELECT page_id FROM pages WHERE page_parent=0 AND page_namespace=0 AND (page_title='' OR page_title='Home')";
	$r = $wgDatabase->query($sql);
	$row = $wgDatabase->fetchRow($r);
	if ( !$row )
		return;

	$sql = "UPDATE pages SET page_title='' WHERE page_id=" . $row['page_id'];
	$wgDatabase->query($sql);

	$sql = "UPDATE pages SET page_parent=0 WHERE page_namespace=0 AND page_parent=" . $row['page_id'];
	$wgDatabase->query($sql);
	
	$sql = "SELECT page_id FROM pages WHERE page_namespace=2 AND page_title=''";
	$r = $wgDatabase->query($sql);
	if($row = $wgDatabase->fetchRow($r)) {
		$sql = "UPDATE pages SET page_parent=0 WHERE page_parent=" . $row['page_id'];
		$wgDatabase->query($sql);
		$sql = "DELETE FROM pages WHERE page_id=" . $row['page_id'];
		$wgDatabase->query($sql);
	}

	$sql = "SELECT page_id FROM pages WHERE page_namespace=10 AND page_title=''";
	$r = $wgDatabase->query($sql);
	if($row = $wgDatabase->fetchRow($r)) {
		$sql = "UPDATE pages SET page_parent=0 WHERE page_parent=" . $row['page_id'];
		$wgDatabase->query($sql);
	}
}

function do_utf8_table_conversion() {
	global $IP;
	dbsource("$IP/maintenance/archives/patch-utf8_table_conversion.sql");
	echo "<li>converting table character set to utf8</li>\n";
}
function do_drop_unused_tables() { 
	global $IP;
	dbsource("$IP/maintenance/archives/patch-drop_unused_tables.sql");
	echo "<li>Dropping unused Gooseberry tables</li>\n";
}

function do_change_private_permission() {
	global $wgDatabase;
	$q = $wgDatabase->select('restrictions', array('restriction_perm_flags'), array('restriction_name' => 'Private'));
	if ($r = $wgDatabase->fetchRow($q)) {
		if ($r['restriction_perm_flags'] == 3) {
			echo("<li>removed browse from private restriction</li>\n");
			$wgDatabase->update('restrictions', array('restriction_perm_flags' => '1'), array('restriction_name' => 'Private'));
		}
	}
}

function do_unhide_none_role() {
	global $wgDatabase;
	$wgDatabase->update('roles', array('role_hidden' => '0'), array('role_name' => 'None'));
}
//fixing regression with tag storage
function do_tags_181d_update() {
	global $wgDatabase;
	$tagName = $wgDatabase->fieldInfo('tags', 'tag_name');
	$tagType = $wgDatabase->fieldInfo('tags', 'tag_type');
	if ($tagName->multiple_key != '1' || $tagType->multiple_key == 1) {
		if ($tagType->multiple_key == 1) {
	 		$wgDatabase->query('ALTER TABLE `tags` DROP index `tag_type`');	
		}
		$tags = array();
		$q = $wgDatabase->select('tags', array('tag_id', 'tag_name', 'tag_type'));
		while ($r = $wgDatabase->fetchRow($q)) {
			if (array_key_exists($r['tag_name'].'-'.$r['tag_type'], $tags)) {
				$tagId = $tags[$r['tag_name'].'-'.$r['tag_type']];
				$wgDatabase->update('tag_map', array('tagmap_tag_id' => $tagId), array('tagmap_tag_id' => $r['tag_id']));
				$wgDatabase->delete('tags', array('tag_id' => $r['tag_id']));
				continue;
			}
			$tags[$r['tag_name'].'-'.$r['tag_type']] = $r['tag_id'];
		}
 		$wgDatabase->query('ALTER TABLE `tags` DROP index `tag_name`');	
  		$wgDatabase->query('ALTER TABLE `tags` ADD UNIQUE `tag_name` ( `tag_name` , `tag_type` )');	
  		$wgDatabase->query('ALTER TABLE `tag_map` DROP key `tagmap_page_id`');	
  		$wgDatabase->query('ALTER TABLE `tag_map` ADD unique `tagmap_page_id` (`tagmap_page_id`, `tagmap_tag_id`)');
 		echo('<li>tags table fixed</li>'."\n");
	}
}
function do_admin_page_update() {
	global $wgDatabase, $wgAdminPagesList;
	
	$q = $wgDatabase->select('pages', array('page_id'), array('page_namespace' => NS_ADMIN, 'page_title' => ''));
	if ($wgDatabase->numRows($q) == 1 && $r = $wgDatabase->fetchRow($q)) {
		$AdminHomeId = $r['page_id'];
		$wgDatabase->update('pages', array('page_parent' => $AdminHomeId), array('page_namespace' => NS_ADMIN, 'page_title != \'\''));
	}
	
	//always keep the admin page list current to what's in PHP
	foreach ($wgAdminPagesList as $page) {
		$q = $wgDatabase->select('pages', array('page_id'), array('page_namespace' => NS_ADMIN, 'page_title' => $page));
		if ($wgDatabase->numRows($q) == 0) {
			$wgDatabase->insert('pages', array('page_parent' => $AdminHomeId, 'page_title' => $page, 'page_namespace' => NS_ADMIN));
		}
	}
}
function do_default_user_update() {
	global $wgDatabase;
	$q = $wgDatabase->select('config', array('config_id'), array('config_key' => 'security/new-account-role'));
	if ($wgDatabase->numRows($q) == 0) {
		$wgDatabase->insert('config', array('config_key' => 'security/new-account-role', 'config_value' => 'Contributor'));
		echo("<li>added 'security/new-account-role' config key</li>\n");
	}
}
function do_requestlog_update() {
	global $wgDatabase, $IP;
	if(!$wgDatabase->tableExists("requestlog") && !$wgDatabase->tableExists("requeststats")) {
		echo "<li>adding requestlog, requeststats tables</li>\n";
		dbsource("$IP/maintenance/archives/patch-requestlog.sql");
	}
}
function do_anonymous_user_update() {
	global $IP;
	dbsource("$IP/maintenance/archives/patch-anonymous_user.sql");
	echo("<li>anonymous user updated for all tables</li>\n");
}
function do_add_sitesettings_update() {
	global $wgAdminPagesList;
	if (is_array($wgAdminPagesList) && in_array('SiteSettings', $wgAdminPagesList)) {
		global $wgDatabase;
		$q = $wgDatabase->select(array('pages'), array('page_id'), array('page_namespace' => NS_ADMIN, 'page_title' => 'SiteSettings'));
		$row = $wgDatabase->fetchRow($q);
		if (!$row || $row['page_id'] == 0) {
			$pa = $wgDatabase->select(array('pages'), array('page_id'), array('page_namespace' => NS_ADMIN, 'page_title' => ''));
			$row = $wgDatabase->fetchRow($pa);
			$admin_id = $row['page_id'];
			$wgDatabase->insert('pages',
				array(
					'page_namespace' => NS_ADMIN,
					'page_title' => 'SiteSettings',
					'page_parent' => $admin_id,
					'page_tip' => 'Admin page',
					'page_content_type' => 'text/plain',
					'page_restriction_id' => '0'));
				
				echo("<li>site settings page added</li>\n");
		}
	}
}
function do_attach_nullable_update() {
	global $wgDatabase, $IP;
	$meta = $wgDatabase->fieldInfo('attachments', 'at_image_width');
	if ($meta && $meta->not_null == 1) {
		echo("<li>nullable attachment update finished</li>\n");
		dbsource("$IP/maintenance/archives/patch-attach_null.sql");
	}
}
function do_add_users_ext_column_update() {
        global $wgDatabase, $IP;
	$meta = $wgDatabase->fieldInfo("users", "user_external_name");
	if (!$meta) {
		echo("<li>user external column added</li>\n");
		dbsource("$IP/maintenance/archives/patch-user_ext.sql");
	}
}
function do_convert_web_to_feed_service() {
	global $wgDatabase;
	$wgDatabase->query("UPDATE services SET service_sid='http://services.mindtouch.com/deki/draft/2007/06/feed' WHERE service_sid='http://services.mindtouch.com/deki/draft/2007/06/web' OR service_sid='http://services.mindtouch.com/deki/draft/2007/06/webdata'");
}

function do_archive_ar_old_id_update() {
	global $wgDatabase, $IP;
	$meta = $wgDatabase->fieldInfo("archive", "ar_old_id");
	if(!$meta) {
		echo "<li>adding ar_old_id to archive table</li>\n";
		dbsource("$IP/maintenance/archives/patch-archive-ar_old_id.sql");
		return true;
	}
	return false;
}

function do_users_serviceid_update() {
	global $wgDatabase, $IP;
	$sql = "SELECT `user_id`  FROM `users` WHERE `user_service_id` = 0";
	$r = $wgDatabase->query($sql);
	if($wgDatabase->numRows($r) > 0) {
		echo("<li>updating user_service_id</li>\n");
		dbsource("$IP/maintenance/archives/patch-users-service-id.sql");
	}
}

function do_comments_update() {
	global $wgDatabase, $IP;
	if(!$wgDatabase->tableExists("comments")) {
		echo("<li>comments table added</li>\n");
		dbsource("$IP/maintenance/archives/patch-comments.sql");
	}
}
function do_admin_pages_update() {
	global $wgDatabase, $IP;
	$sql = "SELECT * FROM pages where page_title IN ('Configuration', 'AccountInfo', 'ProductActivation')";
	$res = $wgDatabase->query($sql);
	if($wgDatabase->numRows($res) > 0) {
		echo("<li>deleting Admin:Configuration, Admin:AccountInfo, Admin:ProductActivation pages</li>\n");
		dbsource("$IP/maintenance/archives/patch-admin_pages.sql");
	}
}

function do_service_sid_nullable_update() {
	global $wgDatabase, $IP;
	$meta = $wgDatabase->fieldInfo("services", "service_sid");
	if($meta->not_null) {
		echo("<li>updated services table set service_sid nullable</li>\n");
		dbsource("$IP/maintenance/archives/patch-services_service_sid.sql");
	}
}

function do_archive_id() {
	global $wgDatabase, $IP;
	$meta = $wgDatabase->fieldInfo("archive", "ar_id");
	if (!$meta) {
		echo("<li>updated archives table with primary key</li>\n");
		dbsource("$IP/maintenance/archives/patch-archive-ar_id.sql");
	}
}

function do_extension_services_update() {
        global $wgDatabase, $IP;
	$sql = "SELECT * FROM services WHERE service_type='ext'";
	$res = $wgDatabase->query($sql);
	if($wgDatabase->numRows($res) == 0) {
		echo "<li>adding extension services</li>\n";
		dbsource("$IP/maintenance/archives/patch-extension_services.sql");
		return true;
	}
	return false;
}

function do_settings_update() {
	global $wgDatabase, $IP;
	if(!$wgDatabase->tableExists("config")) {
		echo "<li>adding config table</li>\n";
		dbsource("$IP/maintenance/archives/patch-settings.sql");
		// insert the attachment storage path config setting
		$sql = "INSERT INTO `config` (`config_key`, `config_value`) VALUES('storage/fs/path','$IP/attachments');";
		$wgDatabase->query($sql);
	}		
}
function do_user_registration_update() {
	global $wgDatabase, $IP;
	if(!$wgDatabase->tableExists("user_registrations")) {
		echo "<li>adding user registration table</li>\n";
		dbsource("$IP/maintenance/archives/patch-user-registration.sql");
	}	
	return true;
}
function do_df2hayes_misc() {
	global $IP;
	echo "<li>applying miscellaneous df2hayes updates</li>\n";
	dbsource("$IP/maintenance/archives/patch-df2hayes-misc.sql");
	return true;
}

function do_special_pages_update() {
	global $IP;
	echo "<li>cleanup up special pages</li>\n";
	dbsource("$IP/maintenance/archives/patch-specialpages2.sql");
	return true;
}

function do_archive_ar_last_page_id_update() {
        global $wgDatabase, $IP;
        $meta = $wgDatabase->fieldInfo("archive", "ar_last_page_id");
        if(!$meta) {
                echo "<li>adding ar_last_page_id to archive table</li>\n";
                dbsource("$IP/maintenance/archives/patch-archive-ar_last_page_id.sql");
                return true;
        }
        return false;
}

function do_old_content_type_update() {
	global $wgDatabase, $IP;
	$meta = $wgDatabase->fieldInfo("old", "old_content_type");
	if(!$meta) {
		echo "<li>adding old_content_type to old table</li>\n";
		dbsource("$IP/maintenance/archives/patch-old_content_type.sql");
		return true;
	}
	return false;	
}

function do_users_unique_keys_update() {
	// r3349
	global $wgDatabase, $IP;
	if(!$wgDatabase->indexExists("groups", "group_name")) {
		echo "<li>adding group_name index to groups table</li>\n";
		dbsource("$IP/maintenance/archives/patch-users_unique_keys.sql");
		return true;
	}
	return false;
}

function do_users_user_builtin_update() {
	global $wgDatabase, $IP;
	$meta = $wgDatabase->fieldInfo("users", "user_builtin");
	if(!$meta) {
		echo "<li>adding user_builtin to users table</li>\n";
		dbsource("$IP/maintenance/archives/patch-users_user_builtin.sql");
		return true;
	}
	return false;
}
	
function do_tags_tag_type_update() {
	global $wgDatabase, $IP;
	$meta = $wgDatabase->fieldInfo("tags", "tag_type");
	if(!$meta) {
		echo "<li>adding tag_types to tags table</li>\n";
		dbsource("$IP/maintenance/archives/patch-tags-tag_type.sql");
		return true;
	}
	return false;
}

function do_hidden_roles_update() {
	global $wgDatabase, $IP;
	$meta = $wgDatabase->fieldInfo("roles", "role_hidden");
	if(!$meta) {
		echo "<li>adding role_hidden to roles table</li>\n";
		dbsource("$IP/maintenance/archives/patch-role_hidden.sql");
		return true;
	}
	return false;
}

function do_specialpages_update() {
	global $wgDatabase, $IP;
	$sql = "SELECT * FROM pages WHERE page_namespace IN (101,103)";
	$res = $wgDatabase->query($sql);
	if($wgDatabase->numRows($res) == 0) {
		echo "<li>adding special pages</li>\n";
		dbsource("$IP/maintenance/archives/patch-specialpages.sql");
		return true;
	}
	return false;
}

function do_tags_update() {
	global $wgDatabase, $IP;
	if(!$wgDatabase->tableExists("tags") || !$wgDatabase->tableExists("tag_map"))
	{
		echo "<li>adding tags tables</li>\n";
		dbsource("$IP/maintenance/archives/patch-tags.sql");
		return true;
	}
}

function do_df2hayes_update() {
	global $wgDatabase, $IP;
	dbsource("$IP/maintenance/archives/patch-df2hayes.sql");
	echo("<li>migrationg dekifarm db to hayes schema</li>\n");
	return true;
}
 
function do_attachments_update() {
	global $wgDatabase, $IP;
	$meta = $wgDatabase->fieldInfo("attachments", "at_image_width");
	if(!$meta)
        {
		dbsource("$IP/maintenance/archives/patch-attachments.sql");
		echo("<li>adding at_image_width, at_image_height to `attachments`</li>\n");
		return true;
	}
	return false;
}

function add_pages_contenttype() {
	global $wgDatabase, $IP;
	$meta = $wgDatabase->fieldInfo( "pages", "page_content_type" );	
	if (!$meta) {
		dbsource( "$IP/maintenance/archives/patch-pages-content-type.sql" );
		echo("<li>adding content-type column to `pages`</li>\n");
		return true;
	}
	return false;
}

function do_combo_permission_update()
{
	global $wgDatabase, $IP, $wgDBserver, $wgDBuser, $wgDBpassword, $wgDBname, $wgDBadminuser, $wgDBadminpassword;
	
	if ($wgDatabase->tableExists('user') && 
		$wgDatabase->tableExists('user_rights') &&
		$wgDatabase->tableExists('cur')) 
	{
		echo "<li>combo permissions system DB upgrade...";
		dbsource( "$IP/maintenance/archives/patch-permission-system-combo.sql" );
		echo "done</li>\n";
		return true;
	}
	return false;
}

function do_config_languages_update() {
	global $wgDatabase;
	$q = $wgDatabase->select('config', array('config_id'), array('config_key' => 'languages'));
	if ($wgDatabase->numRows($q) == 0) {
		$wgDatabase->insert('config', array('config_key' => 'languages', 'config_value' => ''));
		echo("added 'languages' config key\n");
	}
}

# PeteE: function that will always be run to perform any miscellaneous DB changes/updates
function do_misc_updates() {
	global $wgDatabase, $IP;
	echo "<li>Applying misc database updates...";
	dbsource( "$IP/maintenance/archives/onupdate.sql" );
	echo "done</li>\n";
	return true;
}
?>
