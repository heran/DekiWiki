include $(root_dir)/Makefile.include

ASSEMBLY_NAME=mindtouch.deki.data
ASSEMBLY=$(ASSEMBLY_NAME).dll

TARGET=$(addprefix $(build_prefix)/, $(ASSEMBLY))

sources = 							\
		Caching/CacheStatsWrapper.cs	\
		Caching/ICacheProvider.cs	\
		Caching/NullCache.cs		\
		Caching/DreamCache.cs		\
		Caching/CacheIterator.cs	\
		Entities/ServiceBE.cs		\
		Entities/ArchiveBE.cs		\
		Entities/OldBE.cs			\
		Entities/PageBE.cs			\
		Entities/UserBE.cs			\
		Entities/MW_watchlist.cs	\
		Entities/NavBE.cs	\
		Entities/GroupBE.cs			\
		Entities/RoleBE.cs			\
		Entities/GrantBE.cs			\
		Entities/CommentBE.cs		\
		Entities/TagBE.cs           \
		Entities/BanBE.cs			\
		Entities/TransactionBE.cs	\
		Entities/ResourceBE.cs		\
		Entities/AttachmentBE.cs	\
		Entities/PropertyBE.cs		\
		Entities/ResourceContentBE.cs       \
		Entities/ResultPopularityBE.cs      \
		Entities/TermAggregateBE.cs	\
		Entities/LoggedSearchBE.cs  \
        Entities/LoggedSearchResultBE.cs    \
        Entities/RatingBE.cs		\
        Entities/RatingComputedBE.cs        \
		AssemblyInfo.cs				\
		types.cs					\
		DBUtils.cs			        \
		DekiDataException.cs		\
		IDekiDataSession.cs		    \
		IDekiDataSessionFactory.cs	\
		IDekiDataStats.cs		    \
		IInstanceSettings.cs		\
		LoggingDekiDataSession.cs	\
		LoggingDekiDataSessionFactory.cs	\
		SearchAnalyticsQueryType.cs \
		SearchResultType.cs         \
        UserSubscription/IPageSubscriptionDataSession.cs    \
        UserSubscription/IPageSubscriptionDataSessionFactory.cs \
        UserSubscription/PageSubscriptionBE.cs
		
references = 																\
		-r:System															\
		-r:System.Data														\
		-r:System.Xml														\
		-r:$(addprefix $(root_dir)/, redist/log4net.dll)					\
		-r:$(addprefix $(build_prefix)/, mindtouch.deki.util.dll)			\
		-r:$(addprefix $(build_prefix)/, mindtouch.dream.dll)				
	
$(addprefix $(build_prefix)/, mindtouch.dream.dll): $(addprefix $(root_dir)/, redist/mindtouch.dream.dll)
	cp $(addprefix $(root_dir)/, redist/mindtouch.dream.dll) $(addprefix $(build_prefix)/, mindtouch.dream.dll)

$(TARGET): $(sources) $(addprefix $(build_prefix)/, mindtouch.dream.dll) 
	$(MCS) $(MCS_FLAGS) /out:$(TARGET) /target:library $(references) $(sources)

all: $(TARGET) 
