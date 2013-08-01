<?php 

class MindTouchImageGalleryLitePlugin
{
	const AJAX_FORMATTER = 'MindTouchImageGalleryLite';

	/**
	 * Initialize the plugin and hooks into the application
	 */
	public static function load()
	{
		// hook
		DekiPlugin::registerHook(Hooks::AJAX_FORMAT . self::AJAX_FORMATTER, array('MindTouchImageGalleryLitePlugin', 'ajaxHook'));
		DekiPlugin::registerHook(Hooks::PAGE_RENDER_IMAGES, array('MindTouchImageGalleryLitePlugin', 'hook'));
	}

		/**
	 * Called when the ajax formatter for files is hit
	 * 
	 * @param string &$body
	 * @param string &$message
	 * @param bool &$success
	 * @return N/A
	 */
	public static function ajaxHook(&$body, &$message, &$success)
	{
		$Request = DekiRequest::getInstance();	
		// default to failure
		$success = false;

		$pageId = $Request->getInt('page_id');
		$ArticleTitle = Title::newFromID($pageId);
		if (is_null($ArticleTitle))
		{
			$message = 'Page not found';
			return;
		}
		// create the objects required to generate the table html
		$Article = new Article($ArticleTitle);
		$files = $Article->getFiles();

		$previews = self::filterFiles($files);
		$body = self::generateGallery($previews);
		$success = true;
	}

	public static function hook($Article, &$html, &$count)
	{
		global $wgArticle;
		
		$files = $wgArticle->getFiles();
		$previews = self::filterFiles($files);

		$html = self::generateGallery($previews);
		// add the wrapping gallery div#deki-image-gallery-lite
		$html = '<div id="deki-image-gallery-lite">' . $html . '</div>';

		$count = count($previews);
	}

	protected static function filterFiles(&$files)
	{
		$previews = array();
		foreach ($files as $key => &$file)
		{
			$Preview = DekiFilePreview::newFromArray($file);
			if (!is_null($Preview) && $Preview->hasPreview())
			{
				$previews[] = $Preview;
			}
		}
		unset($file);
		
		return $previews;
	}

	protected static function &generateGallery(&$previews, $previewsPerRow = 3)
	{
		$html = '';
		
		$Table = new DomTable();
		$Table->addClass('table');

		// compute the column widths
		$width = intval(100 / $previewsPerRow);
		$widths = array_fill(0, $previewsPerRow, $width.'%');
		call_user_func_array(array($Table, 'setColWidths'), $widths);

		$Table->addRow();
		$Table->addHeading(wfMsg('MindTouch.ImageGalleryLite.heading', count($previews)), $previewsPerRow);

		$count = 0;
		foreach ($previews as &$Preview)
		{
			if ($Preview->hasThumb())
			{
				if ($count % $previewsPerRow == 0)
				{
					$Table->addRow();
				}
				$count++;
				
				$filename = $Preview->toHtml();
				$description = htmlspecialchars($Preview->getDescription());

				// generate the column markup
				$colHtml =	
					'<a rel="image-gallery-lite" class="lightbox" href="'. $Preview->getWebview() .'" title="'. $filename .'">'.
						$Preview->getThumbImage().
					'</a>'
				;
				
				$colHtml .= '<div class="information">';
				if (!empty($description))
				{
					$colHtml .= '<span class="description">'. $description . '</span>';
				}
				$colHtml .= '<a href="'. $Preview->getHref() .'" title="'. wfmsg('MindTouch.ImageGalleryLite.title.filename') .'">'. $filename . '</a>';
				$colHtml .= '</div>';
				
				// add the new column to the gallery
				$Col = $Table->addCol($colHtml);
				$Col->addClass('image');
			}
		}
		unset($Preview);


		if ($count > 0)
		{
			while ($count % $previewsPerRow != 0)
			{
				$Table->addCol('&nbsp;');
				$count++;
			}
		}
		else
		{
			// empty gallery
			$Table->addClass('empty');

			$Table->addRow();
			$Col = $Table->addCol(wfMsg('MindTouch.ImageGalleryLite.data.empty'), 3);
			$Col->addClass('empty');
		}
		
		$html = $Table->saveHtml();
		return $html;
	}
}
MindTouchImageGalleryLitePlugin::load();
