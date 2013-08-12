include $(root_dir)/Makefile.include

ASSEMBLY_NAME=mindtouch.deki.data.mysql
ASSEMBLY=$(ASSEMBLY_NAME).dll

TARGET=$(addprefix $(build_prefix)/, $(ASSEMBLY))

sources = 							\
		DataAccess/GroupDA.cs			\
		DataAccess/GrantsDA.cs			\
		DataAccess/OldDA.cs				\
		DataAccess/UserDA.cs				\
		DataAccess/RecentChangeDA.cs \
		DataAccess/RolesRestrictionsDA.cs \
		DataAccess/ServicesDA.cs			\
		DataAccess/PageDA.cs		\
		DataAccess/RequestLogDA.cs	\
		DataAccess/ArchiveDA.cs		\
		DataAccess/LinksDA.cs		\
		DataAccess/CommentDA.cs		\
		DataAccess/ConfigDA.cs		\
		DataAccess/TagDA.cs		\
		DataAccess/NavDA.cs			\
		DataAccess/BanningDA.cs		\
		DataAccess/TransactionDA.cs \
		DataAccess/RatingDA.cs		\
		DataAccess/ResourceDA.cs	\
		DataAccess/ResourceMapDA.cs	\
		DataAccess/SearchAnalyticsDA.cs \
		DataAccess/WikiDA.cs	\
		AssemblyInfo.cs				\
		MySqlDBUtils.cs	\
		MySqlDekiDataSession.cs	\
		MySqlDekiDataSessionFactory.cs  \
        UserSubscription/MySqlPageSubscriptionSession.cs    \
        UserSubscription/MySqlPageSubscriptionSessionFactory.cs
		
references = 																\
		-r:System															\
		-r:System.Data														\
		-r:System.Xml														\
		-r:System.Web														\
		-r:$(addprefix $(root_dir)/, redist/log4net.dll)					\
		-r:$(addprefix $(build_prefix)/, MySql.Data.dll)					\
		-r:$(addprefix $(build_prefix)/, mindtouch.dream.dll)				\
		-r:$(addprefix $(build_prefix)/, mindtouch.deki.util.dll)			\
		-r:$(addprefix $(build_prefix)/, mindtouch.deki.data.dll)				
	
$(addprefix $(build_prefix)/, MySql.Data.dll): 
	cp $(addprefix $(root_dir)/, redist/MySql.Data.dll) $(addprefix $(build_prefix)/, MySql.Data.dll)

$(addprefix $(build_prefix)/, mindtouch.dream.dll): $(addprefix $(root_dir)/, redist/mindtouch.dream.dll)
	cp $(addprefix $(root_dir)/, redist/mindtouch.dream.dll) $(addprefix $(build_prefix)/, mindtouch.dream.dll)

$(TARGET): $(sources) $(addprefix $(build_prefix)/, MySql.Data.dll) $(addprefix $(build_prefix)/, mindtouch.dream.dll) $(addprefix $(build_prefix)/, mindtouch.deki.data.dll)
	$(MCS) $(MCS_FLAGS) /out:$(TARGET) /target:library $(references) $(sources)

all: $(TARGET) 
