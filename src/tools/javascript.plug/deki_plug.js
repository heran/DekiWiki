
var Deki = Deki || {};

(function(){
	
	Lang = YAHOO.lang;
	
	Deki.Plug = function(oConfig)
	{
		if ( Lang.isString( oConfig ) )
		{
			oConfig = { 'url' : oConfig };
		}
		
		this.Init(oConfig);
		
		return this;
	}
	
	Deki.Plug.prototype =
	{
		Protocol : null,
		User: null,
		Password : null,
		Host : null,
		Port : null,
		Path : null,
		Query : null,
		Anchor : null,
		Headers : {},
		Timeout : 3000,
		
		Connect : null,
		
		Init : function(oConfig)
		{
			oConfig = oConfig || {};
			
			var oUri = ( Lang.isString( oConfig.url ) ) ? parseUri( oConfig.url ) : oConfig.url;
			
			this.SetParam('Protocol', oUri.protocol);
			this.SetParam('Host', oUri.host);
			this.SetParam('Port', oUri.port);
			this.SetParam('Path', oUri.path);
			this.SetParam('Query', oUri.query);
			this.SetParam('Anchor', oUri.anchor);
			
			return this;
		},
		
		SetParam : function(sParam, mValue, mDefaultValue)
		{
			if ( ! Lang.isFunction(this[sParam]) && ! Lang.isUndefined(this[sParam]) )
			{
				mDefaultValue = ( Lang.isValue(mDefaultValue) ) ? mDefaultValue : null;
				this[sParam] = ( Lang.isValue(mValue) ) ? mValue : mDefaultValue;
			}
			
			return this;
		},
		
		GetParam : function(sParam)
		{
			var mValue;
			
			if ( Lang.hasOwnProperty(this, sParam) )
			{
				var mValue = this[sParam];
				if ( Lang.isString(mValue) && mValue.length == 0 )
				{
					mValue = null;
				}
			}
			
			return mValue;
		},
		
		At : function()
		{
			for ( var i = 0 ; i < arguments.length ; i++ )
			{
				var arg = arguments[i];
				
				this.Path += '/';
				
				if ( Lang.isArray(arg) )
				{
					this.Path += arg.shift();
				}
				else
				{
					arg += '';
					
					if ( arg.indexOf('=') === 0 )
					{
						this.Path += '=' + DekiWiki.url.encode(DekiWiki.url.encode(arg.substr(1)));
					}
					else
					{
						this.Path += DekiWiki.url.encode(DekiWiki.url.encode(arg));
					}
				}
			}
			
			return this;
		},
		
		With : function(sName, sValue)
		{
			if ( Lang.isValue(this.Query) && this.Query.length > 0 )
			{
				this.Query += '&';
			}
			
			sValue = ( Lang.isValue(sValue) ) ? DekiWiki.url.encode(sValue + '') : '';
			
			this.Query += DekiWiki.url.encode(sName + '') + '=' + sValue;
			
			return this;
		},
		
		SetHeaders : function(oHeaders)
		{
			if ( Lang.isObject(oHeaders) )
			{
				Lang.merge(this.Headers, oHeaders);
			}
			
			return this;
		},
		
		SetHeader : function(sName, sValue)
		{
			this.Headers[sName] = sValue;
			return this;
		},
		
		WithCredentials : function(sUser, sPassword)
		{
			this.SetParam('User', sUser);
			this.SetParam('Password', sPassword);

			return this;
		},
		
		ApplyCredentials : function()
		{
			var sUser = this.GetParam('User') || Deki.UserName;
			var sPassword = this.GetParam('Password') ||  prompt('Password for ' + sUser, '');

			//this.SetHeader('Authorization', 'Basic ' + base64_encode(sUser + ':' + sPassword));
		},
		
		Invoke : function(sMethod, sData, oCallback, oScope)
		{
			var sUri = this.GetUri();
			
			var cb = function(oResponse)
			{
				oScope = oScope || this;
				
				if ( Lang.isFunction(oCallback) )
				{
					var oResult = new Deki.Plug.Result(oResponse);
					oCallback.apply(oScope, [oResult]);
				}
			}
			
			var _oConnect_cb =
			{
				success : cb,
				failure : cb,
				timeout : this.Timeout,
				scope   : this
			}
			
			var sHeader;
			
			for ( sHeader in this.Headers )
			{
				YAHOO.util.Connect.initHeader(sHeader, this.Headers[sHeader]);
			}
			
			this.Connect = YAHOO.util.Connect.asyncRequest(sMethod, sUri, _oConnect_cb, sData);
		},
		
		Get : function(oConfig)
		{
			var aArguments = [ 'GET', null ];

			if ( Lang.isFunction(oConfig) )
			{
				aArguments.push(oConfig);				
			}
			else
			{
				oConfig = oConfig || {};
				
				if ( Lang.isFunction(oConfig.callback) )
				{
					aArguments.push(oConfig.callback);
				}
	
				if ( Lang.isFunction(oConfig.scope) )
				{
					aArguments.push(oConfig.scope);
				}
			}

			this.Invoke.apply(this, aArguments);
		},
		
		GetUri : function()
		{
			var sUri = '';
			
			var Protocol = this.GetParam('Protocol');
			var Port = this.GetParam('Port');
			var Query = this.GetParam('Query');
			var Anchor = this.GetParam('Anchor');

			if ( Lang.isValue(Protocol) )
			{
				sUri += Protocol + ':';
				if ( Protocol.toLowerCase() != 'mailto' )
				{
					sUri += '//';
				}
			}
			
			sUri += this.GetParam('Host');
			
			sUri += ( Lang.isValue(Port) ) ? ':' + Port : '';
			sUri += this.GetParam('Path');
			sUri += ( Lang.isValue(Query) ) ? '?' + Query : '';
			sUri += ( Lang.isValue(Anchor) ) ? '#' + Anchor : '';
			
			return sUri;
		}
	}
	
	Deki.Plug.Result = function(oResponse)
	{
		this.Result = oResponse;
		this.DataSource = null;
		
		this.HandleResponse();
	}
	
	Deki.Plug.Result.prototype = 
	{
		HandleResponse : function()
		{
			if ( this.IsSuccess() )
			{
				this.DataSource = new YAHOO.util.LocalDataSource(this.GetXml());
			}
		},
		
		IsSuccess : function()
		{
			var nStatus = this.GetStatus();
			return ( nStatus >= 200 && nStatus < 300 );
		},
		
		Parse : function( oResponseSchema, oCallback )
		{
			if ( this.DataSource )
			{
				this.DataSource.responseSchema = oResponseSchema;
				this.DataSource.sendRequest( null, oCallback );
			}
		},
		
		GetText : function()
		{
			return this.Result.responseText;
		},
		
		GetXml : function()
		{
			return this.Result.responseXML;
		},
		
		GetStatus : function()
		{
			return this.Result.status || 0;
		},
		
		GetStatusText : function()
		{
			return this.Result.statusText;
		},
		
		GetAllHeaders : function()
		{
			return this.Result.getAllResponseHeaders;
		},
		
		GetHeader : function(sHeader)
		{
			return this.Result.getResponseHeader[sHeader];
		}
	}
})();
