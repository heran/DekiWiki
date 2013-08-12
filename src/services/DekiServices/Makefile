include $(root_dir)/Makefile.include

ASSEMBLY_NAME=mindtouch.deki.services
ASSEMBLY=$(ASSEMBLY_NAME).dll

TARGET=$(addprefix $(build_prefix)/, $(ASSEMBLY))

sources = 								\
		AssemblyInfo.cs					\
		Authentication/LdapClient.cs	\
		Authentication/LdapService.cs	\
		Authentication/DrupalService.cs	\
		Authentication/JoomlaService.cs \
		Authentication/WordPressService.cs  \
		Authentication/HttpPassthroughService.cs	\
		Authentication/DekiSocialService.cs	\
		Authentication/DekiPassthroughService.cs	\
		Authentication/VBulletinService.cs	\
		Authentication/XmlAuthenticationService.cs	\
		Extension/DekiScriptService.cs	\
		Extension/FileSystemService.cs	\
		Extension/GoogleService.cs		\
		Extension/YahooService.cs		\
		Extension/YouTrackService.cs	\
		Extension/GraphvizService.cs	\
		Extension/MathService.cs		\
		Extension/MediaService.cs		\
		Extension/MySqlService.cs		\
		Extension/RealTimeInfoService.cs	\
		Extension/ImageMagickService.cs	\
		Extension/UnsafeHtmlService.cs	\
		Extension/FeedService.cs		\
		Extension/WindowsLiveService.cs	\
		Extension/DapperService.cs		\
		Extension/MantisService.cs		\
		Extension/PageReloadNotificationService.cs	\
		Extension/PostgreSqlService.cs	\
		Extension/SubversionService.cs	\
		Extension/SilverlightService.cs	\
		Extension/SvgService.cs			\
		Extension/TracService.cs		\
		Extension/JiraService.cs		\
		Extension/AdoDotNetService.cs	\
		Extension/TestService.cs		\
		Extension/WebCacheService.cs	\
		Extension/PostgreSqlService.cs	\
		Properties/Settings.Designer.cs	\
		Web\ References/MantisWebServices/Reference.cs	\
		Web\ References/JiraWebServices/Reference.cs

references = 																\
		-r:System															\
		-r:System.Data														\
		-r:System.Xml														\
		-r:System.Web														\
		-r:System.Web.Services												\
		-r:System.Configuration												\
		-r:$(addprefix $(build_prefix)/, mindtouch.deki.ext.dll)			\
		-r:$(addprefix $(build_prefix)/, mindtouch.deki.script.dll)			\
		-r:$(addprefix $(root_dir)/, redist/mindtouch.dream.dll)			\
		-r:$(addprefix $(root_dir)/, redist/log4net.dll)					\
		-r:$(addprefix $(root_dir)/, redist/SgmlReaderDll.dll)				\
		-r:$(addprefix $(root_dir)/, redist/Novell.Directory.Ldap.dll)			\
		-r:$(addprefix $(root_dir)/, redist/MySql.Data.dll)			\
		-r:$(addprefix $(root_dir)/, redist/CookComputing.XmlRpcV2.dll)			\
		-r:$(addprefix $(root_dir)/, redist/Mono.Security.dll)			\
		-resource:'Resources/rss2html.xslt,MindTouch.Deki.Services.Resources.rss2html.xslt' 	\
		-resource:'Resources/slideshow.css,MindTouch.Deki.Services.Resources.slideshow.css'	\
		-resource:'Resources/slideshow.js,MindTouch.Deki.Services.Resources.slideshow.js'	\
		-resource:'Resources/sorttable.js,MindTouch.Deki.Services.Resources.sorttable.js'	\
		-resource:'Resources/Mantis.css,MindTouch.Deki.Services.Resources.Mantis.css'	\
		-resource:'Resources/dapper-run.xml,MindTouch.Deki.Services.Resources.dapper-run.xml'	\
		-resource:'Resources/google-feeds.xml,MindTouch.Deki.Services.Resources.google-feeds.xml'	\
		-resource:'Resources/google-findfeeds.xml,MindTouch.Deki.Services.Resources.google-findfeeds.xml'	\
		-resource:'Resources/google-map.xml,MindTouch.Deki.Services.Resources.google-map.xml'	\
		-resource:'Resources/google-search.xml,MindTouch.Deki.Services.Resources.google-search.xml'	\
        -resource:'Resources/livechannel.html,MindTouch.Deki.Services.Resources.livechannel.html'	\
        -resource:'Resources/liveprivacy.html,MindTouch.Deki.Services.Resources.liveprivacy.html'	\
        -resource:'Resources/feed-logo.png,MindTouch.Deki.Services.Resources.feed-logo.png'	\
        -resource:'Resources/filesystem-logo.png,MindTouch.Deki.Services.Resources.filesystem-logo.png'	\
        -resource:'Resources/google-logo.png,MindTouch.Deki.Services.Resources.google-logo.png'	\
        -resource:'Resources/media-logo.png,MindTouch.Deki.Services.Resources.media-logo.png'	\
        -resource:'Resources/mysql-logo.png,MindTouch.Deki.Services.Resources.mysql-logo.png'	\
        -resource:'Resources/windowslive-logo.png,MindTouch.Deki.Services.Resources.windowslive-logo.png'	\
		-resource:'Resources/pgsql-logo.png,MindTouch.Deki.Services.Resources.pgsql-logo.png' \
        -resource:'Resources/yahoo-logo.png,MindTouch.Deki.Services.Resources.yahoo-logo.png' \
        -resource:'Resources/dapper-logo.png,MindTouch.Deki.Services.Resources.dapper-logo.png'	\
        -resource:'Resources/silverlight-logo.png,MindTouch.Deki.Services.Resources.silverlight-logo.png'	\
        -resource:'Resources/Silverlight.js,MindTouch.Deki.Services.Resources.Silverlight.js'	\

$(addprefix $(build_prefix)/, mindtouch.dream.dll): $(addprefix $(root_dir)/, redist/mindtouch.dream.dll)
	cp $(addprefix $(root_dir)/, redist/mindtouch.dream.dll) $(addprefix $(build_prefix)/, mindtouch.dream.dll)

$(addprefix $(build_prefix)/, SgmlReaderDll.dll): $(addprefix $(root_dir)/, redist/SgmlReaderDll.dll)
	cp $(addprefix $(root_dir)/, redist/SgmlReaderDll.dll) $(addprefix $(build_prefix)/, SgmlReaderDll.dll)

$(addprefix $(build_prefix)/, log4net.dll): $(addprefix $(root_dir)/, redist/log4net.dll)
	cp $(addprefix $(root_dir)/, redist/log4net.dll) $(addprefix $(build_prefix)/, log4net.dll)

$(addprefix $(build_prefix)/, Novell.Directory.Ldap.dll): $(addprefix $(root_dir)/, redist/Novell.Directory.Ldap.dll)
	cp $(addprefix $(root_dir)/, redist/Novell.Directory.Ldap.dll) $(addprefix $(build_prefix)/, Novell.Directory.Ldap.dll)

$(addprefix $(build_prefix)/, MySql.Data.dll): $(addprefix $(root_dir)/, redist/MySql.Data.dll)
	cp $(addprefix $(root_dir)/, redist/MySql.Data.dll) $(addprefix $(build_prefix)/, MySql.Data.dll)

$(addprefix $(build_prefix)/, CookComputing.XmlRpcV2.dll): $(addprefix $(root_dir)/, redist/CookComputing.XmlRpcV2.dll)
	cp $(addprefix $(root_dir)/, redist/CookComputing.XmlRpcV2.dll) $(addprefix $(build_prefix)/, CookComputing.XmlRpcV2.dll)

$(addprefix $(build_prefix)/, Mono.Security.dll): $(addprefix $(root_dir)/, redist/Mono.Security.dll)
	cp $(addprefix $(root_dir)/, redist/Mono.Security.dll) $(addprefix $(build_prefix)/, Mono.Security.dll)

$(TARGET): $(sources) $(addprefix $(build_prefix)/, mindtouch.dream.dll) $(addprefix $(build_prefix)/, SgmlReaderDll.dll) $(addprefix $(build_prefix)/, log4net.dll) $(addprefix $(build_prefix)/, Novell.Directory.Ldap.dll) $(addprefix $(build_prefix)/, MySql.Data.dll) $(addprefix $(build_prefix)/, CookComputing.XmlRpcV2.dll) $(addprefix $(build_prefix)/, Mono.Security.dll) $(addprefix $(build_prefix)/, mindtouch.deki.script.dll)
	$(MCS) $(MCS_FLAGS) /out:$(TARGET) /target:library $(references) $(sources)

all: $(TARGET) 
