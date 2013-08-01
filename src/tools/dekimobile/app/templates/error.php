<?php
/*
 * MindTouch Deki - enterprise collaboration and integration platform
 * Copyright (C) 2006-2008 MindTouch, Inc.
 * www.mindtouch.com  oss@mindtouch.com
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
 
/**
 * Special case view file for rendering errors in production
 */
$Exception = $this->get('error.exception');
?>

<div id="application-error">
	<h2>Sorry but there was an error while processing your request.</h2>
	<div class="block">
		Try hitting back on your browse to try again. If the problem persists please contact support with the following information.
	</div>

	<div class="title">
		<h3>Error Report</h3>
	</div>

	<div class="block">
		<p class="error">Error (Code: <?php echo $Exception->getCode(); ?>) <?php echo $Exception->getMessage(); ?></p>
		<textarea readonly="readonly"><?php echo htmlspecialchars($Exception->getTraceAsString()) . "\n"; ?>Error (Code: <?php echo $Exception->getCode(); ?>) <?php echo $Exception->getMessage(); ?></textarea>
	</div>
</div>
