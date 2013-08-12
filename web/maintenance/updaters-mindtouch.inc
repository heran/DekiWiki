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
	do_specialpages_update();
	do_users_user_builtin_update();
	do_tags_tag_type_update();
	do_users_unique_keys_update();
	do_old_content_type_update();
	do_archive_ar_last_page_id_update();
    do_special_pages_update();
    do_settings_update();
	do_extension_services_update();
	do_archive_id();
	do_service_sid_nullable_update();
	do_comments_update();
	do_move_apikey_update();
	do_users_serviceid_update();
	do_add_apikey_to_db();
	do_archive_ar_old_id_update();
	do_convert_web_to_feed_service();
	do_add_users_ext_column_update();
	do_attach_nullable_update();
	do_anonymous_user_update();
	do_requestlog_update();
	do_default_user_update();
	do_tags_181d_update();
	do_change_private_permission();
	// here ends 1.8.2 database updates
	
	// 1.8.3 updates 
	do_remove_hidden_roles();
	do_add_attachment_indices();
	do_hosted_roles_update();
	
	// 1.9.0 updates
	do_add_page_parent_index();
	add_pages_language_and_displayname();	
	do_banning_update();
	do_page_template_id_update();
	
	// 8.05 updates
	do_config_languages_update();
	do_transaction_update();
	
	// 8.07 updates
	do_set_permission_impersonator();
	
	// 9.02 updates
	do_remove_admin_pages();
	do_add_properties();
	add_comment_posterid_index();
	do_file_schema_move();
	do_add_user_creation_timestamp();
	do_file_fix_upload(); // r. 12483
	do_remove_site_stats();
	do_remove_user_registrations_table();
	do_make_parent_nullable();
	do_add_user_meta_columns();
	do_user_metadata_transition();
	do_customized_styles_to_properties();
	do_namespace_column_update();
	do_files_config_keys_update();
	
	// 9.02.2 updates
	do_resource_update_for_strict();
	do_builtin_extension_to_dekiscript_extension_migration();
	do_jpeg_mimetype_change();
	
	// 9.08 updates
	do_revision_hiding_change(); 
	do_revision_ishidden_rename(); 
	
	// 9.12 release
	do_set_default_skin();
	do_remove_user_text_colums();
	add_recentchange_rcuser_index();
	
	// 9.12.2 release
	fix_recentchanges_rc_cur_id();

	// olympic release
	add_group_index();
	add_default_email_sender(); 
	add_querylogs(); 
	add_page_revisions();
	add_linked_titles(); 
	add_ratings_table(); 
	drop_stored_procedures();
	optimize_old_tables(); 
	reload_search_tables(); 
	add_tagmap_tag_id_index();
	do_remove_flag_colums();
	add_user_dashboard_config_key();
	update_deuce_skin();
	change_public_restriction();
	clear_out_real_names(); //MUST BE REMOVED AT A LATER RELEASE
	fix_tags_table_indexes();
	add_ckb_services();
	drop_unused_pages_columns();
	
	// 10.0.3 release
	add_pagesub();
	
	add_user_seat();
	
	// 10.1 release
	migrate_page_views();
  drop_unused_10_1_tables();
}

function drop_unused_10_1_tables() {
  global $wgDatabase;
  if($wgDatabase->tableExists('linkscc')) {
    echo("dropping table `linkscc`\n");
    $wgDatabase->query("DROP TABLE `linkscc`");
  }
  if($wgDatabase->tableExists('objectcache')) {
    echo("dropping table `objectcache`\n");
    $wgDatabase->query("DROP TABLE `objectcache`");
  }
  if($wgDatabase->tableExists('querycache')) {
    echo("dropping table `querycache`\n");
    $wgDatabase->query("DROP TABLE `querycache`");
  }
}

function migrate_page_views() {
	global $wgDatabase;
	if (!$wgDatabase->tableExists('page_viewcount')) {
	    $wgDatabase->query(
	        "CREATE TABLE `page_viewcount` (".
	        "  `page_id` int(8) unsigned NOT NULL,".
	        "  `page_counter` bigint(20) unsigned not null default 0,".
	        "  PRIMARY KEY  (`page_id`)) ENGINE=MyISAM DEFAULT CHARSET=utf8 COLLATE utf8_general_ci");
	    $wgDatabase->query("INSERT INTO page_viewcount SELECT page_id, page_counter FROM pages");
        echo("Page views moved to page_viewcount table\n");
		return;
	}
}

function add_user_seat() {
	global $wgDatabase;
	
	$userseat = $wgDatabase->fieldInfo('users', 'user_seat');
	if ($userseat === false) {
		echo("adding user_seat column to users table\n");
		$wgDatabase->query("alter table `users` add column `user_seat` tinyint UNSIGNED DEFAULT '0' NOT NULL");
	}
}

function drop_unused_pages_columns() {
	global $wgDatabase;
	
	if($wgDatabase->fieldInfo('pages', 'page_random')) {
		$wgDatabase->query("ALTER TABLE `pages` DROP INDEX `page_random`");
		$wgDatabase->query("ALTER TABLE `pages` DROP INDEX `name_title_timestamp`");
		$wgDatabase->query("ALTER TABLE `pages` DROP INDEX `user_timestamp`");
		$wgDatabase->query("ALTER TABLE `pages` DROP INDEX `usertext_timestamp`");
		$wgDatabase->query("ALTER TABLE `pages` DROP `page_random`");
		$wgDatabase->query("ALTER TABLE `pages` DROP `page_inverse_timestamp`");
		$wgDatabase->query("ALTER TABLE `pages` ADD INDEX `page_user_id` (`page_user_id`)");
 		echo("unused page columns dropped from the database\n");
	}
}

function fix_tags_table_indexes() {
	global $wgDatabase;
	$unique = $wgDatabase->indexUnique('tags', 'tag_name');
	if(!$unique) {
		$tags = array();
		$q = $wgDatabase->select('tags', array('tag_id', 'tag_name', 'tag_type'));
		while ($r = $wgDatabase->fetchRow($q)) {
			$tagName = strtolower($r['tag_name']);
			if(array_key_exists($tagName.'-'.$r['tag_type'], $tags)) {
				$tagId = $tags[$tagName.'-'.$r['tag_type']];
				$wgDatabase->update('tag_map', array('tagmap_tag_id' => $tagId), array('tagmap_tag_id' => $r['tag_id']));
				$wgDatabase->delete('tags', array('tag_id' => $r['tag_id']));
				continue;
			}
			$tags[$tagName.'-'.$r['tag_type']] = $r['tag_id'];
		}
		echo("fixing tags table index\n");
		$wgDatabase->query("ALTER table `tags` DROP index `tag_name`");
		$wgDatabase->query("ALTER table `tags` ADD UNIQUE `tag_name` (`tag_name`, `tag_type`)");

		// clean up any duplicate rows in tag_map
		$tagMap = array();
		$q = $wgDatabase->select('tag_map', array('tagmap_id', 'tagmap_page_id', 'tagmap_tag_id'));
		while ($r = $wgDatabase->fetchRow($q)) {
			if( array_key_exists($r['tagmap_page_id'].'-'.$r['tagmap_tag_id'], $tagMap)) {
				$wgDatabase->delete('tag_map', array('tagmap_id' => $r['tagmap_id']));
				continue;
			}
			$tagMap[$r['tagmap_page_id'].'-'.$r['tagmap_tag_id']] = $r['tagmap_id'];
		}
		$wgDatabase->query('ALTER TABLE `tag_map` DROP key `tagmap_page_id`');
		$wgDatabase->query('ALTER TABLE `tag_map` ADD unique `tagmap_page_id` (`tagmap_page_id`, `tagmap_tag_id`)');
	}
}

function add_ckb_services() {
	
	global $wgDatabase;
	$q = $wgDatabase->select('services', array('COUNT(*) as count'), array('service_sid' => 'sid://mindtouch.com/std/2009/04/activitystream'));
	if ($r = $wgDatabase->fetchRow($q)) {
		if ($r['count'] == 0) {
			$wgDatabase->insert('services', 
				array(
					'service_type' => 'ext', 
					'service_sid' => 'sid://mindtouch.com/std/2009/04/activitystream',
					'service_description' => 'Activity Stream', 
					'service_enabled' => '0'
				)
			);
			echo("Added Activity Stream service\n");
		}
	}
	
	$q = $wgDatabase->select('services', array('COUNT(*) as count'), array('service_sid' => 'sid://mindtouch.com/ext/2009/12/anychart'));
	if ($r = $wgDatabase->fetchRow($q)) {
		if ($r['count'] == 0) {
			$wgDatabase->insert('services', 
				array(
					'service_type' => 'ext', 
					'service_sid' => 'sid://mindtouch.com/ext/2009/12/anychart',
					'service_description' => 'MindTouch Charts', 
					'service_enabled' => '0'
				)
			);
			echo("Added MindTouch Charts service\n");
		}
	}
	
	$q = $wgDatabase->select('services', array('COUNT(*) as count'), array('service_sid' => 'sid://mindtouch.com/ext/2010/06/analytics.search'));
	if ($r = $wgDatabase->fetchRow($q)) {
		if ($r['count'] == 0) {
			$wgDatabase->insert('services', 
				array(
					'service_type' => 'ext', 
					'service_sid' => 'sid://mindtouch.com/ext/2010/06/analytics.search',
					'service_description' => 'Curation Analytics (Search)', 
					'service_enabled' => '0'
				)
			);
			echo("Added Curation Analytics (Search) service\n");
		}
	}
	$q = $wgDatabase->select('services', array('COUNT(*) as count'), array('service_sid' => 'sid://mindtouch.com/ext/2010/06/analytics.content'));
	if ($r = $wgDatabase->fetchRow($q)) {
		if ($r['count'] == 0) {
			$wgDatabase->insert('services', 
				array(
					'service_type' => 'ext', 
					'service_sid' => 'sid://mindtouch.com/ext/2010/06/analytics.content',
					'service_description' => 'Curation Analytics (Content)', 
					'service_enabled' => '0'
				)
			);
			echo("Added Curation Analytics (Content) service\n");
		}
	}
}

function clear_out_real_names() {
	// see bug #8281
	global $wgDatabase;
	$wgDatabase->update('users', array('user_real_name' => ''), array('user_real_name != ""')); 	
}

function change_public_restriction() {
	global $wgDatabase;

	// HACK: PeteE - Need to use SHOW FIELDS since fieldInfo() doesn't differentiate int types (mediumint versus bigint)
	$q = $wgDatabase->query("SHOW FIELDS FROM restrictions");
	while ($r = $wgDatabase->fetchRow($q)) {
		if ($r['Field'] == 'restriction_perm_flags') {
			if (substr($r['Type'], 0, 9) == 'mediumint') {
				$wgDatabase->query("ALTER TABLE `restrictions` MODIFY restriction_perm_flags bigint unsigned NOT NULL");
				$wgDatabase->update('restrictions', array('restriction_perm_flags' => '18446744073709551615'), array('restriction_name' => 'Public'));
				echo("changing restriction_perm_flags for Public role\n");
				break;
			}
		}
	}
}

function update_deuce_skin() {
	global $wgDatabase; 
	$q = $wgDatabase->select('config', array('config_value'), array('config_key' => 'ui/template')); 
	if ($r = $wgDatabase->fetchRow($q)) {
		if ($r['config_value'] == 'deucebeta') {
			$wgDatabase->update('config', array('config_value' => 'deuce'), array('config_key' => 'ui/template'));
			echo("Updated to default skin: Deuce\n");
		}
	}	
}
function add_user_dashboard_config_key() {
	global $wgDatabase;
	$q = $wgDatabase->select('config', array('config_value'), array('config_key' => 'ui/user-dashboards'));
	if (!$wgDatabase->fetchRow($q)) {
		echo("creating ui/user-dashboards config key\n");
		$wgDatabase->insert('config', array('config_key' => 'ui/user-dashboards', 'config_value' => 'Template:MindTouch/Views/ActivityDashboard, user_page'));
	}
}

function do_remove_flag_colums() 
{
	global $wgDatabase;
	
	if ($wgDatabase->fieldInfo('old', 'old_flags'))
	{
		$wgDatabase->query("ALTER TABLE `old` DROP `old_flags`");
		$wgDatabase->query("ALTER TABLE `archive` DROP `ar_flags`");
		echo("dropping old.old_flags and archive.ar_flags column\n");
	}
}

function add_tagmap_tag_id_index()
{
	global $wgDatabase;
	$index = $wgDatabase->indexInfo('tag_map', 'tagmap_tag_id');
	if(!$index)
	{
		$wgDatabase->query("ALTER table `tag_map` add index `tagmap_tag_id` (`tagmap_tag_id`)");
		echo("created tag_map.tagmap_tag_id index\n");
	}
}

function reload_search_tables() 
{
	global $wgDatabase;
	$col = $wgDatabase->fieldInfo('query_log', 'last_result_id'); 
	if ($col === false) 
	{		
		$wgDatabase->query("DROP TABLE `query_log`"); 
		$wgDatabase->query("DROP TABLE `query_terms`"); 
		$wgDatabase->query("DROP TABLE `query_term_map`"); 
		$wgDatabase->query("DROP TABLE `query_result_log`"); 
		
        dbsource("maintenance/archives/patch-search-analytics.sql");
		echo('Search analytics tables recreated'."\n"); 
	}
}

function optimize_old_tables() 
{
	global $wgDatabase, $IP, $wgContLang, $wgLanguageCode, $wgNamespaceNamesEn; 
	
	$DisplayColumn = $wgDatabase->fieldInfo('old', 'old_display_name');
	if ($DisplayColumn->not_null != 1) 
	{
		// wow, some massive bootstrapping necessary so we can include the title magic	
		require_once($IP.'/includes/Title.php');
		require_once($IP.'/includes/Article.php');
		require_once($IP.'/includes/GlobalFunctions.php');
		require_once($IP.'/languages/Language.php');
		require_once($IP.'/deki/core/deki_namespace.php');
		
		$wgContLang = new Language();
		// for nulled display names, set the value
		$q = $wgDatabase->select('old', array('old_namespace', 'old_title', 'old_id', 'old_display_name'), array('old_display_name is null')); 
		while ($r = $wgDatabase->fetchRow($q)) 
		{
			
			$nt = Title::newFromText($r['old_title'], $r['old_namespace']); 
			$segments = Article::getParentsFromName($nt->getPrefixedText()); 
			
			// get the last portion of the path for the display title
			$display_name = end($segments);
			
			// converts path segments to the closest thing we have to a display title on the front-end
			$display_name = wfDecodeTitle($display_name); 
			
			// populate
			$wgDatabase->update('old', array('old_display_name' => $display_name), array('old_id' => $r['old_id'])); 
		}
		
		$wgDatabase->query("alter table `old` change `old_display_name` `old_display_name` varchar(255) not null");
		echo("old table successfully repopulated\n");
		
		// drop old_namespace and old_title
		$wgDatabase->query("ALTER TABLE `old` DROP `old_namespace`"); 
		$wgDatabase->query("ALTER TABLE `old` DROP `old_title`"); 
		$wgDatabase->query("ALTER TABLE `old` DROP `inverse_timestamp`"); 
		$wgDatabase->query("ALTER TABLE `old` DROP index `user_timestamp`"); 
		$wgDatabase->query("ALTER TABLE `old` DROP index `old_timestamp`"); 
		$wgDatabase->query("ALTER TABLE `old` ADD index `old_user` ( `old_user` )"); 
		echo("old_namespace and old_title columns dropped from the database\n");
		
	}
}

// bug 7392
function drop_stored_procedures() {
	global $wgDatabase;
	$sprocs = array(
		'config_get_Config',
		'perm_del_PageUserGroupGrants',
		'perm_upd_CopySecurityToPages',
		'rc_ins_LoggingEntry',
		'tag_del_TagsForPageIdWithTagIds',
		'tag_ins_NewTag',
		'util_String_SplitToTable',
		'log_ins_hit',
		'log_ins_statsbyminute',
		'log_ins_statsbyhour');

	echo("dropping stored procedures\n");
	foreach($sprocs as $sproc) {
		$wgDatabase->query('DROP PROCEDURE IF EXISTS `' . $sproc . '`');
	}
	$wgDatabase->query('DROP FUNCTION IF EXISTS `util_Page_IsAntecedent`');
	$wgDatabase->query('DROP FUNCTION IF EXISTS `util_String_ReplaceStartOfString`');
}

function add_ratings_table() {
	global $wgDatabase; 
	
	if (!$wgDatabase->tableExists('ratings')) 
	{
        dbsource("maintenance/archives/patch-ratings.sql");
        echo("Ratings tables added\n");
	}
}

function add_linked_titles() {
	global $wgDatabase, $IP, $wgContLang, $wgLanguageCode, $wgNamespaceNamesEn; 
	
	$DisplayColumn = $wgDatabase->fieldInfo('pages', 'page_display_name');
	if ($DisplayColumn->not_null != 1) 
	{
		
		// drop the old linked title
		$linked_title = $wgDatabase->fieldInfo('pages', 'page_title_linked'); 
		if ($linked_title) 
		{
			$wgDatabase->query('ALTER TABLE `pages` DROP `page_title_linked`'); 	
		}
		
		// wow, some massive bootstrapping necessary so we can include the title magic	
		require_once($IP.'/includes/Title.php');
		require_once($IP.'/includes/Article.php');
		require_once($IP.'/includes/GlobalFunctions.php');
		require_once($IP.'/languages/Language.php');
		require_once($IP.'/deki/core/deki_namespace.php');
		
		$wgContLang = new Language();
		
		// for nulled display names, set the value
		$q = $wgDatabase->select('pages', array('page_namespace', 'page_title', 'page_id', 'page_display_name'), array('page_display_name is null')); 
		while ($r = $wgDatabase->fetchRow($q)) 
		{
			
			$nt = Title::newFromText($r['page_title'], $r['page_namespace']); 
			
			// the null reference exception is worrisome, because it means that the path has invalid characters
			// if this occurs, then the display title will not be set
			if (is_null($nt)) 
			{
				continue;
			}
			$segments = Article::getParentsFromName($nt->getPrefixedText()); 
			
			// get the last portion of the path for the display title
			$display_name = end($segments);
						
			// converts path segments to the closest thing we have to a display title on the front-end
			$display_name = wfDecodeTitle($display_name); 
			
			// populate
			$wgDatabase->update('pages', array('page_display_name' => $display_name), array('page_id' => $r['page_id'])); 
		}
		
		// for nulled display names, set the value
		$q = $wgDatabase->select('archive', array('ar_namespace', 'ar_title', 'ar_id', 'ar_display_name'), array('ar_display_name is null')); 
		while ($r = $wgDatabase->fetchRow($q)) 
		{
			
			$nt = Title::newFromText($r['ar_title'], $r['ar_namespace']); 
			if(is_null($nt))
			{
				continue;
			}

			$segments = Article::getParentsFromName($nt->getPrefixedText()); 
			
			// get the last portion of the path for the display title
			$display_name = end($segments);
			
			// converts path segments to the closest thing we have to a display title on the front-end
			$display_name = wfDecodeTitle($display_name); 
			
			// populate
			$wgDatabase->update('archive', array('ar_display_name' => $display_name), array('ar_id' => $r['ar_id'])); 
		}
		
		// make display_name non-nullable
		$wgDatabase->query("alter table `pages` change `page_display_name` `page_display_name` varchar(255) not null");
		$wgDatabase->query("alter table `archive` change `ar_display_name` `ar_display_name` varchar(255) not null");
		
		// special case: set a default for the root home page
		$q = $wgDatabase->select('config', array('config_value'), array('config_key' => 'ui/sitename')); 
		if ($r = $wgDatabase->fetchRow($q))
		{
			$sitename = $r['config_value']; 
		}
				
		// make root-level homepage a "fixed" page type
		$wgDatabase->update('pages', array('page_display_name' => $sitename), array('page_namespace' => NS_MAIN, "page_title = ''")); 
		
		echo("Page titles populated\n");
	}
}

// bug #8001
function add_page_revisions() {
	global $wgDatabase;
	
	// this is for the devs
	$temp_index = $wgDatabase->indexInfo('old', 'page_id_temp');
	if ($temp_index) 
	{
		echo("cleaning up temp dev data\n");
		$wgDatabase->query("alter table old drop index page_id_temp"); 
		$wgDatabase->query("alter table old drop `old_revision`, drop `old_page_id`"); 
		$wgDatabase->query("alter table pages drop `page_revision`, drop `page_etag`"); 
		$wgDatabase->query("alter table archive drop `ar_revision`"); 
	}
	
	$old_revision = $wgDatabase->fieldInfo('old', 'old_revision');
	if ($old_revision === false)
	{
		echo("Starting page revision alters... \n");flush();
		$wgDatabase->query("alter table old add column old_revision int(8) unsigned not null default 0, add column old_page_id int(8) unsigned not null default 0"); 
		$wgDatabase->query("alter table pages add column page_revision int(8) unsigned not null default 1, add column page_etag varchar(32) not null default ''"); 
		$wgDatabase->query("alter table archive add column ar_revision int(8) unsigned not null default 0;"); 

		// add temporary index on old_page_id, then drop it and add a unique index later
		$wgDatabase->query("alter table old add index old_page_id_temp  (old_page_id)");
		
		// this needs to be re-run once old_page_id gets support from the API
		$wgDatabase->query("update old set old_page_id = (select page_id from pages where page_title = old_title and page_namespace = old_namespace)"); 
		$wgDatabase->query("update pages set page_revision = (SELECT count(*) + 1 FROM old WHERE old_page_id = page_id ), page_etag = md5(pages.page_text)"); 

		// drop temporary index
		$wgDatabase->query("alter table old drop index old_page_id_temp");

		$last_page_id = null;
		$last_page_title = null; 
		$q = $wgDatabase->query("SELECT old_id, old_page_id, old_title, old_namespace FROM old ORDER BY old_page_id ASC, old_id ASC"); 
		while ($r = $wgDatabase->fetchRow($q)) 
		{
			// if we're on a new page
			if ($last_page_id != $r['old_page_id'] || ($r['old_page_id'] == 0 && $last_title != $r['old_title'])) {
				$i = 1; 
			}
			$wgDatabase->update('old', array('old_revision' => $i), array('old_id' => $r['old_id'])); 
			
			$i++; 
			$last_page_id = $r['old_page_id'];
			$last_title = $r['old_title'];
		}
		$wgDatabase->query("update archive set ar_revision = (select count(*)+1 from ( select ar_last_page_id last_page_id, ar_timestamp ts from archive) a where a.last_page_id = ar_last_page_id and ts < ar_timestamp)"); 
		
		// this is data loss, but it's not like we can recover it anyways
		$wgDatabase->delete('old', array('old_page_id' => '0')); 
		$wgDatabase->query("alter table old add unique index old_page (old_page_id, old_revision)"); 
		
		echo("Page optimization revisions with unique constraint added\n");
	}
}

// bug #7975
function add_querylogs() {
	global $wgDatabase; 
	if (!$wgDatabase->tableExists('query_log')) {
        dbsource("maintenance/archives/patch-searchqueries.sql");
        echo("Search query logs added\n");
		return;
	}
}

function add_pagesub() {
	global $wgDatabase; 
	if (!$wgDatabase->tableExists('pagesub')) {
        dbsource("maintenance/archives/patch-pagesub.sql");
        echo("Page Subscription table added\n");
		return;
	}
}

// bug #7876
function add_default_email_sender() {
	global $wgDatabase; 
	$q = $wgDatabase->select('config', array('COUNT(*) as count'), array('config_key' => 'admin/email')); 
	if ($r = $wgDatabase->fetchRow($q)) {
		if ($r['count'] > 0) {
			return; 
		}
		
		$q = $wgDatabase->select('users', array('user_email'), array('user_name' => 'Admin')); 
		if ($r = $wgDatabase->fetchRow($q)) {
			$wgDatabase->insert('config', array('config_key' => 'admin/email', 'config_value' => $r['user_email'])); 
			echo("Set the system email sender 'from' field to the default administrator's email\n");
		}
	}
}

// bug #7244
function add_group_index() {
	global $wgDatabase;
	
	if (!$wgDatabase->indexExists("user_groups", "group_id")) {
		$wgDatabase->query("ALTER table `user_groups` ADD INDEX `group_id` (`group_id`)"); 
		echo("Group index added\n");
	}
}

function fix_recentchanges_rc_cur_id() {
	global $wgDatabase;

	$q = $wgDatabase->query(
		"SELECT COUNT(*) as count from recentchanges where ".
		"`rc_cur_id`=0"
	);
	$r = $wgDatabase->fetchRow($q);
	
	if( $r['count'] > 0  ) {
		echo("fixing recentchanges.rc_cur_id data\n");
		$q = $wgDatabase->select('recentchanges', array('rc_id','rc_title','rc_namespace'), array('rc_cur_id' => '0'));

		while($r = $wgDatabase->fetchRow($q)) {
			$rc_id = $r['rc_id'];
			$rc_title = $r['rc_title'];
			$rc_namespace = $r['rc_namespace'];
			
			$q2 = $wgDatabase->select('pages', array('page_id'), array('page_namespace' => $rc_namespace, 'page_title' => $rc_title));
			$r2 = $wgDatabase->fetchRow($q2);

			if($r2)
				$wgDatabase->update('recentchanges', array('rc_cur_id' => $r2['page_id']), array('rc_id' => $rc_id));
		}

	}
}

function add_recentchange_rcuser_index() {
        global $wgDatabase;
	if(!$wgDatabase->indexExists("recentchanges", "rc_user")) {
                $wgDatabase->query("ALTER table `recentchanges` ADD INDEX `rc_user` (`rc_user`)");
                echo("added rc_user index to recentchanges table\n");
        }     
}

function do_remove_user_text_colums() {
	global $wgDatabase;
	$old_user_text = $wgDatabase->fieldInfo('old', 'old_user_text');	
	if($old_user_text) {
		echo("deleting old.old_user_text\n");
		$wgDatabase->query("ALTER table `old` DROP `old_user_text`");
	}
	$ar_user_text = $wgDatabase->fieldInfo('archive', 'ar_user_text');	
	if($ar_user_text) {
		echo("deleting archive.ar_user_text\n");
		$wgDatabase->query("ALTER table `archive` DROP `ar_user_text`");
	}
	$rc_user_text = $wgDatabase->fieldInfo('recentchanges', 'rc_user_text');	
	if($rc_user_text) {
		echo("deleting recentchanges.rc_user_text\n");
		$wgDatabase->query("ALTER table `recentchanges` DROP `rc_user_text`");
	}
}

function do_set_default_skin() {
	global $wgDatabase;
	
	$q = $wgDatabase->query("SELECT COUNT(*) as count from config where config_key = 'ui/skin' OR config_key = 'ui/template'"); 
	if ($r = $wgDatabase->fetchRow($q)) {
		if ($r['count'] < 2) {
			global $wgActiveTemplate, $wgActiveSkin; 
			$wgDatabase->insert('config', array('config_key' => 'ui/skin', 'config_value' => $wgActiveSkin)); 
			$wgDatabase->insert('config', array('config_key' => 'ui/template', 'config_value' => $wgActiveTemplate)); 
			echo("Inserted skin values into config table\n"); 
		}	
	}
}

function do_revision_ishidden_rename() {
	global $wgDatabase;
	$hidden = $wgDatabase->fieldInfo('resources', 'resrev_ishidden');
	if($hidden) {
		echo("renaming resrev_ishidden to resrev_is_hidden\n");
		$wgDatabase->query("alter table `resources` change `resrev_ishidden` `resrev_is_hidden` tinyint UNSIGNED DEFAULT '0' NOT NULL");
		$wgDatabase->query("alter table `resourcerevs` change `resrev_ishidden` `resrev_is_hidden` tinyint UNSIGNED DEFAULT '0' NOT NULL");
	}
}

function do_revision_hiding_change() {
	global $wgDatabase;
	
	$old_hidden = $wgDatabase->fieldInfo('old', 'old_is_hidden');
	if ($old_hidden === false) {
		echo("adding fields to add revision hiding\n");
		$wgDatabase->query("alter table `old` add column `old_is_hidden` tinyint UNSIGNED DEFAULT '0' NOT NULL");
		$wgDatabase->query("alter table `old` add column `old_meta` text NULL");
		$wgDatabase->query("alter table `archive` add column `ar_is_hidden` tinyint UNSIGNED DEFAULT '0' NOT NULL");
		$wgDatabase->query("alter table `archive` add column `ar_meta` text NULL");
		$wgDatabase->query("alter table `pages` add column `page_is_hidden` tinyint UNSIGNED  DEFAULT '0' NOT NULL");
		$wgDatabase->query("alter table `pages` add column `page_meta` text   NULL");
		$wgDatabase->query("alter table `resources` add column `resrev_is_hidden` tinyint UNSIGNED DEFAULT '0' NOT NULL after `resrev_language`");
		$wgDatabase->query("alter table `resourcerevs` add column `resrev_is_hidden` tinyint UNSIGNED DEFAULT '0' NOT NULL after `resrev_language`");
	}
}

function do_jpeg_mimetype_change() {
	global $wgDatabase;
	$q = $wgDatabase->select(
		'resources',
		'COUNT(*) as count',
		array('resrev_mimetype' => 'image/jpg')
	);
	$r = $wgDatabase->fetchRow($q);
	if ($r['count'] > 0) {
		echo("fixing resources 'image/jpeg' mimetypes\n");
		$wgDatabase->update(
			'resources',
			array('resrev_mimetype' => 'image/jpeg'),
			array('resrev_mimetype' => 'image/jpg')
		);
	}

	$q = $wgDatabase->select(
		'resourcerevs',
		'COUNT(*) as count',
		array('resrev_mimetype' => 'image/jpg')
	);
	$r = $wgDatabase->fetchRow($q);
	if ($r['count'] > 0) {
		$wgDatabase->update(
			'resourcerevs',
			array('resrev_mimetype' => 'image/jpeg'),
			array('resrev_mimetype'=> 'image/jpg')
		);
	}

	$q = $wgDatabase->select(
		'resourcecontents',
		'COUNT(*) as count',
		array('rescontent_mimetype' => 'image/jpg')
	);
	$r = $wgDatabase->fetchRow($q);
	if ($r['count'] > 0) {
		$wgDatabase->update(
			'resourcecontents',
			array('rescontent_mimetype' => 'image/jpeg'),
			array('rescontent_mimetype' => 'image/jpg')
		);
	}
}

function do_builtin_extension_to_dekiscript_extension_migration() {
	global $wgDatabase;
	$dekiscriptSid = 'sid://mindtouch.com/2007/12/dekiscript';
	$services = array(
		'http://services.mindtouch.com/deki/draft/2007/06/digg' => 'http://scripts.mindtouch.com/digg.xml',
		'sid://mindtouch.com/2007/06/flickr' => 'http://scripts.mindtouch.com/flickr.xml',
		'http://services.mindtouch.com/deki/draft/2007/06/flickr' => 'http://scripts.mindtouch.com/flickr.xml',
		'http://services.mindtouch.com/deki/draft/2007/06/gabbly' => 'http://scripts.mindtouch.com/gabbly.xml',
		'http://services.mindtouch.com/deki/draft/2007/06/widgetbox' => 'http://scripts.mindtouch.com/widgetbox.xml',
		'sid://mindtouch.com/2007/06/syntax' => 'http://scripts.mindtouch.com/syntax.xml',
		'http://services.mindtouch.com/deki/draft/2007/06/syntax' => 'http://scripts.mindtouch.com/syntax.xml'
	);

	foreach($services as $sid => $uri) {
		$newServiceExists = false;
		$q = $wgDatabase->select(
			'services',
			'service_id',
			array('service_sid' => $sid)
		);
		if ($r = $wgDatabase->fetchRow($q)) {
			$service_id = $r['service_id'];

			// check if the new service has already been defined
			$q = $wgDatabase->select(
				array('services','service_config'),
				'COUNT(*) as count',
				array('services.service_id = service_config.service_id', 'config_name' => 'manifest', 'config_value' => $uri)
			);
			$r = $wgDatabase->fetchRow($q);
			if ($r['count'] > 0) { 
				$newServiceExists = true;	
			}

			// if the XML extension already exists do nothing, otherwise change SID and add manifest
			if(!$newServiceExists) {
				$wgDatabase->update(
					'services',
					array('service_sid' => $dekiscriptSid ),
					array('service_id' => $service_id)
				);
				$wgDatabase->insert(
					'service_config',
					array('service_id' => $service_id, 'config_name' => 'manifest', 'config_value' => $uri)
				);
				echo("Migrating built-in extension to dekiscript extension: $uri\n");
			}
		}
	}
}

function do_resource_update_for_strict() {
	global $wgDatabase;
	
	$meta = $wgDatabase->fieldInfo('resources', 'resrev_content_id');
	if ($meta->not_null == 1) 
	{
		$wgDatabase->query("alter table `resources` change `res_create_timestamp` `res_create_timestamp` datetime  DEFAULT '0001-01-01 00:00:00' NOT NULL , change `res_update_timestamp` `res_update_timestamp` datetime  DEFAULT '0001-01-01 00:00:00' NOT NULL , change `res_create_user_id` `res_create_user_id` int (4)UNSIGNED  DEFAULT '0' NOT NULL , change `res_update_user_id` `res_update_user_id` int (4)UNSIGNED  DEFAULT '0' NOT NULL , change `resrev_rev` `resrev_rev` smallint (2) DEFAULT '0' NOT NULL , "
			."change `resrev_user_id` `resrev_user_id` int (4)UNSIGNED  DEFAULT '0' NOT NULL , change `resrev_parent_id` `resrev_parent_id` int (4)UNSIGNED   NULL , change `resrev_parent_page_id` `resrev_parent_page_id` int (4)UNSIGNED   NULL , change `resrev_parent_user_id` `resrev_parent_user_id` int (4)UNSIGNED   NULL , change `resrev_change_description` `resrev_change_description` varchar (255) NULL COLLATE utf8_general_ci , change `resrev_timestamp` `resrev_timestamp` datetime  DEFAULT "
			."'0001-01-01 00:00:00' NOT NULL , change `resrev_content_id` `resrev_content_id` int (4)UNSIGNED NULL , change `resrev_language` `resrev_language` varchar (255) NULL  COLLATE utf8_general_ci , change `resrev_meta` `resrev_meta` text   NULL  COLLATE utf8_general_ci,  change `resrev_mimetype` `resrev_mimetype` varchar (255) DEFAULT '' NOT NULL  COLLATE utf8_general_ci;");
		$wgDatabase->query("alter table `resourcerevs` change `resrev_id` `resrev_id` int (4)UNSIGNED   NOT NULL AUTO_INCREMENT , change `resrev_res_id` `resrev_res_id` int (4)UNSIGNED  DEFAULT '0' NOT NULL , change `resrev_rev` `resrev_rev` smallint (2) DEFAULT '0' NOT NULL , change `resrev_user_id` `resrev_user_id` int (4)UNSIGNED  DEFAULT '0' NOT NULL , change `resrev_change_description` `resrev_change_description` varchar (255) DEFAULT NULL  COLLATE utf8_general_ci , change `resrev_timestamp` "
			."`resrev_timestamp` datetime  DEFAULT '0001-01-01 00:00:00' NOT NULL , change `resrev_content_id` `resrev_content_id` int (4)UNSIGNED   NULL , change `resrev_language` `resrev_language` varchar (255) DEFAULT NULL  COLLATE utf8_general_ci, change `resrev_mimetype` `resrev_mimetype` varchar (255) DEFAULT '' NOT NULL  COLLATE utf8_general_ci;");
		$wgDatabase->query("alter table `resourcecontents` change `rescontent_mimetype` `rescontent_mimetype` varchar (255) DEFAULT '' NOT NULL  COLLATE utf8_general_ci");
		echo("Fix resources tables for strict mode\n");
	}
}
function do_files_config_keys_update() {
	global $wgDatabase;

	// add files/blacklisted-disposition-mimetypes
	$q = $wgDatabase->select(
		'config', 
		array('COUNT(*) as count'), 
		array('config_key' => 'files/blacklisted-disposition-mimetypes')
	);
	$r = $wgDatabase->fetchRow($q);
        if ($r['count'] == 0) { 
		$wgDatabase->insert('config', array('config_key' => 'files/blacklisted-disposition-mimetypes', 'config_value' => ''));
		echo("added 'files/blacklisted-disposition-mimetypes' config key\n"); 
	}

	// add files/whitelisted-disposition-mimetypes
	$q = $wgDatabase->select(
		'config', 
		array('COUNT(*) as count'), 
		array('config_key' => 'files/whitelisted-disposition-mimetypes')
	);
	$r = $wgDatabase->fetchRow($q);
	if ($r['count'] == 0) {
		$wgDatabase->insert('config', 
			array('config_key' => 'files/whitelisted-disposition-mimetypes', 'config_value' => 'text/plain, text/xml, application/xml, application/pdf, application/msword, application/vnd.ms-excel, application/vnd.ms-powerpoint, application/vnd.openxmlformats-officedocument.wordprocessingml.document, application/vnd.openxmlformats-officedocument.spreadsheetml.sheet, application/vnd.openxmlformats-officedocument.presentationml.presentation, application/vnd.oasis.opendocument.presentation, application/vnd.oasis.opendocument.spreadsheet, application/vnd.oasis.opendocument.text, application/x-shockwave-flash'));
		echo("added 'files/whitelisted-disposition-mimetypes' config key\n"); 
	}

	// add files/force-text-extensions
	$q = $wgDatabase->select(
		'config', 
		array('COUNT(*) as count'), 
		array('config_key' => 'files/force-text-extensions')
	);
	$r = $wgDatabase->fetchRow($q);
	if ($r['count'] == 0) {
		$wgDatabase->insert('config', array('config_key' => 'files/force-text-extensions', 'config_value' => 'htm, html, xhtml, bat, reg, sh'));
		echo("added 'files/force-text-extensions' config key\n"); 
	}
}

function do_namespace_column_update() {
	global $wgDatabase, $wgActiveTemplate, $wgActiveSkin;
	$data = $wgDatabase->fieldInfo('recentchanges', 'rc_namespace');
	if(!$data->unsigned) {
		$wgDatabase->update(
			'recentchanges',
			array('rc_namespace' => 102),
			array('rc_namespace < 0')
			);
	
		$wgDatabase->query("ALTER table `recentchanges` modify column `rc_namespace` tinyint(3) unsigned NOT NULL default '0'");
		echo("Changing recentchanges.rc_namespace column\n");
	}
	$data = $wgDatabase->fieldInfo('transactions', 't_namespace');
	if(!$data->unsigned) {
		$wgDatabase->update(
			'transactions',
			array('t_namespace' => 102),
			array('t_namespace < 0')
			);
	
		$wgDatabase->query("ALTER table `transactions` modify column `t_namespace` tinyint(2) unsigned default NULL");
		echo("Changing transactions.t_namespace column\n");
	}
}
function do_customized_styles_to_properties() {
	global $wgDatabase, $wgActiveTemplate, $wgActiveSkin;
	
	// find the admin or sysop user who is active 
	$adminUserId = 0;
	$q = $wgDatabase->select(
		'users', 
		array('user_id'), 
		array('user_name' => 'Admin', 'user_active' => 1)
	);
	if ($r = $wgDatabase->fetchArray($q)) 
	{
		$adminUserId = $r['user_id'];
	}
	else {
		$q = $wgDatabase->select(
			'users', 
			array('user_id'), 
			array('user_name' => 'Sysop', 'user_active' => 1)
		);
		if ($r = $wgDatabase->fetchArray($q)) {
			$adminUserId = $r['user_id'];	
		}
	}
	
	if ($adminUserId == 0) {
		$adminUserId = 1; //fuzzy guess
	}
	
	$settings = array();
	$skin = $wgActiveSkin;
	$style = $wgActiveTemplate;
	
	# transition custom CSS regions
	$q = $wgDatabase->select('config', array('config_id', 'config_key', 'config_value'), array("config_key IN ('ui/skin', 'ui/template', 'ui/custom/bodycss', 'ui/custom/css')"));
	while ($r = $wgDatabase->fetchArray($q)) {
		$settings[$r['config_key']] = $r['config_value'];
	}
	if (isset($settings['ui/skin'])) {
		$skin = $settings['ui/template'];
		$style = $settings['ui/skin'];	
	}
	
	// combine the css values
	if (!empty($settings['ui/custom/bodycss']) || !empty($settings['ui/custom/css'])) {
		echo("Migrating custom CSS\n");
		$css = $settings['ui/custom/css'] . "\n /* Body CSS */ \n" . $settings['ui/custom/bodycss'];
		
		// insert the file as a resource with the original copy (placeholder; will be updated later with head)
		$wgDatabase->insert('resources', 
			array(
				'res_headrev' => '1',
				'res_type' => 4, // it's a property!
				'res_create_timestamp' => wfTimestamp(TS_DB), 
				'res_update_timestamp' => wfTimestamp(TS_DB), 
				'res_update_user_id' => $adminUserId, 
				'res_create_user_id' => $adminUserId
			)
		);
		$resource_id = $wgDatabase->insertId();
		
		$wgDatabase->insert('resourcecontents', 
			array(
				'rescontent_res_id' => $resource_id, 
				'rescontent_res_rev' => '1', 
				'rescontent_size' => strlen($css),
				'rescontent_value' => $css, 
				'rescontent_location' => null, 
				'rescontent_mimetype' => 'text/css; charset=utf-8'
			)
		); 
		$resource_content_id = $wgDatabase->insertId();
		
		$resourcerev = array(
			'resrev_res_id' => $resource_id, 
			'resrev_rev' => '1', 
			'resrev_user_id' => $adminUserId, 
			'resrev_parent_id' => null, 
			'resrev_change_mask' => 35, 
			'resrev_content_id' => $resource_content_id,
			'resrev_name' => 'urn:ui.deki.mindtouch.com#css.'.strtolower($skin).'.'.strtolower($style), 
			'resrev_timestamp' => wfTimestamp(TS_DB), 
			'resrev_size' => strlen($css),
			'resrev_mimetype' => 'text/css; charset=utf-8'
		);
		$wgDatabase->insert('resourcerevs', $resourcerev); 
		
		unset($resourcerev['resrev_res_id']);
		$resourcerev['res_update_timestamp'] = wfTimestamp(TS_DB); // update the headrev count with the total transitioned files
		$resourcerev['res_update_user_id'] = $adminUserId; // update the headrev count with the total transitioned files
	
		$wgDatabase->update('resources', $resourcerev, array('res_id' => $resource_id));
		$q = $wgDatabase->delete('config', array("config_key IN ('ui/custom/bodycss', 'ui/custom/css')"));
	}
	
	# transition custom HTML regions
	$htmlareas = array();
	global $IP;
	require_once($IP.'/includes/dream.php');
	
	$configs = array();
	
	$q = $wgDatabase->query("SELECT config_id, config_key, config_value FROM config WHERE config_key LIKE '%ui/custom/html%' ORDER BY config_key ASC");
	
	while ($r = $wgDatabase->fetchRow($q)) {
		echo("Migrating HTML region ".$r['config_key']."\n");
		//ui/custom/html/fiesta/opengarden/html2
		$keys = explode('/', $r['config_key']);
		$skin = $keys[3];
		$theme = $keys[4];
		$area = (int) str_replace('html', '', $keys[5]);
		$key = 'urn:ui.deki.mindtouch.com#html.'.strtolower($skin).'.'.strtolower($theme);
		if (!array_key_exists($key, $htmlareas)) {
			$htmlareas[$key] = array();
		}
		
		$htmlareas[$key][] = array(
			'@id' => $area + 1,
			'#text' => $r['config_value']
		);
		
		$configs[] = $r['config_id'];
	}
	
	foreach ($htmlareas as $key => $value) {
		$customregion = encode_xml(array(
			'regions' => array(
				'region' => $value
			)
		));
		
		// insert the file as a resource with the original copy (placeholder; will be updated later with head)
		$wgDatabase->insert('resources', 
			array(
				'res_headrev' => '1',
				'res_type' => 4, // it's a property!
				'res_create_timestamp' => wfTimestamp(TS_DB), 
				'res_update_timestamp' => wfTimestamp(TS_DB), 
				'res_update_user_id' => $adminUserId, 
				'res_create_user_id' => $adminUserId
			)
		);
		$resource_id = $wgDatabase->insertId();
		
		$wgDatabase->insert('resourcecontents', 
			array(
				'rescontent_res_id' => $resource_id, 
				'rescontent_res_rev' => '1', 
				'rescontent_size' => strlen($customregion),
				'rescontent_value' => $customregion, 
				'rescontent_location' => null, 
				'rescontent_mimetype' => 'application/xml; charset=utf-8'
			)
		); 
		$resource_content_id = $wgDatabase->insertId();
		
		$resourcerev = array(
			'resrev_res_id' => $resource_id, 
			'resrev_rev' => '1', 
			'resrev_user_id' => $adminUserId, 
			'resrev_parent_id' => null, 
			'resrev_change_mask' => 35, 
			'resrev_content_id' => $resource_content_id,
			'resrev_name' => $key, 
			'resrev_timestamp' => wfTimestamp(TS_DB), 
			'resrev_size' => strlen($customregion),
			'resrev_mimetype' => 'application/xml; charset=utf-8'
		);
		$wgDatabase->insert('resourcerevs', $resourcerev); 
		
		unset($resourcerev['resrev_res_id']);
		$resourcerev['res_update_timestamp'] = wfTimestamp(TS_DB); // update the headrev count with the total transitioned files
		$resourcerev['res_update_user_id'] = $adminUserId; // update the headrev count with the total transitioned files
	
		$wgDatabase->update('resources', $resourcerev, array('res_id' => $resource_id));
	}	
	
	// clean up configuration if this script didn't break
	foreach ($configs as $id) {
		$wgDatabase->delete('config', array('config_id' => $id));	
	}
}

// converts db user options into user properties
function do_user_metadata_transition() {
	global $wgDatabase;
	
	$user_options = $wgDatabase->fieldInfo('users', 'user_options');
	if ($user_options === false) {
		return;
	}
	
	echo("Transitioning user options to new user columns... "); 
	$q = $wgDatabase->select('users', array('user_id', 'user_options'));
	while ($r = $wgDatabase->fetchArray($q)) {
		if (empty($r['user_options'])) {
			continue;
		}
		$options = explode("\n", $r['user_options']);
		if (empty($options) || !is_array($options)) {
			continue;
		}
		$user_tz = '';
		$user_lang = '';
		foreach ($options as $option) {
			list($key, $val) = explode("=", $option);
			if (!in_array($key, array('timecorrection', 'language'))) {
				continue;
			}
			if ($key == 'timecorrection') {
				$user_tz = $val;
			}
			if ($key == 'language') {
				if (!empty($options['variant'])) {
					$val = $val.'-'.$options['variant'];
				}
				$user_lang = $val;
			}
		}
		$wgDatabase->update('users', array('user_language' => $user_lang, 'user_timezone' => $user_tz), array('user_id' => $r['user_id']));
	}
	echo("done\n");
	$wgDatabase->query("ALTER table `users` DROP `user_options`");
	return; 
}

function do_add_user_meta_columns() {
	global $wgDatabase;
	$user_language = $wgDatabase->fieldInfo('users', 'user_language');
	if (is_null($user_language) || !$user_language) {
		$wgDatabase->query("ALTER table `users` add column `user_language` varchar (255)  NULL");
		$wgDatabase->query("alter table `users` add column `user_timezone` varchar (255)  NULL");
		echo("User columns for language and timezone added\n");
	}
}
function do_make_parent_nullable() {
	global $wgDatabase;
	
	$data = $wgDatabase->fieldInfo('resources', 'resrev_parent_id');
	if ($data->not_null == 1) {
		$wgDatabase->query("ALTER table `resourcerevs` change `resrev_parent_id` `resrev_parent_id` int(4) UNSIGNED NULL");
		$wgDatabase->query("ALTER table `resources` change `resrev_parent_id` `resrev_parent_id` int(4) UNSIGNED  NULL");
		echo("Resources' parent_id to be nullable\n");
	}
}

function do_remove_user_registrations_table() {
	global $wgDatabase;
	if (!$wgDatabase->tableExists('user_registrations')) {
		return;
	}
	$wgDatabase->query("DROP table `user_registrations`");
	echo("Dropped unused table user registrations\n");
}
function do_remove_site_stats() {
	global $wgDatabase;
	if (!$wgDatabase->tableExists('site_stats')) {
		return;
	}
	$wgDatabase->query("DROP table `site_stats`");
	echo("Dropped unused table site_stats\n");
}

function do_file_fix_upload() {
	global $wgDatabase;
	
	$meta = $wgDatabase->fieldInfo('resourcefilemap', 'resource_id');
	if ($meta->not_null == '1') {
		$wgDatabase->query("ALTER table `resourcefilemap` change `resource_id` `resource_id` int (10)UNSIGNED default null");
		echo("altered resourcefilemap table\n");
	}
}

function do_add_user_creation_timestamp() {
	global $wgDatabase;
	
	$meta = $wgDatabase->fieldInfo('users', 'user_create_timestamp');
	if ($meta === false) {
		$wgDatabase->query("ALTER TABLE `users` ADD `user_create_timestamp` DATETIME NOT NULL default '0001-01-01 00:00:00'");
		
		// get the oldest page timestamp in the current or old tables, so we can set all users to that value, at the least
		$q = $wgDatabase->select('pages', array('page_timestamp'), array("page_timestamp != ''"), array('ORDER' => 'page_timestamp ASC', 'LIMIT' => 1));
		if ($r = $wgDatabase->fetchRow($q)) {
			$cur_ts = $r['page_timestamp'];
		}
		$q = $wgDatabase->select('old', array('old_timestamp'), array(), array('ORDER' => 'old_timestamp ASC', 'LIMIT' => 1));
		if ($r = $wgDatabase->fetchRow($q)) {
			$old_ts = $r['old_timestamp'];
		}
		$default_ts = strcmp($old_ts, $cur_ts) < 0 ? $old_ts: $cur_ts; 
		
		// we need to populate this value as close as possible - so we'll use the oldest available timestamp available for that user
		$q = $wgDatabase->select('users', array('user_id', 'user_name'));
		while ($r = $wgDatabase->fetchRow($q)) {
			$timestamp = null;
			
			// first look into the archive table; sort by the ID asc (should be a reasonable assumption)
			// fortunately, we don't need to encode the titles when doing the page look-up; see bug #5535
			$qo = $wgDatabase->select(
				'old', 
				array('old_timestamp'), 
				array('old_title' => $r['user_name'], 'old_namespace' => NS_USER),
				array('ORDER' => 'old_id ASC', 'LIMIT' => 1)
			);
			if ($wgDatabase->numRows($qo) > 0) {
				if ($qr = $wgDatabase->fetchRow($qo)) {
					$timestamp = $qr['old_timestamp'];
				}
			}
			// if no archive, just go to the current page timestamp
			else {
				$qc = $wgDatabase->select(
					'pages', 
					array('page_timestamp'), 
					array('page_title' => $r['user_name'], 'page_namespace' => NS_USER)
				);
				if ($rc = $wgDatabase->fetchRow($qc)) {
					$timestamp = $rc['page_timestamp'];
				}
			}
			
			// if the value is null, use the site-wide one
			if (is_null($timestamp)) {
				$timestamp = wfTimestamp(TS_DB, $default_ts);
			}
			$wgDatabase->update('users', array('user_create_timestamp' => $timestamp), array('user_id' => $r['user_id']));
		}
		echo("Added user creation timestamp\n");
	}
}
   
// See http://wiki.developer.mindtouch.com/User:SteveB/Lyons_-_API_Work_Items/Resource_Data_Model#Alternative_storage_path_scheme_for_file_system_storage
function do_file_schema_move_get_file_name($id, $suffix = '.bin') {
	$id = (string) $id;
	$path = '';
	for ($i = strlen($id); $i > 3; $i -= 3) {
		$path .= substr($id, 0, 3) . '/';
		$id = substr($id, 3);
	}
	return $path . $id . $suffix;
}

function do_file_schema_move_verify_file_path($filepath, $prefixpath = '', $chown = null, $chgrp = null) {
	if (strpos($filepath, '/') === false) {
		return !is_file($prefixpath.'/'.$filepath);
	}
	$filepaths = explode('/', $filepath); 
	$file = array_pop($filepaths); //last path will be a file
	$path = $prefixpath;
	foreach ($filepaths as $filepath) {
		if (empty($filepath)) {
			break;
		}
		if (!is_dir($path.'/'.$filepath)) {
			mkdir($path.'/'.$filepath);
			if (!is_null($chown)) {
				chown($path.'/'.$filepath, $chown);
			}
			if (!is_null($chgrp)) {
				chgrp($path.'/'.$filepath, $chgrp);
			}
		}
		$path = $path.'/'.$filepath;
	}
	return !is_file($prefixpath.'/'.$filepath);
}

function do_file_schema_move() {
 	
	global $wgDatabase, $wgCacheDirectory;
	
	echo("Start update script\n");
	# STEP 1: Verify the state of Deki
	
	// no more attachments table - this script has already run successfully
	if (!$wgDatabase->tableExists('attachments')) {
		return;
	}
	
	// should we drop attachments?
	$q = $wgDatabase->query("SELECT COUNT(*) as count FROM attachments");
	if ($r = $wgDatabase->fetchRow($q)) {
		if ($r['count'] == 0) {
			// no more files left in attachments; drop the table
			$wgDatabase->query("DROP table `attachments`");
			return;
		}
	}
	
	// create the backups table
	if (!$wgDatabase->tableExists('attachments_backup')) {
		$wgDatabase->query("CREATE table `attachments_backup` SELECT * FROM attachments");
	}
	
	// determine if we're using a local file system
	$q = $wgDatabase->select('config', array('config_value'), array('config_key' => 'storage/type'));
	if ($r = $wgDatabase->fetchRow($q)) {
		$localfs = $r['config_value'] == 'fs';
	}
	
	// if on a local file system, attempt to retrieve the local path
	if ($localfs) {
		$localpath = null;
		$q = $wgDatabase->select('config', array('config_value'), array('config_key' => 'storage/fs/path'));
		if ($r = $wgDatabase->fetchRow($q)) {
			$localpath = $r['config_value'];
		}
		if (is_null($localpath)) {
			echo("ERROR: no storage path specified - this MUST be corrected\n");
			return;
		}
		
		//let's check to see if the attachments folder exists
		if (!is_dir($localpath.'/')) {
			if (!mkdir($localpath.'/')) {
				echo("ERROR: script does not have access to write to the storage path.\n");
				return;
			}
			else {
				echo("Wrote the new attachments folder\n");
			}
		}
		
		$chown = fileowner($localpath);
		$chgrp = filegroup($localpath);
	}
	
	# STEP 2: Get the base revisions (oldest) of a file
	$q = $wgDatabase->query("SELECT attachments.* FROM attachments INNER JOIN ( SELECT MIN(at_id) as at_baseid FROM attachments group by at_from, at_name, at_extension) ids ON attachments.at_id = ids.at_baseid");
	
	$last_desc = null;
	while ($r = $wgDatabase->fetchArray($q)) {
				
		$file_name = !empty($r['at_extension']) ? $r['at_name'].'.'.$r['at_extension']: $r['at_name'];
		
		echo("Migrating fileid ".$r['at_id']."\n");
		
		// TODO: check if this is a restart
		
		// insert the file as a resource with the original copy (placeholder; will be updated later with head)
		$wgDatabase->insert('resources', 
			array(
				'res_headrev' => '0',
				'res_type' => 2, // it's a file!
				'res_create_timestamp' => wfTimestamp(TS_DB, $r['at_timestamp']), /* Timestamp of first version */
				'res_create_user_id' => $r['at_user']
			)
		);
		$resource_id = $wgDatabase->insertId();
				
		// grab all revisions of this file, including itself (it's repetitive, i know)
		$query = $wgDatabase->select(
			'attachments', 
			array('*'), 
			array('at_from' => $r['at_from'], 'at_name' => $r['at_name'], 'at_extension' => $r['at_extension']), 
			array('ORDER' => 'at_filename ASC')
		);
		
		$i = 0; // keeps a count of how many file revisions are transitioned
		$k = 1; //attachment description counter
		$attach_desc = null; // know when attachments have been changed
		$desc_set = false;
		$head_id = $r['at_id'];
		while ($row = $wgDatabase->fetchArray($query)) {			
			if ($localfs) {
				$old_file = $localpath.'/'.$row['at_from'].'/'.$row['at_filename'];
				if (!is_file($old_file)) {
					echo("ERROR: could not find ".$old_file."\n");
					$wgDatabase->delete('attachments', array('at_id' => $row['at_id'])); // clean up this file
					continue; //don't transition later versions of the files, either; repairing will be difficult to do
				}
				else {
					$new_path = do_file_schema_move_get_file_name($resource_id, '.res').'/'.do_file_schema_move_get_file_name($i + 1, '');
					
					// do the necessary directories exist, and does the file *not* exist?
					if (do_file_schema_move_verify_file_path($new_path, $localpath, $chown, $chgrp)) {
						
						rename($old_file, $localpath.'/'.$new_path.'.bin');
						if (is_file($old_file.'_thumb')) {
							rename($old_file.'_thumb', $localpath.'/'.$new_path.'.thumb');
						}
						if (is_file($old_file.'_webview')) {
							rename($old_file.'_webview', $localpath.'/'.$new_path.'.webview');
						}
						echo("Moved ".$old_file." to ".$localpath.'/'.$new_path."\n".' (res. '.$resource_id.')');
					}
				}
			}
			
			$i++; 
									
			$metaxml = '<meta>';
			
			// sanity check on at_image_height
			if (!empty($row['at_image_width'])) {
				$metaxml.= '<width>'.($row['at_image_width'] < 65535 ? $row['at_image_width']: '0').'</width>';	
			}
			if (!empty($row['at_image_height'])) {
				$metaxml.= '<height>'.($row['at_image_height'] < 65535 ? $row['at_image_height']: '0').'</height>';	
			}
			$metaxml.= '<fileid>'.$head_id.'</fileid>';
			$metaxml.= '</meta>';
			
			$wgDatabase->insert('resourcecontents', 
				array(
					'rescontent_res_id' => $resource_id, 
					'rescontent_res_rev' => $i, 
					'rescontent_size' => $row['at_filesize'],
					'rescontent_value' => null, 
					'rescontent_location' => !$localfs ? '' /* TODO: S3 location */ : null, 
					'rescontent_mimetype' => $row['at_filetype']
				)
			); 
			$resource_content_id = $wgDatabase->insertId();
			
			$resourcerev = array(
				'resrev_res_id' => $resource_id, 
				'resrev_rev' => $i, 
				'resrev_user_id' => $row['at_user'], 
				'resrev_parent_id' => '0', 
				'resrev_change_mask' => $i == 1 ? 43: 9, 
				'resrev_content_id' => $resource_content_id,
				'resrev_parent_page_id' => $row['at_from'],
				'resrev_name' => $file_name, 
				'resrev_timestamp' => wfTimestamp(TS_DB, $row['at_timestamp']), 
				'resrev_size' => $row['at_filesize'], 
				'resrev_meta' => $metaxml, 
				'resrev_mimetype' => $row['at_filetype']
			);
			
			// insert the resource revision
			$wgDatabase->insert('resourcerevs', $resourcerev); 
			
			$created = false;
			// if this is the first description for this attachment
			if (!($desc_set) && !empty($row['at_desc'])) {				
				$wgDatabase->insert('resources', 
					array(
						'res_headrev' => '1', 
						'res_type' => 4, // it's a property!
						'res_create_timestamp' => wfTimestamp(TS_DB, $row['at_timestamp']),
						'res_create_user_id' => $r['at_user'], /* head rev */
						'res_update_timestamp' => wfTimestamp(TS_DB, $row['at_timestamp']), 
						'res_update_user_id' => $r['at_user'],  /* head rev */
						'resrev_parent_id' => $resource_id
					)
				);
				$attachdesc_id = $wgDatabase->insertId();
				$desc_set = true;
				$created = true;
				$attach_desc = $row['at_desc']; //set for comp
			}
			
			// we need to insert the resource revision if attachment description exists as a resource
			if ($desc_set && !empty($row['at_desc']) && ($attach_desc != $row['at_desc'] || $created)) {
				$wgDatabase->insert('resourcecontents', 
					array(
						'rescontent_res_id' => $attachdesc_id, 
						'rescontent_res_rev' => $k, 
						'rescontent_value' => $row['at_desc'], 
						'rescontent_size' => strlen($row['at_desc']), 
						'rescontent_mimetype' => 'text/plain; charset=utf-8'
					)
				);
				$attachdesc_content_id = $wgDatabase->insertId();
				$content_query = $wgDatabase->lastQuery();
				
				$desc_rev = array(
					'resrev_res_id' => $attachdesc_id, 
					'resrev_rev' => $k, 
					'resrev_user_id' => $row['at_user'], 
					'resrev_content_id' => $attachdesc_content_id, 
					'resrev_parent_id' => $resource_id, 
					'resrev_change_mask' => '43',
					'resrev_name' => 'urn:deki.mindtouch.com#description', 
					'resrev_size' => strlen($row['at_desc']), 
					'resrev_timestamp' => wfTimestamp(TS_DB, $row['at_timestamp']), 
					'resrev_mimetype' => 'text/plain; charset=utf-8'
				);
				$wgDatabase->insert('resourcerevs', $desc_rev); 
				
				unset($desc_rev['resrev_res_id']);
				$desc_rev['res_headrev'] = $k; // update the headrev count with the total transitioned files
				$desc_rev['res_update_timestamp'] = wfTimestamp(TS_DB, $row['at_timestamp']); // update the headrev count with the total transitioned files
				$desc_rev['res_update_user_id'] = $row['at_user']; // update the headrev count with the total transitioned files
			
				$wgDatabase->update('resources', $desc_rev, array('res_id' => $attachdesc_id));
				$k++;
				echo("Wrote attachment description #".$row['at_id']." (".$row['at_desc'].") for resource #".$resource_id 
					. ' and content # '.$attachdesc_content_id.' (Content query: '.$content_query.')'."\n");
			}
			
			$wgDatabase->delete('attachments', array('at_id' => $row['at_id'])); // clean up this file
			$lastrow = $row;
		}
				
		// nothing was transitioned; delete the placeholder resource
		if ($i == 0) {
			$wgDatabase->delete('resources', array('res_id' => $resource_id));
		}
		else {
			
			// insert into resourcefilemap
			$wgDatabase->insert('resourcefilemap', array('file_id' => $head_id, 'resource_id' => $resource_id));
			
			// if deleted!
			if (!empty($lastrow['at_removed'])) {
				// need to get the userid of the user who has deleted this file
				$qu = $wgDatabase->select(
					array('users'), 
					array('user_id'), 
					array('user_name' => $lastrow['at_removed_by_text'])
				);
				if ($ru = $wgDatabase->fetchArray($qu)) {
					$i++; //incremement resrev_rev
					
					$resourcerev = array(
						'resrev_res_id' => $resource_id, 
						'resrev_rev' => $i, 
						'resrev_user_id' => $ru['user_id'], 
						'resrev_parent_id' => '0', 
						'resrev_content_id' => $resource_content_id, /* todo: check and see if the last content_id is correct */
						'resrev_change_mask' => 16, 
						'resrev_parent_page_id' => $lastrow['at_from'],
						'resrev_name' => $file_name, 
						'resrev_timestamp' => wfTimestamp(TS_DB, $lastrow['at_timestamp']), 
						'resrev_size' => $lastrow['at_filesize'], 
						'resrev_meta' => $metaxml, 
						'resrev_mimetype' => $lastrow['at_filetype'],
						'resrev_deleted' => '1'
					);

					// insert the deleted resource revision to mark this file as deleted
					$wgDatabase->insert('resourcerevs', $resourcerev); 
					echo("Marked ".$resource_id." as a deleted attachment\n");
					$resourcerev['res_deleted'] = 1;
				}
			}
			
			// update the attachment description property
			if (!empty($desc_rev)) {
				unset($desc_rev['resrev_res_id']);
				$desc_rev['res_update_timestamp'] = wfTimestamp(TS_DB, $lastrow['at_timestamp']);
				$desc_rev['res_update_user_id'] = $lastrow['at_user'];
				$wgDatabase->update('resources', $desc_rev, array('res_id' => $attachdesc_id));
			}
			
			// remove the resrev_id since it's not in the resources table
			unset($resourcerev['resrev_res_id']); 
			
			// this should actually do an addition - subsequent restarts may cause this value to be incrementally increased
			$resourcerev['res_headrev'] = $i; // update the headrev count with the total transitioned files
			$resourcerev['res_update_timestamp'] = wfTimestamp(TS_DB, $lastrow['at_timestamp']); // update the headrev count with the total transitioned files
			$resourcerev['res_update_user_id'] = $lastrow['at_user']; // update the headrev count with the total transitioned files
			
			// update the resources with the latest resource value
			$wgDatabase->update('resources', $resourcerev, array('res_id' => $resource_id));
		}
	} 
	
	
	// should we drop attachments?
	$q = $wgDatabase->query("SELECT COUNT(*) as count FROM attachments");
	if ($r = $wgDatabase->fetchRow($q)) {
		if ($r['count'] == 0) {
			// no more files left in attachments; drop the table
			$wgDatabase->query("DROP table `attachments`");
			return;
		}
	}
}

function add_comment_posterid_index() {
	global $wgDatabase;
	$meta = $wgDatabase->fieldInfo('comments', 'cmnt_poster_user_id');
	if ($meta->multiple_key == 0) {
		$wgDatabase->query("ALTER table `comments` add index `cmnt_poster_user_id` (`cmnt_poster_user_id`)");
		echo("added comment poster user id index to comments table\n");
	}	
}

function do_add_properties() {
	global $wgDatabase;
	if (!$wgDatabase->tableExists('resourcerevs')) {
        dbsource("maintenance/archives/patch-resources.sql");
        echo("added resource properties\n");	
	}
	else {
		$meta = $wgDatabase->fieldInfo('resources', 'resrev_change_mask');
		if ($meta === false) {
	        dbsource("maintenance/archives/patch-resources-new.sql");
	        echo("added resource properties (#2)\n");
		}
	}
}

function do_remove_admin_pages() {
	global $wgProductVersion;
	
	// to be on the safe side, any version after the non-existent 8.09 release
	// todo: if we ever choose to add in admin pages, this script should be blocked with an endpoint on the version
	if (strcmp($wgProductVersion, '8.09') > 0) {
		global $wgDatabase;
		echo("removing admin-namespaced pages from database (cleanup)\n");
		$wgDatabase->delete('pages', array('page_namespace' => NS_ADMIN));
	}
}

function do_set_permission_impersonator() {
	global $wgDatabase;
	$q = $wgDatabase->select('config', array('COUNT(*) as count'), array('config_key' => 'security/admin-user-for-impersonation'));
	$r = $wgDatabase->fetchRow($q);
	if ($r['count'] > 0) {
		return;
	}
	
	$q = $wgDatabase->select(
		'users', 
		array('user_name'), 
		array('(user_name = "Admin" OR user_name = "Sysop") AND user_role_id = 5'), 
		array('ORDER' => 'user_id ASC')
	);
	if ($r = $wgDatabase->fetchArray($q)) {
		$wgDatabase->insert('config', array('config_key' => 'security/admin-user-for-impersonation', 'config_value' => $r['user_name']));
		echo("Admin username to config table for system administration\n");
	}
	return;
}

function do_transaction_update() {
	global $wgDatabase;
	if (!$wgDatabase->tableExists('transactions')) {
        dbsource("maintenance/archives/patch-transactions.sql");
        echo("added transactions table\n");
	}
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
		echo("Added index `page_parent` to `pages`\n");
	}
}

function do_add_attachment_indices() {
	global $wgDatabase;
	if (!$wgDatabase->tableExists('attachments')) {
		return;
	}
	$meta = $wgDatabase->fieldInfo('attachments', 'at_from');
	if ($meta === false) 
	{
		return;	
	}
	if ($meta->multiple_key == 0) 
	{
		$wgDatabase->query("ALTER TABLE `attachments` ADD INDEX `name_ext_from` (`at_from`, `at_extension`, `at_name`) ");
		echo("Updated `attachments`: Added indices\n");	
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
	echo("Updated `roles`: dropped unused column role_hidden\n");
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

function do_change_private_permission() {
	global $wgDatabase;
	$q = $wgDatabase->select('restrictions', array('restriction_perm_flags'), array('restriction_name' => 'Private'));
	if ($r = $wgDatabase->fetchRow($q)) {
		if ($r['restriction_perm_flags'] == 3) {
			echo("removed browse from private restriction\n");
			$wgDatabase->update('restrictions', array('restriction_perm_flags' => '1'), array('restriction_name' => 'Private'));
		}
	}
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
 		echo('tags table fixed'."\n");
	}
}
function do_default_user_update() {
	global $wgDatabase;
	$q = $wgDatabase->select('config', array('config_id'), array('config_key' => 'security/new-account-role'));
	if ($wgDatabase->numRows($q) == 0) {
		$wgDatabase->insert('config', array('config_key' => 'security/new-account-role', 'config_value' => 'Contributor'));
		echo("added 'security/new-account-role' config key\n");
	}
}

function do_requestlog_update() {
	global $wgDatabase;
	if(!$wgDatabase->tableExists("requestlog") && !$wgDatabase->tableExists("requeststats")) {
	        echo "adding requestlog, requeststats tables\n";
	        dbsource("maintenance/archives/patch-requestlog.sql");
	}
}
function do_anonymous_user_update() {
	global $wgDatabase;
	if (!$wgDatabase->tableExists('attachments')) {
		return;
	}	
	dbsource("maintenance/archives/patch-anonymous_user.sql");
	echo("anonymous user updated for all tables\n");
}

function do_attach_nullable_update() {
	global $wgDatabase;
	if (!$wgDatabase->tableExists('attachments')) {
		return;
	}
	$meta = $wgDatabase->fieldInfo('attachments', 'at_image_width');
	if ($meta && $meta->not_null == 1) {
		echo("nullable attachment update finished\n");
		dbsource("maintenance/archives/patch-attach_null.sql");
	}	
}
function do_add_users_ext_column_update() {
	global $wgDatabase;
	$meta = $wgDatabase->fieldInfo("users", "user_external_name");
	if (!$meta) {
		echo("user external column added\n");
		dbsource("maintenance/archives/patch-user_ext.sql");
	}
}
function do_convert_web_to_feed_service() {
	global $wgDatabase;
	$wgDatabase->query("UPDATE services SET service_sid='http://services.mindtouch.com/deki/draft/2007/06/feed' WHERE service_sid='http://services.mindtouch.com/deki/draft/2007/06/web' OR service_sid='http://services.mindtouch.com/deki/draft/2007/06/webdata'");
}

function do_archive_ar_old_id_update() {
	global $wgDatabase;
	$meta = $wgDatabase->fieldInfo("archive", "ar_old_id");
	if(!$meta) {
		echo "adding ar_old_id to archive table\n";
		dbsource("maintenance/archives/patch-archive-ar_old_id.sql");
		return true;
	}
	return false;
}

function do_add_apikey_to_db() {
	global $wgDekiApiKey;
	global $wgDatabase;
	$q = $wgDatabase->select(array('config'), array('config_id', 'config_value'), array('config_key' => 'security/api-key'));
	if (!$q) {
		if (!isset($wgDekiApiKey)) {
			echo('>>>>YOU DO NOT HAVE A VALID API KEY; PLEASE INSERT MANUALLY<<<<'."\n");	
		}
		else {
			$wgDatabase->insert('config', array('config_key' => 'security/api-key', 'config_value' => $wgDekiApiKey));
		}
	}
}
function do_users_serviceid_update() {
	global $wgDatabase;
	$sql = "SELECT `user_id`  FROM `users` WHERE `user_service_id` = 0";
	$r = $wgDatabase->query($sql);
	if($wgDatabase->numRows($r) > 0) {
		echo("updating user_service_id\n");
		dbsource("maintenance/archives/patch-users-service-id.sql");
	}
}
function do_move_apikey_update() {
	global $wgDatabase;
	$q = $wgDatabase->select(array('config'), array('config_id', 'config_value'), array('config_key' => 'wgDekiApikey'));
	if ($r = $wgDatabase->fetchRow($q)) {
		if ($r && $r['config_value'] != '') {
			wfPatchLocalSettingsForApiKey($r['config_value']);
			$wgDatabase->delete('config', array('config_id' => $r['config_id']));
			echo('Dekihost API key moved to LocalSettings.php from database'."\n");
		}
	}	
}
function do_comments_update() {
	global $wgDatabase;
	if(!$wgDatabase->tableExists("comments")) {
		echo("comments table added\n");
		dbsource("maintenance/archives/patch-comments.sql");
	}
}

function do_service_sid_nullable_update() {
	global $wgDatabase;
	$meta = $wgDatabase->fieldInfo("services", "service_sid");
	if($meta->not_null) {
		echo("updated services table set service_sid nullable\n");
		dbsource("maintenance/archives/patch-services_service_sid.sql");
	}
}

function do_archive_id() {
	global $wgDatabase;
	$meta = $wgDatabase->fieldInfo("archive", "ar_id");
	if (!$meta) {
		echo('updated archives table with primary key'."\n");
		dbsource("maintenance/archives/patch-archive-ar_id.sql");
	}
}

function do_extension_services_update() {
	global $wgDatabase;
	$sql = "UPDATE services SET service_type='ext' WHERE service_type='livedata'";
	$wgDatabase->query($sql);
	$sql = "SELECT * FROM services WHERE service_type='ext'";
	$res = $wgDatabase->query($sql);
	if($wgDatabase->numRows($res) == 0) {
		echo "adding extension services\n";
		dbsource("maintenance/archives/patch-extension_services.sql");
		return true;
	}
	return false;
}

function do_settings_update() {
	global $wgDatabase;
	if(!$wgDatabase->tableExists("config")) {
        echo "adding config table\n";
        dbsource("maintenance/archives/patch-settings.sql");
	}
}

function do_special_pages_update() {
	global $wgDatabase;
    
	$wgDatabase->delete('pages', array('page_namespace' => '103', 'page_title' => 'AdminVisual'));
	$wgDatabase->delete('pages', array('page_namespace' => '103', 'page_title' => 'SiteSettings'));
    
	// create Special:Tags, Special:Events, Special:About if they don't exist
	$q = $wgDatabase->select('pages', array('COUNT(*) as count'), array('page_namespace' => 101, 'page_title' => 'Tags')); 
	if ($r = $wgDatabase->fetchRow($q)) {
		if ($r['count'] == 0) {
			$wgDatabase->insert('pages', array('page_namespace' => 101, 'page_title' => 'Tags', 'page_tip' => 'Special page', 'page_parent' => 0, 'page_restriction_id' => 0, 'page_content_type' => 'text/plain', 'page_text' => '', 'page_comment' => '', 'page_toc' => '', 'page_timestamp' => '', 'page_touched' => '', 'page_inverse_timestamp' => ''));
		}
	}
	
	$q = $wgDatabase->select('pages', array('COUNT(*) as count'), array('page_namespace' => 101, 'page_title' => 'Events')); 
	if ($r = $wgDatabase->fetchRow($q)) {
		if ($r['count'] == 0) {
			$wgDatabase->insert('pages', array('page_namespace' => 101, 'page_title' => 'Events', 'page_tip' => 'Special page', 'page_parent' => 0, 'page_restriction_id' => 0, 'page_content_type' => 'text/plain', 'page_text' => '', 'page_comment' => '', 'page_toc' => '', 'page_timestamp' => '', 'page_touched' => '', 'page_inverse_timestamp' => ''));
		}
	}
	
	$q = $wgDatabase->select('pages', array('COUNT(*) as count'), array('page_namespace' => 101, 'page_title' => 'About')); 
	if ($r = $wgDatabase->fetchRow($q)) {
		if ($r['count'] == 0) {
			$wgDatabase->insert('pages', array('page_namespace' => 101, 'page_title' => 'About', 'page_tip' => 'Special page', 'page_parent' => 0, 'page_restriction_id' => 0, 'page_content_type' => 'text/plain', 'page_text' => '', 'page_comment' => '', 'page_toc' => '', 'page_timestamp' => '', 'page_touched' => '', 'page_inverse_timestamp' => ''));        
		}
	}

	// create Template: page if it doesn't exist
	$q = $wgDatabase->select('pages', array('COUNT(*) as count'), array('page_namespace' => 10, 'page_title' => ''));
	if ($r = $wgDatabase->fetchRow($q)) {
		if ($r['count'] == 0) {
			$wgDatabase->insert('pages', array('page_namespace' => 10, 'page_title' => '', 'page_tip' => 'Template page', 'page_parent' => 0, 'page_restriction_id' => 0, 'page_content_type' => 'text/plain', 'page_text' => '', 'page_comment' => '', 'page_toc' => '', 'page_timestamp' => '', 'page_touched' => '', 'page_inverse_timestamp' => ''));
		}
	}

	// create User: page if it doesn't exist
	$q = $wgDatabase->select('pages', array('COUNT(*) as count'), array('page_namespace' => 2, 'page_title' => ''));
	if ($r = $wgDatabase->fetchRow($q)) {
		if ($r['count'] == 0) {
			$wgDatabase->insert('pages', array('page_namespace' => 2, 'page_title' => '', 'page_tip' => 'User page', 'page_parent' => 0, 'page_restriction_id' => 0, 'page_content_type' => 'text/plain', 'page_text' => '', 'page_comment' => '', 'page_toc' => '', 'page_timestamp' => '', 'page_touched' => '', 'page_inverse_timestamp' => ''));
		}
	}

	// create Special: page if it doesn't exist
	$q = $wgDatabase->select('pages', array('COUNT(*) as count'), array('page_namespace' => 101, 'page_title' => ''));
	if ($r = $wgDatabase->fetchRow($q)) {
		if ($r['count'] == 0) {
			$wgDatabase->insert('pages', array('page_namespace' => 101, 'page_title' => '', 'page_tip' => 'Special page', 'page_parent' => 0, 'page_restriction_id' => 2, 'page_content_type' => 'application/x.deki-text', 'page_text' => '{{ wiki.tree("Special:") }}', 'page_comment' => '', 'page_toc' => '', 'page_timestamp' => '', 'page_touched' => '', 'page_inverse_timestamp' => ''));
		}
	}

	return true;
}

function do_archive_ar_last_page_id_update() {
	global $wgDatabase;
	$meta = $wgDatabase->fieldInfo("archive", "ar_last_page_id");
	if(!$meta) {
		echo "adding ar_last_page_id to archive table\n";
		dbsource("maintenance/archives/patch-archive-ar_last_page_id.sql");
		return true;
	}
	return false;
}

function do_old_content_type_update() {
	global $wgDatabase;
	$meta = $wgDatabase->fieldInfo("old", "old_content_type");
	if(!$meta) {
		echo "adding old_content_type to old table\n";
		dbsource("maintenance/archives/patch-old_content_type.sql");
		return true;
	}
	return false;
}

function do_users_unique_keys_update() {
	// r3349
	global $wgDatabase;
	if(!$wgDatabase->indexExists("groups", "group_name")) {
		echo "adding group_name index to groups table\n";
		dbsource("maintenance/archives/patch-users_unique_keys.sql");
		return true;
	}
	return false;
}

function do_users_user_builtin_update() {
	global $wgDatabase;
        $meta = $wgDatabase->fieldInfo("users", "user_builtin");
        if(!$meta) {
                echo "adding user_builtin to users table\n";
                dbsource("maintenance/archives/patch-users_user_builtin.sql");
                return true;
        }
        return false;
}

function do_tags_tag_type_update() {
        global $wgDatabase;
        $meta = $wgDatabase->fieldInfo("tags", "tag_type");
        if(!$meta) {
                echo "adding tag_types to tags table\n";
                dbsource("maintenance/archives/patch-tags-tag_type.sql");
                return true;
        }
        return false;
}

function do_specialpages_update() {
	global $wgDatabase;
	$sql = "SELECT * FROM pages WHERE page_namespace IN (101,103)";
	$res = $wgDatabase->query($sql);
	if($wgDatabase->numRows($res) == 0) {
		echo "adding special pages\n";
		dbsource("maintenance/archives/patch-specialpages.sql");
		return true;
	}
	return false;
}

function do_tags_update() {
	global $wgDatabase;
	if(!$wgDatabase->tableExists("tags") || !$wgDatabase->tableExists("tag_map"))
	{
		echo "adding tags tables\n";
		dbsource("maintenance/archives/patch-tags.sql");
		return true;
	}
}

function do_attachments_update() {
	global $wgDatabase;
	if (!$wgDatabase->tableExists('attachments')) {
		return;
	}
	$meta = $wgDatabase->fieldInfo("attachments", "at_image_width");
	if(!$meta)
        {
		dbsource("maintenance/archives/patch-attachments.sql");
		echo("adding at_image_width, at_image_height to `attachments`\n");
		return true;
	}
	return false;
}

function add_pages_contenttype() {
	global $wgDatabase;
	$meta = $wgDatabase->fieldInfo( "pages", "page_content_type" );	
	if (!$meta) {
		dbsource( "maintenance/archives/patch-pages-content-type.sql" );
		echo("adding content-type column to `pages`\n");
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
		echo "combo permissions system DB upgrade...";
		dbsource( "maintenance/archives/patch-permission-system-combo.sql" );
		echo "done\n";
		return true;
	}
	return false;
}

function add_pages_language_and_displayname() {
	global $wgDatabase;
	$meta = $wgDatabase->fieldInfo( "pages", "page_language" );	
	if (!$meta) {
		dbsource( "maintenance/archives/patch-pages-language_and_displayname.sql" );
		echo("adding language and display_name columns to `pages` (this may take a moment, please be patient!)\n");
		return true;
	}
	return false;
}

function do_banning_update() {
	global $wgDatabase;
	if(!$wgDatabase->tableExists("bans")) {
		echo("bans, banips, banusers tables added\n");
		dbsource("maintenance/archives/patch-banning.sql");
	}
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
	global $wgDatabase;
	echo "Applying misc database updates...";
	dbsource( "maintenance/archives/onupdate.sql" );
	echo "done\n";
	return true;
}
?>
