include $(root_dir)/Makefile.include
ASSEMBLY_NAME=mindtouch.deki.util.tests
ASSEMBLY=$(ASSEMBLY_NAME).dll
TARGET=$(addprefix $(build_prefix)/, $(ASSEMBLY))

sources =							\
	Documentation/HtmlDocumentationBuilderTests.cs  \
    Documentation/ReflectionTests.cs  \
    Documentation/TypeInspectorTests.cs   \
    Documentation/TypesForDocumentation.cs  \
    Export/ExportTests.cs   \
	Export/ExportManagerTests.cs	\
	Import/ImportTests.cs			\
	Import/ImportManagerTests.cs	\
	Packaging/ArchivePackagerTests.cs	\
	Packaging/FilePackagerTests.cs	\
	Packaging/PackagerBaseTests.cs  \
	PropertiesAssemblyInfo.cs       \
	Search/LuceneClauseBuilderTests.cs    \
	Search/LuceneClauseParserTests.cs    \
	Search/QueryTermTests.cs    \
	Search/SearchQueryParserTests.cs    \
	Search/SearchQueryTests.cs

references = \
	-r:System \
	-r:System.Data \
	-r:System.Xml \
	-r:System.Web \
	-r:$(addprefix $(root_dir)/, redist/mindtouch.deki.util.dll) \
	-r:$(addprefix $(root_dir)/, redist/mindtouch.dream.dll) \
	-r:$(addprefix $(root_dir)/, redist/mindtouch.documentation.dll) \
	-r:$(addprefix $(root_dir)/, redist/mindtouch.reflection.dll) \
	-r:$(addprefix $(root_dir)/, redist/mindtouch.dream.dll) \
	-r:$(addprefix $(root_dir)/, redist/mindtouch.dream.test.dll) \
	-r:$(addprefix $(root_dir)/, redist/nunit/nunit.framework.dll) \
	-r:$(addprefix $(root_dir)/, redist/log4net.dll)    \
    -r:$(addprefix $(root_dir)/, redist/Moq.dll)    \
	-r:$(addprefix $(root_dir)/, redist/SgmlReaderDll.dll)

$(TARGET): $(sources)
	$(MCS) $(MCS_FLAGS) /out:$(TARGET) /target:library $(references) $(sources) 

all: $(TARGET)
