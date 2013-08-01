/*
 * MindTouch Deki - enterprise collaboration and integration platform
 * Copyright (C) 2006-2009 MindTouch, Inc.
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
 * A content assist processor proposes completions and computes context
 * information for a particular character offset. This interface is similar to
 * Eclipse's IContentAssistProcessor
 * @class
 * @author Sergey Chikuyonok (serge.che@gmail.com)
 * @link http://chikuyonok.ru
 *
 * @author MindTouch
 * @link http://mindtouch.com
 *
 * @include "EditorViewer.js"
 * @include "CompletitionProposal.js"
 */
function DekiScriptAssistProcessor(words)
{
	this.namespaces = {};

	if (words)
	{
		this.setWords(words);
	}
}

DekiScriptAssistProcessor.prototype =
{
	/**
	 * Returns a list of completion proposals based on the specified location
	 * within the document that corresponds to the current cursor position
	 * within the text viewer.
	 *
	 * @param {TextViewer} viewer The viewer whose document is used to compute
	 * the proposals
	 *
	 * @return {CompletitionProposal[]}
	 */
	computeCompletionProposals: function(viewer)
	{
		var cur_text = viewer.getCurrentText(),
			cur_word = this.getWord(cur_text);

		var proposals = null,
			suggestions = this.suggestWords(cur_word);

		if (suggestions.length)
		{
			var next_text = viewer.getNextText(),
				next_word = this.getWord(next_text, true),
				dot = cur_word.indexOf('.'),
				offset = cur_text.length - cur_word.length,
				length = next_word.length + cur_word.length;

			if (dot > 0)
			{
				offset += dot + 1;
				length -= dot + 1;
			}
			
			proposals = [];

			for (var i = 0, il = suggestions.length; i < il; i++)
			{
				var suggestion = suggestions[i];
				var proposal = this.completitionProposalFactory(suggestion.word, offset, length, suggestion.info);
				proposals.push(proposal);
			}
		}

		return proposals;
	},

	/**
	 * @param {String} str The actual string to be inserted into the document
	 * @param {Number} offset The offset of the text to be replaced
	 * @param {Number} length The length of the text to be replaced
	 * @param {String} info Additional info
	 * @return {CompletionProposal}
	 */
	completitionProposalFactory: function(str, offset, length, info)
	{
		return new CompletionProposal(str, offset, length, null, info);
	},

	getWord : function(text, fromStart)
	{
		if (!text.length)
		{
			return '';
		}

		var re = fromStart ?
			/^([^\s,\!\?\#%\^\$\(\)\{\}<>'"«»\.]+)/ :
			/([^\s,\!\?\#%\^\$\(\)\{\}<>'"«»]+)$/;

		var matches = re.exec(text);

		return matches ? matches[1] : '';
	},

	setWords: function(words, namespace)
	{
		var _w = namespace ? this.getNamespace(namespace) : {},
			_ns = {}, i, il;

		_w['__all'] = words.sort();

		// index words by first letter for faster search
		for (i = 0, il = words.length; i < il; i++)
		{
			var word = words[i],
				dot = word.indexOf('.');

			// if namespace is not specified but word has dot
			// put namespace name to the global namespace
			// and collect all words for this namespace
			if (!namespace && dot > 0)
			{
				var ns = word.substring(0, dot);
				word = word.substring(dot + 1);

				_ns[ns] = _ns[ns] || [];
				_ns[ns].push(word);

				if (_ns[ns].length == 1)
				{
					word = ns;
				}
				else
				{
					continue;
				}
			}

			var ch = word.toString().charAt(0);
			if (!(ch in _w))
			{
				_w[ch] = [];
			}

			_w[ch].push(word);
		}

		if (!namespace)
		{
			this.words = _w;

			for (i in _ns)
			{
				this.setWords(_ns[i], i);
			}
		}
		else
		{
			this.namespaces[namespace] = _w;
		}
	},

	getNamespace : function(namespace)
	{
		this.namespaces[namespace] = this.namespaces[namespace] || {};
		return this.namespaces[namespace];
	},

	setAdditionalInfo : function( additionalInfo )
	{
		this.additionalInfo = additionalInfo;
	},

	/**
	 * Returs suggested code assist proposals for prefix
	 * @param {String} prefix Word prefix
	 * @return {Array}
	 */
	suggestWords: function(prefix)
	{
		prefix = String(prefix);
		var result = [],
			_words = this.words,
			dot = prefix.indexOf('.'),
			ns, i, il, word, nsWord;

		if (dot > 0)
		{
			ns = prefix.substring(0, dot);
			prefix = prefix.substring(dot + 1);
			_words = this.namespaces[ns] || {};

			if (prefix.length == 0 && _words['__all'])
			{
				for (var i in _words['__all'])
				{
					word = {word : _words['__all'][i]};

					nsWord = ns + '.' + _words['__all'][i];
					if (this.additionalInfo[nsWord])
					{
						word['info'] = this.additionalInfo[nsWord];
					}
					
					result.push(word);
				}
			}
		}

		if (prefix && prefix.length && _words)
		{
			var first_ch = prefix.charAt(0),
				prefix_len = prefix.length;
			if (first_ch in _words)
			{
				var words = _words[first_ch];
				for (i = 0, il = words.length; i < il; i++)
				{
					word = {word : words[i].toString()};
					if (word.word.indexOf(prefix) === 0 && word.word.length > prefix_len)
					{
						nsWord = ns ? ns + '.' + word.word : word.word;

						if (this.additionalInfo[nsWord])
						{
							word['info'] = this.additionalInfo[nsWord];
						}

						result.push(word);
					}
				}
			}
		}

		return result;
	}
}
