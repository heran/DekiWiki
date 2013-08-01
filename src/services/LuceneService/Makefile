include $(root_dir)/Makefile.include

ASSEMBLY_NAME=mindtouch.indexservice
ASSEMBLY=$(ASSEMBLY_NAME).dll

TARGET=$(addprefix $(build_prefix)/, $(ASSEMBLY))

sources = 							\
		ActionStack.cs				\
        Analyzers.cs                \
		Properties/AssemblyInfo.cs	\
		Html2Text.cs                \
		LuceneService.cs			\
		SearchInstanceData.cs       \
		UpdateDelayQueue.cs			\
		UpdateRecord.cs				\
		UpdateRecordDispatcher.cs
		
references = 																\
		-r:System															\
		-r:System.Data														\
		-r:System.Xml														\
		-r:$(addprefix $(build_prefix)/, mindtouch.dream.dll)				\
		-r:$(addprefix $(root_dir)/, redist/log4net.dll)					\
		-r:$(addprefix $(root_dir)/, redist/Lucene.Net.dll)					\
		-r:$(addprefix $(root_dir)/, redist/Highlighter.Net.dll)					\
		-r:$(addprefix $(root_dir)/, redist/SgmlReaderDll.dll)				

(addprefix $(build_prefix)/, mindtouch.dream.dll): $(addprefix $(root_dir)/, redist/mindtouch.dream.dll)
	cp $(addprefix $(root_dir)/, redist/mindtouch.dream.dll) $(addprefix $(build_prefix)/, mindtouch.dream.dll)

$(addprefix $(build_prefix)/, SgmlReaderDll.dll): $(addprefix $(root_dir)/, redist/SgmlReaderDll.dll)
	cp $(addprefix $(root_dir)/, redist/SgmlReaderDll.dll) $(addprefix $(build_prefix)/, SgmlReaderDll.dll)

$(addprefix $(build_prefix)/, Lucene.Net.dll): $(addprefix $(root_dir)/, redist/Lucene.Net.dll)
	cp $(addprefix $(root_dir)/, redist/Lucene.Net.dll) $(addprefix $(build_prefix)/, Lucene.Net.dll)
	
$(addprefix $(build_prefix)/, Highlighter.Net.dll): $(addprefix $(root_dir)/, redist/Highlighter.Net.dll)
	cp $(addprefix $(root_dir)/, redist/Highlighter.Net.dll) $(addprefix $(build_prefix)/, Highlighter.Net.dll)

$(TARGET): $(sources) $(addprefix $(build_prefix)/, mindtouch.dream.dll) $(addprefix $(build_prefix)/, SgmlReaderDll.dll) $(addprefix $(build_prefix)/, Lucene.Net.dll) $(addprefix $(build_prefix)/, Highlighter.Net.dll)
	$(MCS) $(MCS_FLAGS) /out:$(TARGET) /target:library $(references) $(sources)

all: $(TARGET) 
