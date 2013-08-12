include $(root_dir)/Makefile.include

ASSEMBLY_NAME=mindtouch.deki
ASSEMBLY=$(ASSEMBLY_NAME).dll

TARGET=$(addprefix $(build_prefix)/, $(ASSEMBLY))

sources = 							\
		Exceptions.cs				\
		Export/Export.cs			\
		Export/SiteExportBuilder.cs			\
		Export/SiteImportBuilder.cs			\
		Logic/PermissionsBL.cs		\
		Logic/PageArchiveBL.cs		\
		Logic/AttachmentPreviewBL.cs	\
		Logic/AttachmentBL.cs	\
		Logic/AuthBL.cs				\
		Logic/ExternalServiceSA.cs	\
		Logic/ExtensionBL.cs			\
		Logic/UserBL.cs				\
		Logic/PageBL.cs				\
		Logic/ServiceBL.cs			\
		Logic/PageSiteMapBL.cs		\
		Logic/SiteBL.cs				\
		Logic/TagBL.cs				\
		Logic/CommentBL.cs			\
		Logic/ConfigBL.cs			\
		Logic/GroupBL.cs			\
		Logic/NavBL.cs				\
		Logic/BanningBL.cs			\
		Logic/LicenseBL.cs			\
		Logic/RatingBL.cs			\
		Logic/RecentChangeBL.cs			\
		Logic/ResourceBL.cs			\
		Logic/PropertyBL.cs		\
		Logic/ResourceMapBL.cs		\
		Logic/SearchBL.cs           \
		PubSub/DekiDispatcher.cs    \
        Search/RankableSearchResultItem.cs      \
        Search/SearchResultRankCalculator.cs    \
		Search/SearchResult.cs            \
		Search/SearchResultDetail.cs      \
		Search/SearchResultItem.cs        \
        Search/SearchSerializer.cs        \
        Search/TrackingInfo.cs            \
		Storage/S3Storage.cs		\
		Storage/IStorageProvider.cs \
		Storage/FSStorage.cs		\
		PackageUpdate/PackageUpdater.cs               \
        PackageUpdate/PackageUpdaterService.cs        \
		UserSubscription/IPageSubscriptionInstance.cs	\
		UserSubscription/PageSubscriptionInstance.cs	\
		UserSubscription/PageSubscriptionUser.cs		\
		UserSubscription/NotificationDelayQueue.cs		\
		UserSubscription/NotificationUpdateRecord.cs	\
		UserSubscription/PageChangeCache.cs				\
		UserSubscription/PageChangeCacheData.cs			\
		AssemblyInfo.cs				\
		DekiChangeSink.cs			\
		DekiChangeSubscriptionService.cs	\
		DekiContext.cs				\
		DekiExceptionMapper.cs      \
        DekiResource.cs             \
        DekiResourceBuilder.cs      \
		DekiInstance.cs				\
		DekiWiki-Pages.cs			\
		DekiXmlParser.cs		    \
		DekiWiki-Banning.cs			\
		DekiWiki-Files.cs			\
		DekiWiki-Functions.cs		\
		DekiWiki-Host.cs    		\
		DekiWiki-Nav.cs				\
		DekiWiki-News.cs			\
		DekiWiki-Properties.cs		\
		DekiWiki-Search.cs          \
		DekiWiki-Services.cs		\
		DekiWiki-Users.cs			\
		DekiWiki-Ratings.cs			\
		DekiWiki-RecycleBin.cs		\
		DekiWiki-Site.cs			\
		DekiWiki-SiteRoles.cs		\
		DekiWiki-Tags.cs			\
		DekiWiki-Comments.cs		\
		DekiWiki-Groups.cs			\
		DekiWiki-License.cs			\
		DekiWikiService.cs			\
		DekiResources.cs			\
		Font.cs						\
		IndexRebuilder.cs           \
		PackageService.cs		\
		PlainTextResourceManager.cs \
		ServiceRepository.cs        \
		Utils.cs					\
		VarnishPurgeService.cs		\
		Varnish/UpdateDelayQueue.cs	\
		Varnish/UpdateRecord.cs		\
		Varnish/UpdateRecordDispatcher.cs	\
		WikiManagement/InstanceManager.cs	\
		WikiManagement/LocalInstanceManager.cs		\
		WikiManagement/RemoteInstanceManager.cs		\
		WikiManagement/DbInstanceManager.cs
		
references = 																\
		-r:System															\
		-r:System.Core														\
		-r:System.Data														\
		-r:System.Xml														\
		-r:$(addprefix $(build_prefix)/, MySql.Data.dll)					\
		-r:$(addprefix $(build_prefix)/, mindtouch.dream.dll)				\
		-r:$(addprefix $(build_prefix)/, mindtouch.deki.data.dll)			\
		-r:$(addprefix $(build_prefix)/, mindtouch.deki.ext.dll)			\
		-r:$(addprefix $(build_prefix)/, mindtouch.deki.script.dll)			\
		-r:$(addprefix $(build_prefix)/, mindtouch.deki.util.dll)			\
		-r:$(addprefix $(root_dir)/, redist/Autofac.dll)					\
		-r:$(addprefix $(root_dir)/, redist/log4net.dll)					\
		-r:$(addprefix $(root_dir)/, redist/SgmlReaderDll.dll)				\
		-r:$(addprefix $(root_dir)/, redist/Lucene.Net.dll)					\
		-resource:'Resources/ExtensionConverter.xslt,MindTouch.Deki.Resources.ExtensionConverter.xslt'	\
		-resource:'Resources/ExtensionRender.xslt,MindTouch.Deki.Resources.ExtensionRender.xslt'	\
		-resource:'Resources/Arial.mtdf,MindTouch.Deki.Resources.Arial.mtdf'	\
		-resource:'Resources/mindtouch-logo.png,MindTouch.Deki.Resources.mindtouch-logo.png'	\
		-resource:'Resources/license-community.xml,MindTouch.Deki.Resources.license-community.xml'
	
$(addprefix $(build_prefix)/, MySql.Data.dll): 
	cp $(addprefix $(root_dir)/, redist/MySql.Data.dll) $(addprefix $(build_prefix)/, MySql.Data.dll)

$(addprefix $(build_prefix)/, mindtouch.dream.dll): $(addprefix $(root_dir)/, redist/mindtouch.dream.dll)
	cp $(addprefix $(root_dir)/, redist/mindtouch.dream.dll) $(addprefix $(build_prefix)/, mindtouch.dream.dll)

$(addprefix $(build_prefix)/, SgmlReaderDll.dll): $(addprefix $(root_dir)/, redist/SgmlReaderDll.dll)
	cp $(addprefix $(root_dir)/, redist/SgmlReaderDll.dll) $(addprefix $(build_prefix)/, SgmlReaderDll.dll)

$(addprefix $(build_prefix)/, Lucene.Net.dll): $(addprefix $(root_dir)/, redist/Lucene.Net.dll)
	cp $(addprefix $(root_dir)/, redist/Lucene.Net.dll) $(addprefix $(build_prefix)/, Lucene.Net.dll)

$(TARGET): $(sources) $(addprefix $(build_prefix)/, MySql.Data.dll) $(addprefix $(build_prefix)/, mindtouch.dream.dll) $(addprefix $(build_prefix)/, SgmlReaderDll.dll) $(addprefix $(build_prefix)/, Lucene.Net.dll) $(addprefix $(build_prefix)/, mindtouch.deki.data.dll) $(addprefix $(build_prefix)/, mindtouch.deki.script.dll) $(addprefix $(build_prefix)/, mindtouch.deki.util.dll)
	$(MCS) $(MCS_FLAGS) /out:$(TARGET) /target:library $(references) $(sources)

$(addprefix $(build_prefix)/, mindtouch.deki.startup.xml): mindtouch.deki.startup.xml
	cp -v mindtouch.deki.startup.xml $(addprefix $(build_prefix)/, mindtouch.deki.startup.xml)

all: $(TARGET) $(addprefix $(build_prefix)/, mindtouch.deki.startup.xml)
