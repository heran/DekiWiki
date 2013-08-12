include $(root_dir)/Makefile.include

ASSEMBLY_NAME=mindtouch.deki.mwconverter
ASSEMBLY=$(ASSEMBLY_NAME).exe

TARGET=$(addprefix $(build_prefix)/, $(ASSEMBLY))

sources = 							\
		IPBlockBE.cs					\
		LogBE.cs					\
		MediaWikiConverterContext.cs			\
		MediaWikiConverterService.cs			\
		MediaWikiDA.cs					\
		Program.cs					\
		RecentChangeBE.cs				\
		Site.cs						\
		WatchlistBE.cs					\
		WikiTextProcessor.cs				\
		Properties/AssemblyInfo.cs

references = 																\
		-r:System.Data														\
		-r:System.Xml														\
		-r:$(addprefix $(build_prefix)/, mindtouch.deki.data.dll)			\
		-r:$(addprefix $(build_prefix)/, mindtouch.deki.data.mysql.dll)		\
		-r:$(addprefix $(build_prefix)/, mindtouch.deki.dll)				\
		-r:$(addprefix $(build_prefix)/, mindtouch.deki.ext.dll)			\
		-r:$(addprefix $(build_prefix)/, mindtouch.deki.script.dll)			\
		-r:$(addprefix $(build_prefix)/, mindtouch.deki.util.dll)			\
		-r:$(addprefix $(build_prefix)/, mindtouch.indexservice.dll)		\
		-r:$(addprefix $(root_dir)/, redist/MySql.Data.dll)					\
		-r:$(addprefix $(root_dir)/, redist/mindtouch.core.dll)				\
		-r:$(addprefix $(root_dir)/, redist/mindtouch.dream.dll)

$(TARGET): $(sources) $(addprefix $(build_prefix)/, MySql.Data.dll) $(addprefix $(build_prefix)/, mindtouch.core.dll) $(addprefix $(build_prefix)/, mindtouch.dream.dll)
	$(MCS) /out:$(TARGET) /target:exe $(references) $(sources)

all: $(TARGET) 
